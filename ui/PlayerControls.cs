using System;
using System.Collections.Generic;
using ImVec2 = System.Numerics.Vector2;
using System.Runtime.InteropServices;
using System.Linq;

namespace AnimLib {

    public class Gizmo3DObj : SceneObject3D {
        public override object Clone() {
            return new Gizmo3DObj() {
                transform = new SceneTransform3D(transform.Pos, transform.Rot),
                timeslice = timeslice,
            };
        }
    }

    internal enum DragDropObject {
        None,
        Circle,
        Rectangle,
        Line,
        Arrow,
        Spline,
        Text,
        Shape,
    }

    public class PlayerControls {
        AnimationPlayer player;
        public delegate void PlayD();
        public delegate void StopD();
        public delegate void SeekD(double progress);

        public event PlayD OnPlay;
        public event StopD OnStop;
        public event SeekD OnSeek;

        // scene has been changed, but change is not finished (e.g. gizmos still being used)
        bool changePending = false;

        private bool _showExport = false;
        private bool _showPerformance = false;
        private bool _showResources = false;
        private bool _showProperties = false;
        int selectedResource = 0;
        
        bool playing = false;
        float progress = 0.0f;
        double progressSeconds = 0.0;

        string currentError = null;
        string currentStackTrace = null;

        Vector2 anchor = new Vector2(0.5f, 1.0f);

        SceneObject _selection = null;
        public SceneObject Selection {
            get {
                return _selection;
            }
            set {
                _selection = value;
            }
        }

        RenderState renderer;
        SceneView view;

        public SceneView MainView {
            get {
                return view;
            }
        }

        Dictionary<string, Gizmo3DObj> gizmoStates = new Dictionary<string, Gizmo3DObj>();

        public PlayerControls(RenderState renderer) {
            this.renderer = renderer;
            view = new SceneView(0, 0, 100, 100, 1920, 1080);
            renderer.AddSceneView(view);
            renderer.imgui.DrawMenuEvent += DrawMainMenu;
            renderer.imgui.PlayEvent += () => {
                Debug.Log("Play " + playing);
                if (!this.playing)
                {
                    this.player.Play();
                }
                else
                {
                    this.player.Stop();
                }
            };
            renderer.imgui.SeekEvent += (float progress) => {
                this.player.Seek(progress);
            };
        }

        private void OnBaked() {
            currentError = null;
            currentStackTrace = null;
        }

        private void OnError(string error, string stackTrace) {
            currentError = error;
            currentStackTrace = stackTrace;
        }

        public void SetPlayer(AnimationPlayer player) {
            if(this.player != null)
                throw new Exception();
            this.player = player;
            player.OnAnimationBaked += OnBaked;
            player.OnError += OnError;
        }

        public void SetPlaying(bool playing) {
            this.playing = playing;
        }

        public void SetProgress(float progress, double seconds) {
            this.progress = progress;
            this.progressSeconds = seconds;
        }

        public void Delete() {
            if(Selection != null) {
                lock(player.Scene.sceneLock) {
                    player.Scene.DestroyObject(Selection);
                }
                Selection = null;
                player.SetAnimationDirty(true);
            }
        }

        public void Copy() {
            if(Selection != null) {
                lock(player.Scene.sceneLock) {
                    Clipboard.Object = Selection.Clone();
                }
            }
        }

        public void Paste() {
            if(Clipboard.Object != null) {
                var cam = view.LastCamera as PerspectiveCameraState;
                var plane = new Plane() {
                    n = new Vector3(0.0f, 0.0f, -1.0f),
                    o = 0.0f,
                };
                var w = view.Buffer.Size.Item1;
                var h = view.Buffer.Size.Item2;
                Vector2 mpos = ImguiContext.GetMousePos();
                var ray = cam.RayFromClip(new Vector2((mpos.x/w)*2.0f-1.0f, (mpos.y/h)*-2.0f+1.0f), w/h);
                var pos3 = ray.Intersect(plane);
                if(pos3 != null) {
                    switch(Clipboard.Object) {
                        case SceneObject2D s1:
                        if(true) {
                            var obj = (SceneObject2D) s1.Clone();
                            obj.transform.Pos = new Vector2(pos3.Value.x, pos3.Value.y);
                            lock(player.Scene.sceneLock) {
                                player.Scene.Add(obj);
                                player.Scene.UpdateEvents();
                            }
                            player.SetAnimationDirty(true);
                        } else {
                            var obj = (SceneObject2D) s1.Clone();
                            obj.transform.Pos = mpos;
                            lock(player.Scene.sceneLock) {
                                player.Scene.Add(obj);
                                player.Scene.UpdateEvents();
                            }
                            player.SetAnimationDirty(true);
                        }
                        break;
                    }
                }
            }
        }

