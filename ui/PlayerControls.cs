using System;
using System.Collections.Generic;
using ImGuiNET;
using ImGuizmoNET;
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
                Vector2 mpos = ImGui.GetMousePos();
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
        double exportEndTime = 10.0;

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
            Vector2 dropPos = ImGui.GetMousePos();
            var maybeRay = view.ScreenRay(dropPos);
            if(maybeRay != null && ImGui.BeginDragDropTarget()) {
                var ray = maybeRay.Value;
                ImGuiPayloadPtr payloadPtr = ImGui.AcceptDragDropPayload("DND_CREATE_ITEM_2D");
                unsafe {
                    if(payloadPtr.NativePtr != null)
                    {
                        Debug.TLog("Attempt drop 2D object");
                        // intersect canvases
                        CanvasState canvas = null;
                        // normalized canvas coordinates
                        var canvases = player.Machine.Entities.Where(x => x is CanvasState).Select(x  => (CanvasState)x).ToArray();
                        var canvasPos = view.TryIntersectCanvases(canvases, dropPos, out canvas);
                        // drop
                        DragDropObject obj = (DragDropObject)Marshal.ReadInt32(payloadPtr.Data);
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
                payloadPtr = ImGui.AcceptDragDropPayload("DND_CREATE_ITEM_3D");
                unsafe {
                    if(payloadPtr.NativePtr != null) {
                        Debug.TLog("Attempt drop 3D object");
                        var pos3 = ray.Intersect(plane); 
                        DragDropObject obj = (DragDropObject)Marshal.ReadInt32(payloadPtr.Data);
                        if(pos3 != null) {
                            DropEntity3D(obj, pos3.Value, dropPos);
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }        
        }

        private void ShowExportWindow() {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(430, 450), ImGuiCond.FirstUseEver);
            ImGuiWindowFlags wflags = ImGuiWindowFlags.NoDocking;
            if(!ImGui.Begin("Export", ref _showExport, wflags)) {
                ImGui.End();
                return;
            }
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(2.0f, 2.0f));
            ImGui.Columns(1);
            ImGui.Separator();

            ImGui.Text("Export animation");
            ImGui.Spacing();

            ImGui.InputText("File name", ref exportfileName, 64);
            ImGui.InputDouble("Start Time", ref exportStartTime);
            ImGui.InputDouble("End Time", ref exportEndTime);
            if(ImGui.Button("Export")) {
                player.ExportAnimation(exportfileName, exportStartTime, exportEndTime);
            }

            ImGui.Separator();
            ImGui.PopStyleVar();
            ImGui.End();
        }

        ImGuiDockNodeFlags dockNodeFlags = ImGuiDockNodeFlags.PassthruCentralNode;

        private void ShowSceneWindow(SceneView sview) {
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.DockNodeHost;
            windowFlags |= ImGuiWindowFlags.NoScrollbar;
            windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
            ImGui.Begin("Scene", windowFlags);



            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

            var fpadding = ImGui.GetStyle().FramePadding;
            var ispacing = ImGui.GetStyle().ItemInnerSpacing;

            string playText = "Play";
            string pauseText = "Pause";
            var btnSize = ImGui.CalcTextSize(playText+pauseText);

            // offset due to play etc buttons
            float verticalOffset = btnSize.Y+4.0f*fpadding.Y+2.0f*ispacing.Y;
            size.Y -= verticalOffset;

            var area = sview.CalculateArea(0, (int)verticalOffset, (int)size.X, (int)size.Y);
            size.X = area.Item3;
            size.Y = area.Item4;

            // play controls
            //ImGui.SameLine((size.X - btnSize.X/2.0f - fpadding.X) / 2.0f);
            ImGui.SameLine(area.Item1 + 2*fpadding.X);
            if(!playing) {
                if(ImGui.Button(playText, new ImVec2(50.0f, 20.0f))) {
                    OnPlay();
                }
            }
            else {
                if(ImGui.Button(pauseText, new ImVec2(50.0f, 20.0f))) {
                    OnStop();
                }
            }
            ImGui.SameLine();
            float playCur = progress;
            //ImGui.SliderFloat("", ref playCur, 0.0f, 1.0f);
            int hours = (int)Math.Floor(progressSeconds/360.0);
            int minutes = (int)Math.Floor(progressSeconds / 60.0);
            int seconds = (int)Math.Floor((progressSeconds-minutes*60)); 

            float z = 0.0f, o = 1.0f;
            GCHandle zhandle = GCHandle.Alloc(z, GCHandleType.Pinned);
            GCHandle ohandle = GCHandle.Alloc(o, GCHandleType.Pinned);
            GCHandle vhandle = GCHandle.Alloc(playCur, GCHandleType.Pinned);
            ImGui.SliderScalar($"{minutes:D2}:{seconds:D2}", ImGuiDataType.Float, vhandle.AddrOfPinnedObject(), zhandle.AddrOfPinnedObject(), ohandle.AddrOfPinnedObject(), "");
            float newValue = ((float)vhandle.Target);
            if (newValue != progress)
            {
                OnSeek(newValue);
            }
            zhandle.Free();
            ohandle.Free();
            vhandle.Free();


            var pos = (ImGui.GetWindowSize() - size) * 0.5f + new ImVec2(0.0f, verticalOffset/2.0f);
            ImGui.SetCursorPos(pos);
            var spos = ImGui.GetCursorScreenPos();
            ImGui.Image((IntPtr)view.TextureHandle, size, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 0.0f));
            SceneDropTarget();

            //ImGui.Image((IntPtr)OpenTKPlatform.handle, size, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 0.0f));
            view.SetArea((int)spos.X, (int)spos.Y, (int)size.X, (int)size.Y);
            ImGui.End();
        }

        private void ShowDock() {
            var vp = ImGui.GetMainViewport();

            var menuSize = new ImVec2(0.0f, 0.0f);

            ImGuiWindowFlags wflags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
            ImGui.SetNextWindowPos(new ImVec2(0.0f, menuSize.Y));
            ImGui.SetNextWindowSize(vp.Size - new ImVec2(0.0f, menuSize.Y));
            ImGui.SetNextWindowViewport(vp.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            wflags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            wflags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(0.0f, 0.0f));
            ImGui.Begin("DockSpace", wflags);
            ImGui.PopStyleVar(3);

            ImGui.BeginMenuBar();
            if(ImGui.BeginMenu("File")) {
                if(ImGui.MenuItem("New project..."))
                {
                    var result = FileChooser.ChooseDirectory("Choose a directory for new project...", "");
                    System.Console.WriteLine($"new project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.CreateProject(result);
                    }
                }
                if(ImGui.MenuItem("Open project..."))
                {
                    var result = FileChooser.ChooseFile("Choose a project file to open...", "", new string[] {"*.animproj"});
                    System.Console.WriteLine($"open project: {result}");
                    if(!string.IsNullOrEmpty(result)) {
                        player.ResourceManager.SetProject(result);
                    } else {
                        Debug.Warning("Failed to choose project file");
                    }
                }
                if(ImGui.MenuItem("Update"))
                {
                    player.SetAnimationDirty(true);
                }
                if(ImGui.MenuItem("Export video..."))
                {
                    _showExport = !_showExport;
                    if(_showExport) {
                        exportfileName = "animation-"+DateTime.Now.ToString("yyyy_MM_dd_HHmmss")+".mp4";
                    }
                }
                ImGui.EndMenu();
            }
            if(ImGui.BeginMenu("Window")) {
                if(ImGui.MenuItem("Resources..."))
                {
                    _showResources = true;
                }
                if(ImGui.MenuItem("Values..."))
                {
                    _showProperties = true;
                }
                if(ImGui.MenuItem("Preferences")) {
                }
                if(ImGui.MenuItem("Debug")) {
                    _showPerformance = true;
                }
                ImGui.EndMenu();
            }
            if(ImGui.BeginMenu("Create")) {
                var cam = view.LastCamera as PerspectiveCameraState;
                if(cam != null) 
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    ImGui.Text("(Drag and drop)");
                    ImGui.PopStyleColor();
                    Action<string, int, bool> createItem = (string name, int idx, bool is2d) => {
                        ImGui.Selectable(name);

                        ImGuiDragDropFlags src_flags = 0;
                        src_flags |= ImGuiDragDropFlags.SourceNoDisableHover;     // Keep the source displayed as hovered
                        src_flags |= ImGuiDragDropFlags.SourceNoHoldToOpenOthers; // Because our dragging is local, we disable the feature of opening foreign treenodes/tabs while dragging
                        //src_flags |= ImGuiDragDropFlags_SourceNoPreviewTooltip; // Hide the tooltip
                        if (ImGui.BeginDragDropSource(src_flags))
                        {
                            if((src_flags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0)
                                ImGui.Text("Creating " + name.ToLower());
                            var mem = Marshal.AllocHGlobal(4);
                            Marshal.WriteInt32(mem, idx);
                            ImGui.SetDragDropPayload(is2d ? "DND_CREATE_ITEM_2D" : "DND_CREATE_ITEM_3D", mem, sizeof(int));
                            Marshal.FreeHGlobal(mem);
                            ImGui.EndDragDropSource();
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
                ImGui.EndMenu();
            }
    
            menuSize = ImGui.GetWindowSize();
            ImGui.EndMenuBar();

            uint dockspaceId = 0;

            // DockSpace
            var io = ImGui.GetIO();
            if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
            {
                dockspaceId = ImGui.GetID("MyDockSpace");
                ImGui.DockSpace(dockspaceId, new ImVec2(0.0f, 0.0f), dockNodeFlags);

                var firstTime = true;
                if (firstTime)
                {
                    firstTime = false;
                }
            }

            ImGui.End();

            if(currentError != null) {
                ImGuiWindowFlags wf = ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.Begin("Animation error", wf);
                ImGui.Text(currentError);
                ImGui.Text(currentStackTrace);
                ImGui.End();
            }
            
            ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
            ShowSceneWindow(view);
        }

        public void ShowResourceInterface() {
            ImGuiWindowFlags wflags = ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize;
            if(ImGui.Begin("Resources", ref _showResources, wflags)) {

                var entries = player.ResourceManager.GetStoredResources();
                if(entries.Length > 0) {
                    var items = entries.Select(x => x.name).ToArray();
                    ImGui.ListBox("Resource files", ref selectedResource, items, items.Length);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    if(ImGui.Button("Delete")) {
                        if(selectedResource < entries.Length) {
                            var res = entries[selectedResource].name;
                            player.ResourceManager.DeleteResource(res);
                            Console.WriteLine($"Delete resource {res}");
                        }
                    }
                    ImGui.PopStyleColor();
                } else {
                    if(player.ResourceManager.haveProject) {
                        ImGui.Text("No resource files in this project!\nTo add some, you can drag and drop them in the application window.");
                    } else {
                        ImGui.Text("No project loaded.");
                    }
                }
            }
            ImGui.End();
        }

        byte[] colorNameBuf = new byte[64];
        private void ShowProperties() {
            if(ImGui.Begin("Values", ref _showProperties)) {
                var values = player.GetValues();
                if(values != null) {
                    ImGui.Text("Add values and use them in code");
                    // Colors
                    if(ImGui.CollapsingHeader("Colors")) {
                        // Show existing
                        foreach(var (key,col) in values.ColorMap.Select(x => (x.Key, x.Value)).ToArray()) {
                            System.Numerics.Vector4 c = (System.Numerics.Vector4)col.ToVector4();
                            if(ImGui.ColorEdit4(key, ref c)) {
                                values.ColorMap[key] = new Color(c);
                                player.SetAnimationDirty();
                            }
                        }
                        // Create new
                        ImGui.InputText("Name", colorNameBuf, (uint)colorNameBuf.Length);
                        ImGui.SameLine();
                        if(ImGui.Button("Create")) {
                            int len = Array.IndexOf(colorNameBuf, (byte)0);
                            var str = System.Text.Encoding.UTF8.GetString(colorNameBuf, 0, len);
                            if(!string.IsNullOrEmpty(str) && !values.ColorMap.ContainsKey(str)) {
                                values.ColorMap.Add(str, Color.GREEN);
                            }
                        }
                    }
                } else {
                    ImGui.Text("No values");
                }
            }
            ImGui.End();
        }

        private void ShowItemSelection() {
            var values = player.GetValues();
            lock(player.Scene.sceneLock) {
                // 3D translation handle
                if(true) { // world space (3D) selection
                    switch(Selection) {
                        case SceneObject2D s2:
#warning Need to do coordinate transformation here!
                        var mt = view.DoSelectionGizmo(s2.transform.Pos, Quaternion.IDENTITY, Vector3.ONE);
                        if (mt != null) {
                            var t = mt.Value;
                            if (t.posChanged) {
                                s2.transform.Pos = t.newPosition.xy;
                                changePending = true;
                            }
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
                // Object specific 2D handles (resizing etc)
                switch(Selection) {
                    case SceneObject2D s2:
                    var h2 = s2.GetHandles2D();
                    if(h2 != null) {
                        int i = 0;
                        foreach(var hnd in h2) {
                            var uid = "obj"+Selection.GetHashCode()+"-"+i;
                            bool endupdate;
#warning Need to do coordinate transformation here
                            var newp = view.DoWorldSurfPointGizmo(uid, hnd, s2.GetSurface().Value, out endupdate);
                            if(newp != null && newp.Value.xy != hnd) {
                                s2.SetHandle(i, newp.Value);
                            }
                            if(endupdate) {
                                player.SetAnimationDirty(true);
                            }
                            i++;
                        }
                    }
                    break;
                }
                // Imgui proprty window

                ImGui.SetNextWindowSize(new System.Numerics.Vector2(430, 450), ImGuiCond.FirstUseEver);
                
                ImGuiWindowFlags wflags = ImGuiWindowFlags.AlwaysAutoResize;
                if(!ImGui.Begin("Object properties", wflags)) {
                    ImGui.End();
                    return;
                }
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(2.0f, 2.0f));
                ImGui.Columns(1);
                ImGui.Separator();

                ImGui.Text("Object properties");
                ImGui.Spacing();
                foreach(var propF in Selection.Properties) {
                    var prop = propF.Item2();
                    switch(prop) {
                        case Vector3 v1:
                        System.Numerics.Vector3 v = v1;
                        if(ImGui.InputFloat3(propF.Item1, ref v)) {
                            propF.Item3((Vector3)v);
                            player.SetAnimationDirty();
                        }
                        break;
                        case Vector2 v2:
                        System.Numerics.Vector2 vv = v2;
                        if(ImGui.InputFloat2(propF.Item1, ref vv)) {
                            propF.Item3((Vector2)vv);
                            player.SetAnimationDirty();
                        }
                        break;
                        case double d1:
                        if(ImGui.InputDouble(propF.Item1, ref d1)) {
                            propF.Item3(d1);
                            player.SetAnimationDirty();
                        }
                        break;
                        case float f1:
                        if(ImGui.InputFloat(propF.Item1, ref f1)) {
                            propF.Item3(f1);
                            player.SetAnimationDirty();
                        }
                        break;
                        case Color c1:
                        System.Numerics.Vector4 c = (System.Numerics.Vector4)c1.ToVector4();
                        if(ImGui.ColorEdit4(propF.Item1, ref c)) {
                            propF.Item3(new Color(c.X, c.Y, c.Z, c.W));
                            player.SetAnimationDirty();
                        }
                        //ImGui.SameLine();
                        var colNames = values.ColorMap.Select(x =>x.Key).ToArray();
                        if(ImGui.BeginCombo(propF.Item1, "Select color")) {
                            for(int i = 0; i < colNames.Length; i++) {
                                if(ImGui.Selectable(colNames[i], false)) {
                                    propF.Item3(values.ColorMap[colNames[i]]);
                                    player.SetAnimationDirty();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        break;
                        case string s1:
                        if(ImGui.InputText(propF.Item1, ref s1, 128)) {
                            propF.Item3(s1);
                            player.SetAnimationDirty();
                        }
                        break;
                        default:
                            if(prop.GetType().IsEnum) {
                                if(ImGui.BeginCombo(propF.Item1, prop.ToString())) {
                                    var enumValues = Enum.GetValues(prop.GetType());
                                    foreach(var val in enumValues) {
                                        if(ImGui.Selectable(val.ToString(), false)) {
                                            propF.Item3(val);
                                            player.SetAnimationDirty();
                                        }
                                    }
                                    ImGui.EndCombo();
                                }
                            }
                        break;
                    }
                }

                ImGui.Separator();
                ImGui.PopStyleVar();
                ImGui.End();
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
                updateDone = g3d.position != pos && !ImGuizmo.IsUsing();
                return g3d.position;
            }
            return pos;
        }

        public void ShowPerf() {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 150), ImGuiCond.FirstUseEver);
            ImGuiWindowFlags wflags = ImGuiWindowFlags.NoDocking;
            if(!ImGui.Begin("Developer debug", ref _showPerformance, wflags)) {
                ImGui.End();
                return;
            }
            ImGui.Text($"Canvas rendering: {Performance.TimeToRenderCanvases*1000.0:N3}ms");
            ImGui.End();
        }

        public void DoInterface() {

            if(_showPerformance) {
                ShowPerf();
            }

            ShowDock();
            if(_showExport) {
                ShowExportWindow();
            }

            if(_showResources)
                ShowResourceInterface();
            if(_showProperties)
                ShowProperties();

            // select on left click
            if(ImGui.IsMouseClicked(0)
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
                    if(view.TryIntersectCanvas(c, ImGui.GetMousePos(), out normPos)) {
                        Vector2 canvasPos = (normPos)*new Vector2(c.width, c.height);
                        Debug.TLog($"Found canvas {c.name} {normPos} {canvasPos}");
                        lock(player.Scene.sceneLock) {
                            obj = player.Scene.GetCanvasObject(c.name, canvasPos);
                            if(obj != null) {
                                Selection = obj;
                                Debug.TLog($"Found canvas object {obj.name}");
                            } else {
                                Debug.Warning($"Canvas has pained the pixel under mouse, but the object was not found!");
                            }
                        }
                    }
                }
                else {
                    Debug.Warning($"Mouse over entity, but no entity with id {UserInterface.MouseEntityId} in scene");
                }
            } else if(ImGui.IsMouseClicked(0) && UserInterface.MouseEntityId == -1 && !ImGuizmo.IsOver()) {
                Selection = null;
            }

            if(Selection != null) {
                ShowItemSelection();
            }

            // update once nothing is being changed anymore
            // (updating animation is very expensive)
            if (changePending && !ImGuizmo.IsUsing()) {
                player.SetAnimationDirty(true);
                changePending = false;
            }
        }
    }
}