        public void Save() {
            player.SaveProject();
        }

        string exportfileName = "test.mp4";
        double exportStartTime = 0.0;
        double exportEndTime = 99999.0;

        private void DropEntity2D(DragDropObject obj, Vector2 pos, CanvasState canvas) {
            float size;
            switch(obj) {
                case DragDropObject.Circle:
                    size = 0.5f;
                    // 2d canvas coordinates are in pixels, make it larger
                    if(canvas.is2d) size *= 100.0f;
                    var c = new PlayerCircle(size, canvas.name);
                    c.transform.Pos = pos;

                lock(player.Scene.sceneLock) {
                    player.Scene.Add(c);
                    Debug.TLog($"Dropped circle at {c.transform.Pos} on canvas {canvas.name}");
                }
                break;
                case DragDropObject.Rectangle:
                    size = 1.0f;
                    if(canvas.is2d) size *= 100.0f;
                    var rr = new PlayerRect(size, size, canvas.name);
                    rr.transform.Pos = pos;
                    lock(player.Scene.sceneLock) {
                        player.Scene.Add(rr);
                        Debug.TLog($"Dropped rectangle at {rr.transform.Pos} on canvas {canvas.name}");
                    }
                break;
                case DragDropObject.Shape:
                    var ps = new PlayerShape(canvas.name);
                    ps.transform.Pos = pos;
                    lock(player.Scene.sceneLock) {
                        player.Scene.Add(ps);
                        Debug.TLog($"Dropped shape at {ps.transform.Pos} on canvas {canvas.name}");
                    }
                break;

            }
        }

        private void DropEntity3D(DragDropObject obj, Vector3 pos3, Vector2 dropPos) {
            switch(obj) {
                case DragDropObject.Line:
                lock(player.Scene.sceneLock) {
                    player.Scene.Add(new PlayerLine() {
                        start = Vector3.ZERO,
                        end = Vector3.RIGHT,
                        width = 0.1f,
                        transform = new SceneTransform2D(pos3.xy, 0.0f),
                        timeslice = (0.0, 99999.0),
                        color = Color.BLACK,
                    });
                }
                break;
                case DragDropObject.Arrow:
                lock(player.Scene.sceneLock) {
                    player.Scene.Add(new PlayerArrow() {
                        start = Vector3.ZERO,
                        end = Vector3.RIGHT,
                        width = 0.1f,
                        transform = new SceneTransform2D(pos3.xy, 0.0f),
                        timeslice = (0.0, 99999.0),
                    });
                }
                break;
                case DragDropObject.Text:
                var text = new Player2DText() {
                    text = "New text",
                    size = 14.0f,
                    color = Color.BLACK,
                    transform = new SceneTransform2D(new Vector2(dropPos.x, dropPos.y), 0.0f),
                    timeslice = (0.0, 99999.0),
                };
                lock(player.Scene.sceneLock) {
                    player.Scene.Add(text);
                }
                break;
                case DragDropObject.Spline:
                var qs = new PlayerQSpline() {
                    width = 1.0f,
                    color = Color.BLACK,
                    transform = new SceneTransform2D(pos3.xy, 0.0f),
                    timeslice = (0.0, 9999999.0),
                    points = new Vector2[] { Vector2.ZERO, new Vector2(1.0f, 0.0f), new Vector2(2.0f, 1.0f) },
                };
                lock(player.Scene.sceneLock) {
                    player.Scene.Add(qs);
                }
                break;
            }

        }

        private void SceneDropTarget() {
            //ImGuiDragDropFlags target_flags = 0;
            var cam = view.LastCamera as PerspectiveCameraState;
            var plane = new Plane() {
                n = new Vector3(0.0f, 0.0f, -1.0f),
                o = 0.0f,
            };
            if (cam == null)
                return;
            Vector2 dropPos = ImguiContext.GetMousePos();
            var maybeRay = view.ScreenRay(dropPos);
            if(maybeRay != null && ImguiContext.BeginDragDropTarget()) {
                var ray = maybeRay.Value;
                IntPtr payloadPtr = ImguiContext.AcceptDragDropPayload("DND_CREATE_ITEM_2D");
                unsafe {
                    if(payloadPtr != IntPtr.Zero)
                    {
                        Debug.TLog("Attempt drop 2D object");
                        // intersect canvases
                        CanvasState canvas = null;
                        // normalized canvas coordinates
                        var canvases = player.Machine.Entities.Where(x => x is CanvasState).Select(x  => (CanvasState)x).ToArray();
                        var canvasPos = view.TryIntersectCanvases(canvases, dropPos, out canvas);
                        // drop
                        DragDropObject obj = (DragDropObject)Marshal.ReadInt32(payloadPtr + 0);
                        if(canvasPos != null) {
                            var canvasPosW = new Vector2(canvas.width, canvas.height)*canvasPos.Value;
                            if(canvasPos != null) {
                                Debug.TLog($"Dropped object on canvas, canvas: {canvas}, pos: {canvasPosW}");
                            }
                            DropEntity2D(obj, canvasPosW, canvas);
                            lock(player.Scene.sceneLock) {
                                player.Scene.UpdateEvents();
                            }
                            player.SetAnimationDirty(true);
                        } else {
                            // TODO: just drop it on default (screen) canvas
                            Debug.TLog("Dropped canvas object but missed canvas");
                        }
                    } 
                }
                payloadPtr = ImguiContext.AcceptDragDropPayload("DND_CREATE_ITEM_3D");
                unsafe {
                    if(payloadPtr != IntPtr.Zero) {
                        Debug.TLog("Attempt drop 3D object");
                        var pos3 = ray.Intersect(plane); 
                        DragDropObject obj = (DragDropObject)Marshal.ReadInt32(payloadPtr + 0);
                        if(pos3 != null) {
                            DropEntity3D(obj, pos3.Value, dropPos);
                        }
                    }
                }
                ImguiContext.EndDragDropTarget();
            }        
        }

        private void ShowExportWindow() {
            ImguiContext.SetNextWindowSize(new System.Numerics.Vector2(430, 450), ImguiContext.ImGuiCond.FirstUseEver);
            var wflags = ImguiContext.ImGuiWindowFlags.NoDocking;
            if(!ImguiContext.Begin("Export", ref _showExport, wflags)) {
                ImguiContext.End();
                return;
            }
            Vector2 v = new (2.0f, 2.0f);
            ImguiContext.PushStyleVar(ImguiContext.ImGuiStyleVar.FramePadding, ref v);
            ImguiContext.Columns(1);
            ImguiContext.Separator();

            ImguiContext.Text("Export animation");
            ImguiContext.Spacing();

            ImguiContext.InputText("File name", ref exportfileName, 64);
            ImguiContext.InputDouble("Start Time", ref exportStartTime);
            ImguiContext.InputDouble("End Time", ref exportEndTime);
            if(ImguiContext.Button("Export")) {
                player.ExportAnimation(exportfileName, exportStartTime, exportEndTime);
            }

            ImguiContext.Separator();
            ImguiContext.PopStyleVar();
            ImguiContext.End();
        }

        //var dockNodeFlags = ImguiContext.ImGuiDockNodeFlags.PassthruCentralNode;

        /*private void ShowDock() {
            var vp = ImguiContext.GetMainViewport();

            var menuSize = new ImVec2(0.0f, 0.0f);

            ImGuiWindowFlags wflags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
            ImguiContext.SetNextWindowPos(new ImVec2(0.0f, menuSize.Y));
            ImguiContext.SetNextWindowSize(vp.Size - new ImVec2(0.0f, menuSize.Y));
            ImguiContext.SetNextWindowViewport(vp.ID);
            ImguiContext.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImguiContext.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            wflags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            wflags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            ImguiContext.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(0.0f, 0.0f));
            ImguiContext.Begin("DockSpace", wflags);
            ImguiContext.PopStyleVar(3);

            ImguiContext.BeginMenuBar();
            if(ImguiContext.BeginMenu("File")) {
                if(ImguiContext.MenuItem("New project..."))
                {
                    var result = FileChooser.ChooseDirectory("Choose a directory for new project...", "");
                    System.Console.WriteLine($"new project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.CreateProject(result);
                    }
                }
                if(ImguiContext.MenuItem("Open project..."))
                {
                    var result = FileChooser.ChooseFile("Choose a project file to open...", "", new string[] {"*.animproj"});
                    System.Console.WriteLine($"open project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.SetProject(result);
                    } else {
                        Debug.Warning("Failed to choose project file");
                    }
                }
                if(ImguiContext.MenuItem("Update"))
                {
                    player.SetAnimationDirty(true);
                }
                if(ImguiContext.MenuItem("Export video..."))
                {
                    _showExport = !_showExport;
                    if(_showExport) {
                        exportfileName = "animation-"+DateTime.Now.ToString("yyyy_MM_dd_HHmmss")+".mp4";
                    }
                }
                ImguiContext.EndMenu();
            }
            if(ImguiContext.BeginMenu("Window")) {
                if(ImguiContext.MenuItem("Resources..."))
                {
                    _showResources = true;
                }
                if(ImguiContext.MenuItem("Values..."))
                {
                    _showProperties = true;
                }
                if(ImguiContext.MenuItem("Preferences")) {
                }
                if(ImguiContext.MenuItem("Debug")) {
                    _showPerformance = true;
                }
                ImguiContext.EndMenu();
            }
            if(ImguiContext.BeginMenu("Create")) {
                var cam = view.LastCamera as PerspectiveCameraState;
                if(cam != null) 
                {
                    ImguiContext.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    ImguiContext.Text("(Drag and drop)");
                    ImguiContext.PopStyleColor();
                    Action<string, int, bool> createItem = (string name, int idx, bool is2d) => {
                        ImguiContext.Selectable(name);

                        ImGuiDragDropFlags src_flags = 0;
                        src_flags |= ImGuiDragDropFlags.SourceNoDisableHover;     // Keep the source displayed as hovered
                        src_flags |= ImGuiDragDropFlags.SourceNoHoldToOpenOthers; // Because our dragging is local, we disable the feature of opening foreign treenodes/tabs while dragging
                        //src_flags |= ImGuiDragDropFlags_SourceNoPreviewTooltip; // Hide the tooltip
                        if (ImguiContext.BeginDragDropSource(src_flags))
                        {
                            if((src_flags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0)
                                ImguiContext.Text("Creating " + name.ToLower());
                            var mem = Marshal.AllocHGlobal(4);
                            Marshal.WriteInt32(mem, idx);
                            ImguiContext.SetDragDropPayload(is2d ? "DND_CREATE_ITEM_2D" : "DND_CREATE_ITEM_3D", mem, sizeof(int));
                            Marshal.FreeHGlobal(mem);
                            ImguiContext.EndDragDropSource();
                        }
                    };
                    createItem("Circle", (int)DragDropObject.Circle, true);
                    createItem("Rectangle", (int)DragDropObject.Rectangle, true);
                    createItem("Shape", (int)DragDropObject.Shape, true);
                    createItem("Line", (int)DragDropObject.Line, false);
                    createItem("Arrow", (int)DragDropObject.Arrow, false);
                    createItem("Text", (int)DragDropObject.Text, false);
                    createItem("Quadratic spline", (int)DragDropObject.Spline, false);
                }
                ImguiContext.EndMenu();
            }
    
            menuSize = ImguiContext.GetWindowSize();
            ImguiContext.EndMenuBar();

            uint dockspaceId = 0;

            // DockSpace
            var io = ImguiContext.GetIO();
            if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
            {
                dockspaceId = ImguiContext.GetID("MyDockSpace");
                ImguiContext.DockSpace(dockspaceId, new ImVec2(0.0f, 0.0f), dockNodeFlags);

                var firstTime = true;
                if (firstTime)
                {
                    firstTime = false;
                }
            }

            ImguiContext.End();

            if(currentError != null) {
                ImGuiWindowFlags wf = ImGuiWindowFlags.AlwaysAutoResize;
                ImguiContext.Begin("Animation error", wf);
                ImguiContext.Text(currentError);
                ImguiContext.Text(currentStackTrace);
                ImguiContext.End();
            }
            
            ImguiContext.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
        }*/

        public void ShowResourceInterface() {
            ImguiContext.ImGuiWindowFlags wflags = ImguiContext.ImGuiWindowFlags.NoDocking | ImguiContext.ImGuiWindowFlags.AlwaysAutoResize;
            if(ImguiContext.Begin("Resources", ref _showResources, wflags)) {

                var entries = player.ResourceManager.GetStoredResources();
                if(entries.Length > 0) {
                    var items = entries.Select(x => x.name).ToArray();
                    ImguiContext.ListBox("Resource files", ref selectedResource, items, items.Length);
                    Vector4 col = new (0.5f, 0.5f, 0.5f, 1.0f);
                    ImguiContext.PushStyleColor(ImguiContext.ImGuiCol.Button, ref col);
                    if(ImguiContext.Button("Delete")) {
                        if(selectedResource < entries.Length) {
                            var res = entries[selectedResource].name;
                            player.ResourceManager.DeleteResource(res);
                            Console.WriteLine($"Delete resource {res}");
                        }
                    }
                    ImguiContext.PopStyleColor();
                } else {
                    if(player.ResourceManager.haveProject) {
                        ImguiContext.Text("No resource files in this project!\nTo add some, you can drag and drop them in the application window.");
                    } else {
                        ImguiContext.Text("No project loaded.");
                    }
                }
            }
            ImguiContext.End();
        }

        private void DrawMainMenu() {
            ImguiContext.BeginMenuBar();
            if (ImguiContext.BeginMenu("File"))
            {
                if (ImguiContext.MenuItem("New project..."))
                {
                    var result = FileChooser.ChooseDirectory("Choose a directory for new project...", "");
                    System.Console.WriteLine($"new project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.CreateProject(result);
                    }
                }
                if (ImguiContext.MenuItem("Open project..."))
                {
                    var result = FileChooser.ChooseFile("Choose a project file to open...", "", new string[] {"*.animproj"});
                    System.Console.WriteLine($"open project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.SetProject(result);
                    } else {
                        Debug.Warning("Failed to choose project file");
                    }
                }
                if (ImguiContext.MenuItem("Export video..."))
                {
                    _showExport = !_showExport;
                    if(_showExport) {
                        exportfileName = "animation-"+DateTime.Now.ToString("yyyy_MM_dd_HHmmss")+".mp4";
                    }
                }
                if (ImguiContext.MenuItem("Update"))
                {
                    player.SetAnimationDirty(true);
                }
                ImguiContext.EndMenu();
            }

            if (ImguiContext.BeginMenu("Window"))
            {
                if (ImguiContext.MenuItem("Resources..."))
                {
                    _showResources = true;
                }
                if (ImguiContext.MenuItem("Values..."))
                {
                    _showProperties = true;
                }
                if (ImguiContext.MenuItem("Preferences"))
                {
                }
                if (ImguiContext.MenuItem("Debug"))
                {
                    _showPerformance = true;
                }
                ImguiContext.EndMenu();
            }

            if (ImguiContext.BeginMenu("Create"))
            {
                var cam = view.LastCamera as PerspectiveCameraState;
                if(cam != null) 
                {
                    Vector4 col = new (0.5f, 0.5f, 0.5f, 1.0f);
                    ImguiContext.PushStyleColor(ImguiContext.ImGuiCol.Text, ref col);
                    ImguiContext.Text("(Drag and drop)");
                    ImguiContext.PopStyleColor();
                    Action<string, int, bool> createItem = (string name, int idx, bool is2d) => {
                        ImguiContext.DragDropItem(name);
                    };
                    createItem("Circle", (int)DragDropObject.Circle, true);
                    createItem("Rectangle", (int)DragDropObject.Rectangle, true);
                    createItem("Shape", (int)DragDropObject.Shape, true);
                    createItem("Line", (int)DragDropObject.Line, false);
                    createItem("Arrow", (int)DragDropObject.Arrow, false);
                    createItem("Text", (int)DragDropObject.Text, false);
                    createItem("Quadratic spline", (int)DragDropObject.Spline, false);
                }
                else
                {
                    Debug.Warning("No camera");
                }
                ImguiContext.EndMenu();
            }

            ImguiContext.EndMenuBar();
        }

        private void ShowProperties() {
            string colorName = "";
            if(ImguiContext.Begin("Values", ref _showProperties)) {
                var values = player.GetValues();
                if(values != null) {
                    ImguiContext.Text("Add values and use them in code");
                    // Colors
                    if(ImguiContext.CollapsingHeader("Colors")) {
                        // Show existing
                        foreach(var (key,col) in values.ColorMap.Select(x => (x.Key, x.Value)).ToArray()) {
                            var c = col.ToVector4();
                            if(ImguiContext.ColorEdit4(key, ref c)) {
                                values.ColorMap[key] = new Color(c);
                                player.SetAnimationDirty();
                            }
                        }
                        // Create new
                        ImguiContext.InputText("Name", ref colorName, 64);
                        ImguiContext.SameLine();
                        if(ImguiContext.Button("Create")) {
                            if(!string.IsNullOrEmpty(colorName) && !values.ColorMap.ContainsKey(colorName)) {
                                values.ColorMap.Add(colorName, Color.GREEN);
                            }
                        }
                    }
                } else {
                    ImguiContext.Text("No values");
                }
            }
            ImguiContext.End();
        }

        private void ShowItemSelection() {
            var values = player.GetValues();
            lock(player.Scene.sceneLock) {

                // Object specific 2D handles (resizing etc)
                switch(Selection) {
                    case SceneObject2D s2:
                    var h2 = s2.GetHandles2D();
                    if(h2 != null) {
                        int i = 0;
                        var canvas = player.Machine.Entities.Where(x => x is CanvasState).Select(x => x as CanvasState).Where(x => x.name == s2.CanvasName).FirstOrDefault();
                        var mat = canvas.CanvasToWorld;
                        var mat2 = canvas.WorldToNormalizedCanvas;
                        if(canvas != null) {
                            foreach(var hnd in h2) {
                                var uid = "obj"+Selection.GetHashCode()+"-"+i;
                                bool endupdate;
                                // canvas to world
                                var worldp = mat * new Vector4(s2.transform.Pos+hnd, 0.0f, 1.0f);
                                // draw and handle interactive UI
                                var updatedWorldp = view.DoWorldSurfPointGizmo(uid, worldp.xyz, canvas.Surface, out endupdate);
                                if(updatedWorldp != null) {
                                    // convert back to canvas coordinates
                                    var newp = (mat2 * new Vector4(updatedWorldp.Value, 1.0f)).xy;
                                    newp *= new Vector2(canvas.width, canvas.height);
                                    newp = newp - s2.transform.Pos;
                                    if(newp != hnd) {
                                        s2.SetHandle(i, newp);
                                    }
                                }
                                if(endupdate) {
                                    player.SetAnimationDirty(true);
                                }
                                i++;
                            }
                        }
                    }
                    break;
                }

                // 3D translation handle
                if(true) { // world space (3D) selection
                    switch(Selection) {
                        case SceneObject2D s2:
                        var canvas = player.Machine.Entities.Where(x => x is CanvasState).Select(x  => (CanvasState)x).Where(x => x.name == s2.CanvasName).FirstOrDefault();
                        if(canvas != null) {
                            var mt = view.DoSelectionGizmo2D(canvas, s2.transform.Pos, s2.transform.Rot, Vector2.ONE);
                            if (mt != null) {
                                var t = mt.Value;
                                if (t.posChanged) {
                                    s2.transform.Pos = t.newPosition.xy;
                                    s2.transform.Rot = t.newRotation.x;
                                    changePending = true;
                                }
                            }
                        } else {
                            Debug.Warning($"2D entity \"{s2.name}\" selected, but its canvas \"{s2.CanvasName}\" doesn't exist.");
                        }
                        break;
                        case SceneObject3D s3:
                        var mt3 = view.DoSelectionGizmo(s3.transform.Pos, Quaternion.IDENTITY, Vector3.ONE);
                        if (mt3 != null) {
                            var t = mt3.Value;
                            if (t.posChanged) {
                                s3.transform.Pos = t.newPosition.xy;
                                changePending = true;
                            }
                        }
                        break;
                    }
                } else { // screen space selection (text etc)
                    /*var mt = view.DoOrthoGizmo(Selection.transform.Pos, 0.0f);
                    if (mt != null) {
                        var t = mt.Value;
                        if (t.posChanged) {
                            Selection.transform.Pos = (Vector3)t.newPosition;
                            changePending = true;
                        }
                    }*/
                }
                // Imgui proprty window

                ImguiContext.SetNextWindowSize(new System.Numerics.Vector2(430, 450), ImguiContext.ImGuiCond.FirstUseEver);
                
                var wflags = ImguiContext.ImGuiWindowFlags.AlwaysAutoResize;
                bool show = true;
                if(!ImguiContext.Begin("Object properties", ref show, wflags)) {
                    ImguiContext.End();
                    return;
                }
                Vector2 fp = new (2.0f, 2.0f);
                ImguiContext.PushStyleVar(ImguiContext.ImGuiStyleVar.FramePadding, ref fp);
                ImguiContext.Columns(1);
                ImguiContext.Separator();

                ImguiContext.Text("Object properties");
                ImguiContext.Spacing();
                foreach(var propF in Selection.Properties) {
                    var prop = propF.Item2();
                    switch(prop) {
                        case Vector3 v1:
                        Vector3 v = v1;
                        if(ImguiContext.InputFloat3(propF.Item1, ref v)) {
                            propF.Item3((Vector3)v);
                            player.SetAnimationDirty();
                        }
                        break;
                        case Vector2 v2:
                        Vector2 vv = v2;
                        if(ImguiContext.InputFloat2(propF.Item1, ref vv)) {
                            propF.Item3((Vector2)vv);
                            player.SetAnimationDirty();
                        }
                        break;
                        case double d1:
                        if(ImguiContext.InputDouble(propF.Item1, ref d1)) {
                            propF.Item3(d1);
                            player.SetAnimationDirty();
                        }
                        break;
                        case float f1:
                        if(ImguiContext.InputFloat(propF.Item1, ref f1)) {
                            propF.Item3(f1);
                            player.SetAnimationDirty();
                        }
                        break;
                        case Color c1:
                        var c = c1.ToVector4();
                        if(ImguiContext.ColorEdit4(propF.Item1, ref c)) {
                            propF.Item3(new Color(c.x, c.y, c.z, c.w));
                            player.SetAnimationDirty();
                        }
                        //ImguiContext.SameLine();
                        var colNames = values.ColorMap.Select(x =>x.Key).ToArray();
                        if(ImguiContext.BeginCombo(propF.Item1, "Select color")) {
                            for(int i = 0; i < colNames.Length; i++) {
                                if(ImguiContext.Selectable(colNames[i], false)) {
                                    propF.Item3(values.ColorMap[colNames[i]]);
                                    player.SetAnimationDirty();
                                }
                            }
                            ImguiContext.EndCombo();
                        }
                        break;
                        case string s1:
                        if(ImguiContext.InputText(propF.Item1, ref s1, 128)) {
                            propF.Item3(s1);
                            player.SetAnimationDirty();
                        }
                        break;
                        default:
                            if(prop.GetType().IsEnum) {
                                if(ImguiContext.BeginCombo(propF.Item1, prop.ToString())) {
                                    var enumValues = Enum.GetValues(prop.GetType());
                                    foreach(var val in enumValues) {
                                        if(ImguiContext.Selectable(val.ToString(), false)) {
                                            propF.Item3(val);
                                            player.SetAnimationDirty();
                                        }
                                    }
                                    ImguiContext.EndCombo();
                                }
                            }
                        break;
                    }
                }

                ImguiContext.Separator();
                ImguiContext.PopStyleVar();
                ImguiContext.End();
            }
        }

        public Vector2 Show2DHandle(string uid, Vector2 pos, Vector2 anchor, out bool updateDone) {
            var npos = view.DoScreenPointGizmo(uid, pos, anchor, out updateDone, 0xFFFF0000, true);
            var ret = npos == null ? pos : npos.Value;
            return ret;
        }

        public Vector3 Show3DHandle(string uid, Vector3 pos, out bool updateDone) {
            Gizmo3DObj state;
            if(!gizmoStates.TryGetValue(uid, out state)) {
                state = new Gizmo3DObj() {
                    transform = new SceneTransform3D(pos, Quaternion.IDENTITY),
                    timeslice = (0.0, 99999.0),
                };
                state.position = pos;
                state.name = uid;
                gizmoStates.Add(uid, state);
            }
            if(view.DoWorldCircleButton(pos, uid)) {
                Selection = state;
            }
            updateDone = false;
            if(Selection is Gizmo3DObj && Selection.name == uid) {
                var g3d = Selection as Gizmo3DObj;
                //updateDone = g3d.position != pos && !ImGuizmo.IsUsing();
                return g3d.position;
            }
            return pos;
        }

        public void ShowPerf() {
            ImguiContext.SetNextWindowSize(new System.Numerics.Vector2(300, 150), ImguiContext.ImGuiCond.FirstUseEver);
            var wflags = ImguiContext.ImGuiWindowFlags.NoDocking;
            if(!ImguiContext.Begin("Developer debug", ref _showPerformance, wflags)) {
                ImguiContext.End();
                return;
            }
            ImguiContext.Text($"Frame processing: {Performance.TimeToProcessFrame*1000.0:N3}ms");
            ImguiContext.Text($"Wait sync: {Performance.TimeToWaitSync*1000.0:N3}ms");
            ImguiContext.Text($"View rendering: {Performance.TimeToRenderViews*1000.0:N3}ms");
            ImguiContext.Text($"  Canvas rendering: {Performance.TimeToRenderCanvases*1000.0:N3}ms");
            ImguiContext.Text($"Number of scene views: {Performance.views}");
            ImguiContext.Text($"Number of commands in animation: {Performance.CommandCount}");
            ImguiContext.Text($"Last bake time: {Performance.TimeToBake*1000:N3}ms");
            ImguiContext.End();
        }

        public void DoInterface() {
            this.renderer.imgui.SceneWindow((double)view.BufferWidth/view.BufferHeight, view.TextureHandle, this.playing, this.progress, 1.0f);

            if(_showPerformance) {
                ShowPerf();
            }

            //ShowDock();
            if(_showExport) {
                ShowExportWindow();
            }

            if(_showResources)
                ShowResourceInterface();
            if(_showProperties)
                ShowProperties();

            // select on left click
            if(ImguiContext.IsMouseClicked(0)
                    && UserInterface.MouseEntityId >= 0 
                    && player.Scene != null) 
            {
                SceneObject obj = null;
                EntityState ent;
                lock(player.Scene.sceneLock) {
                    var entId = UserInterface.MouseEntityId;
                    ent = player.Machine.GetEntityState(entId);
                    if(ent != null) {
                        if(ent.selectable) {
                            obj = player.Scene?.GetSceneObjectById(entId);
                        }
                        // TODO: recursive
                        if(obj == null && ent.parentId != 0) {
                            obj = player.Scene?.GetSceneObjectById(ent.parentId);
                        }
                    }
                }
                if(obj != null) {
                    Selection = obj;
                } else if(ent is CanvasState) {
                    var c = ent as CanvasState;
                    var mat = c.WorldToNormalizedCanvas;
                    Vector2 normPos;
                    if(view.TryIntersectCanvas(c, ImguiContext.GetMousePos(), out normPos)) {
                        Vector2 canvasPos = (normPos)*new Vector2(c.width, c.height);
                        Debug.TLog($"Found canvas {c.name} {normPos} {canvasPos}");
                        lock(player.Scene.sceneLock) {
                            obj = player.Scene.GetCanvasObject(c.name, canvasPos);
                            if(obj != null) {
                                Selection = obj;
                                Debug.TLog($"Found canvas object {obj.name}");
                            } else {
                                Debug.Warning($"Canvas has painted the pixel under mouse, but the object was not found!");
                            }
                        }
                    }
                }
                else {
                    Debug.Warning($"Mouse over entity, but no entity with id {UserInterface.MouseEntityId} in scene");
                }
            } else if(ImguiContext.IsMouseClicked(0) && UserInterface.MouseEntityId == -1 /*&& !ImGuizmo.IsOver()*/) {
                Selection = null;
            }

            if(Selection != null) {
                ShowItemSelection();
            }

            // update once nothing is being changed anymore
            // (updating animation is very expensive)
            if (changePending /*&& !ImGuizmo.IsUsing()*/) {
                player.SetAnimationDirty(true);
                changePending = false;
            }
        }
    }
}
