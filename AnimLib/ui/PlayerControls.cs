using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace AnimLib;

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

internal class PlayerControls {
    AnimationPlayer player;
    public delegate void PlayD();
    public delegate void StopD();
    public delegate void SeekD(double progress);

    // scene has been changed, but change is not finished (e.g. gizmos still being used)
    bool changePending = false;

    private bool Exporting {
        get {
            return _showExport;
        }
    }

    private bool _showExport = false;
    private bool _showPerformance = false;
    private bool _showResources = false;
    private bool _showProperties = false;
    int selectedResource = 0;
    
    bool playing = false;
    float progress = 0.0f;
    double progressSeconds = 0.0;

    string? currentError = null;
    string? currentStackTrace = null;

    Vector2 anchor = new Vector2(0.5f, 1.0f);

    SceneObject? _selection = null;
    public SceneObject? Selection {
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

    public PlayerControls(RenderState renderer, AnimationPlayer player) {
        this.renderer = renderer;

        view = new SceneView(0, 0, 100, 100, 1920, 1080);
        renderer.AddSceneView(view);
        renderer.imgui.DrawMenuEvent += DrawMainMenu;
        
        if(this.player != null)
            throw new Exception();
        this.player = player;
        player.OnAnimationBaked += OnBaked;
        player.OnError += OnError;
        this.player.OnProgressUpdate += (sender, progress) => {
            SetProgress((float)progress, progress);
        };
        this.player.OnPlayStateChanged += (sender, playing) => {
            SetPlaying(playing);
        };

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
            if (cam == null) {
                return;
            }
            var plane = new Plane() {
                n = new Vector3(0.0f, 0.0f, -1.0f),
                o = 0.0f,
            };
            var w = view.Buffer.Size.Item1;
            var h = view.Buffer.Size.Item2;
            Vector2 mpos = Imgui.GetMousePos();
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
        Vector2 dropPos = Imgui.GetMousePos();
        var maybeRay = view.ScreenRay(dropPos);
        if(maybeRay != null && Imgui.BeginDragDropTarget()) {
            var ray = maybeRay.Value;
            IntPtr payloadPtr = Imgui.AcceptDragDropPayload("DND_CREATE_ITEM_2D");
            unsafe {
                if(payloadPtr != IntPtr.Zero) {
                    Debug.TLog("Attempt drop 2D object");
                    // intersect canvases
                    // normalized canvas coordinates
                    var canvases = player.Machine.Entities.Where(x => x is CanvasState).Select(x  => (CanvasState)x).ToArray();
                    var canvasPos = view.TryIntersectCanvases(canvases, dropPos, out var canvas);
                    // drop
                    DragDropObject obj = (DragDropObject)Marshal.ReadInt32(payloadPtr + 0);
                    if(canvas != null && canvasPos != null) {
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
            payloadPtr = Imgui.AcceptDragDropPayload("DND_CREATE_ITEM_3D");
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
            Imgui.EndDragDropTarget();
        }        
    }

    private void ShowExportProgress() {
        bool open = true;
        var flags = Imgui.ImGuiWindowFlags.NoDocking;
        flags |= Imgui.ImGuiWindowFlags.NoResize;
        flags |= Imgui.ImGuiWindowFlags.AlwaysAutoResize;
        flags |= Imgui.ImGuiWindowFlags.NoCollapse;
        if (Imgui.Begin("Export progress", ref open, flags))
        {
            float progress = player.ExportProgress ?? 0.0f;
            Imgui.Text($"Exporting to {exportfileName}");
            Imgui.ProgressBar(progress, new Vector2(400.0f, 0.0f));
            float ptime = progress * player.ExportLength;
            Imgui.Text($"{ptime:F2} / {player.ExportLength:F2}");

            if (Imgui.Button("Cancel"))
            {
                player.CancelExport();
            }

            Imgui.End();
        }
    }

    private void ShowExportWindow() {
        //ImguiContext.SetNextWindowSize(new System.Numerics.Vector2(430, 450), ImguiContext.ImGuiCond.FirstUseEver);
        var wflags = Imgui.ImGuiWindowFlags.NoDocking;
        wflags |= Imgui.ImGuiWindowFlags.AlwaysAutoResize;
        wflags |= Imgui.ImGuiWindowFlags.NoResize;
        if(!Imgui.Begin("Export", ref _showExport, wflags)) {
            Imgui.End();
            return;
        }
        Vector2 v = new (2.0f, 2.0f);
        Imgui.PushStyleVar(Imgui.ImGuiStyleVar.FramePadding, ref v);
        Imgui.Columns(1);
        Imgui.Separator();

        Imgui.Text("Export animation");
        Imgui.Spacing();

        Imgui.InputText("File name", ref exportfileName, 64);
        Imgui.InputDouble("Start Time", ref exportStartTime);
        Imgui.InputDouble("End Time", ref exportEndTime);
        if(Imgui.Button("Export")) {
            player.ExportAnimation(exportfileName, exportStartTime, exportEndTime);
        }

        Imgui.Separator();
        Imgui.PopStyleVar();
        Imgui.End();
    }

    public void ShowResourceInterface() {
        Imgui.ImGuiWindowFlags wflags = Imgui.ImGuiWindowFlags.NoDocking | Imgui.ImGuiWindowFlags.AlwaysAutoResize;
        if(Imgui.Begin("Resources", ref _showResources, wflags)) {
            var entries = player.ResourceManager.GetStoredResources();
            if(entries.Length > 0) {
                var items = entries.Select(x => x.name).ToArray();
                Imgui.ListBox("Resource files", ref selectedResource, items, items.Length);
                Vector4 col = new (0.5f, 0.5f, 0.5f, 1.0f);
                Imgui.PushStyleColor(Imgui.ImGuiCol.Button, ref col);
                if(Imgui.Button("Delete")) {
                    if(selectedResource < entries.Length) {
                        var res = entries[selectedResource].name;
                        player.ResourceManager.DeleteResource(res);
                        Console.WriteLine($"Delete resource {res}");
                    }
                }
                Imgui.PopStyleColor();
            } else {
                if(player.ResourceManager.haveProject) {
                    Imgui.Text("No resource files in this project!\nTo add some, you can drag and drop them in the application window.");
                } else {
                    Imgui.Text("No project loaded.");
                }
            }
        }
        Imgui.End();
    }

    private void DrawMainMenu() {
        Imgui.BeginMenuBar();
        if (Imgui.BeginMenu("File"))
        {
            if (Imgui.MenuItem("New project..."))
            {
                var result = FileChooser.ChooseDirectory("Choose a directory for new project...", "");
                System.Console.WriteLine($"new project: {result}");
                if(!string.IsNullOrEmpty(result)) {
                    player.ResourceManager.CreateProject(result);
                }
            }
            if (Imgui.MenuItem("Open project..."))
            {
                var result = FileChooser.ChooseFile("Choose a project file to open...", "", new string[] {"*.animproj"});
                System.Console.WriteLine($"open project: {result}");
                if(!string.IsNullOrEmpty(result)) {
                    player.ResourceManager.SetProject(result);
                } else {
                    Debug.Warning("Failed to choose project file");
                }
            }
            if (Imgui.MenuItem("Export video..."))
            {
                _showExport = !_showExport;
                if(_showExport) {
                    exportfileName = "animation-"+DateTime.Now.ToString("yyyy_MM_dd_HHmmss")+".mp4";
                }
            }
            if (Imgui.MenuItem("Update"))
            {
                player.SetAnimationDirty(true);
            }
            Imgui.EndMenu();
        }

        if (Imgui.BeginMenu("Window"))
        {
            if (Imgui.MenuItem("Resources..."))
            {
                _showResources = true;
            }
            if (Imgui.MenuItem("Values..."))
            {
                _showProperties = true;
            }
            if (Imgui.MenuItem("Preferences"))
            {
            }
            if (Imgui.MenuItem("Debug"))
            {
                _showPerformance = true;
            }
            Imgui.EndMenu();
        }

        if (Imgui.BeginMenu("Create"))
        {
            var cam = view.LastCamera as PerspectiveCameraState;
            if(cam != null) 
            {
                Vector4 col = new (0.5f, 0.5f, 0.5f, 1.0f);
                Imgui.PushStyleColor(Imgui.ImGuiCol.Text, ref col);
                Imgui.Text("(Drag and drop)");
                Imgui.PopStyleColor();
                Action<string, int, bool> createItem = (string name, int idx, bool is2d) => {
                    Imgui.DragDropItem(name);
                };
                /*
                // commented because it doesn't really work after all the changes that have happened
                createItem("Circle", (int)DragDropObject.Circle, true);
                createItem("Rectangle", (int)DragDropObject.Rectangle, true);
                createItem("Shape", (int)DragDropObject.Shape, true);
                createItem("Line", (int)DragDropObject.Line, false);
                createItem("Arrow", (int)DragDropObject.Arrow, false);
                createItem("Text", (int)DragDropObject.Text, false);
                createItem("Quadratic spline", (int)DragDropObject.Spline, false);
                */
            }
            else
            {
                Debug.Warning("No camera");
            }
            Imgui.EndMenu();
        }

        Imgui.EndMenuBar();

        if(currentError != null) {
            var wf = Imgui.ImGuiWindowFlags.AlwaysAutoResize;
            bool open = true;
            Imgui.Begin("Animation error", ref open, wf);
            Imgui.Text(currentError);
            Imgui.Text(currentStackTrace ?? "No stack trace");
            Imgui.End();
        }
    }

    private void ShowProperties() {
        string colorName = "";
        if(Imgui.Begin("Values", ref _showProperties)) {
            var values = player.GetValues();
            if(values != null) {
                Imgui.Text("Add values and use them in code");
                // Colors
                if(Imgui.CollapsingHeader("Colors")) {
                    // Show existing
                    foreach(var (key,col) in values.ColorMap.Select(x => (x.Key, x.Value)).ToArray()) {
                        var c = col.ToVector4();
                        if(Imgui.ColorEdit4(key, ref c)) {
                            values.ColorMap[key] = new Color(c);
                            player.SetAnimationDirty();
                        }
                    }
                    // Create new
                    Imgui.InputText("Name", ref colorName, 64);
                    Imgui.SameLine();
                    if(Imgui.Button("Create")) {
                        if(!string.IsNullOrEmpty(colorName) && !values.ColorMap.ContainsKey(colorName)) {
                            values.ColorMap.Add(colorName, Color.GREEN);
                        }
                    }
                }
            } else {
                Imgui.Text("No values");
            }
        }
        Imgui.End();
    }

    private void ShowItemSelection(SceneObject selection) {
        var values = player.GetValues();
        lock(player.Scene.sceneLock) {
            // Object specific 2D handles (resizing etc)
            switch(selection) {
                case SceneObject2D s2:
                var h2 = s2.GetHandles2D();
                if(h2 != null) {
                    int i = 0;
                    var canvas = player.Machine.Entities.Where(x => x is CanvasState).Select(x => (CanvasState)x).Where(x => x.name == s2.CanvasName).FirstOrDefault();
                    if (canvas == null) {
                        Debug.Warning("Mouse on 2D object, but its' lcanvas was not found. Selecting failed.");
                        break;
                    }
                    var mat = canvas.CanvasToWorld;
                    var mat2 = canvas.WorldToNormalizedCanvas;
                    if(canvas != null) {
                        foreach(var hnd in h2) {
                            var uid = "obj"+selection.GetHashCode()+"-"+i;
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

            Imgui.SetNextWindowSize(new System.Numerics.Vector2(430, 450), Imgui.ImGuiCond.FirstUseEver);
            
            var wflags = Imgui.ImGuiWindowFlags.AlwaysAutoResize;
            bool show = true;
            if(!Imgui.Begin("Object properties", ref show, wflags)) {
                Imgui.End();
                return;
            }
            Vector2 fp = new (2.0f, 2.0f);
            Imgui.PushStyleVar(Imgui.ImGuiStyleVar.FramePadding, ref fp);
            Imgui.Columns(1);
            Imgui.Separator();

            Imgui.Text("Object properties");
            Imgui.Spacing();
            foreach(var propF in selection.Properties) {
                var prop = propF.Item2();
                switch(prop) {
                    case Vector3 v1:
                    Vector3 v = v1;
                    if(Imgui.InputFloat3(propF.Item1, ref v)) {
                        propF.Item3((Vector3)v);
                        player.SetAnimationDirty();
                    }
                    break;
                    case Vector2 v2:
                    Vector2 vv = v2;
                    if(Imgui.InputFloat2(propF.Item1, ref vv)) {
                        propF.Item3((Vector2)vv);
                        player.SetAnimationDirty();
                    }
                    break;
                    case double d1:
                    if(Imgui.InputDouble(propF.Item1, ref d1)) {
                        propF.Item3(d1);
                        player.SetAnimationDirty();
                    }
                    break;
                    case float f1:
                    if(Imgui.InputFloat(propF.Item1, ref f1)) {
                        propF.Item3(f1);
                        player.SetAnimationDirty();
                    }
                    break;
                    case Color c1:
                    var c = c1.ToVector4();
                    if(Imgui.ColorEdit4(propF.Item1, ref c)) {
                        propF.Item3(new Color(c.x, c.y, c.z, c.w));
                        player.SetAnimationDirty();
                    }
                    //ImguiContext.SameLine();
                    var colNames = values.ColorMap.Select(x =>x.Key).ToArray();
                    if(Imgui.BeginCombo(propF.Item1, "Select color")) {
                        for(int i = 0; i < colNames.Length; i++) {
                            if(Imgui.Selectable(colNames[i], false)) {
                                propF.Item3(values.ColorMap[colNames[i]]);
                                player.SetAnimationDirty();
                            }
                        }
                        Imgui.EndCombo();
                    }
                    break;
                    case string s1:
                    if(Imgui.InputText(propF.Item1, ref s1, 128)) {
                        propF.Item3(s1);
                        player.SetAnimationDirty();
                    }
                    break;
                    default:
                        if(prop != null && prop.GetType().IsEnum) {
                            var name = prop.ToString() ?? "unknown";
                            if(Imgui.BeginCombo(propF.Item1, name)) {
                                var enumValues = Enum.GetValues(prop.GetType());
                                foreach(var val in enumValues) {
                                    if(Imgui.Selectable(val.ToString() ?? "enum option", false)) {
                                        propF.Item3(val);
                                        player.SetAnimationDirty();
                                    }
                                }
                                Imgui.EndCombo();
                            }
                        }
                    break;
                }
            }

            Imgui.Separator();
            Imgui.PopStyleVar();
            Imgui.End();
        }
    }

    public Vector2 Show2DHandle(string uid, Vector2 pos, Vector2 anchor, out bool updateDone) {
        var npos = view.DoScreenPointGizmo(uid, pos, anchor, out updateDone, 0xFFFF0000, true);
        var ret = npos == null ? pos : npos.Value;
        return ret;
    }

    public Vector3 Show3DHandle(string uid, Vector3 pos, out bool updateDone) {
        if(!gizmoStates.TryGetValue(uid, out var state)) {
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
        if(Selection is Gizmo3DObj g3d && Selection.name == uid) {
            //updateDone = g3d.position != pos && !ImGuizmo.IsUsing();
            return g3d.position;
        }
        return pos;
    }

    void TraversePerfTree(Performance.Call? node)
    {
        if (node == null)
            return;
        while (node != null)
        {
            if (Imgui.TreeNode(node.Name))
            {
                var time = (double)node.Time*1000.0/(double)System.Diagnostics.Stopwatch.Frequency;
                Imgui.Text($"Time: {time:N3}ms");
                TraversePerfTree(node.firstChild);
                Imgui.TreePop();
            }
            node = node.nextSibling;
        }
    }

    public void ShowPerf() {
        Imgui.SetNextWindowSize(new System.Numerics.Vector2(300, 150), Imgui.ImGuiCond.FirstUseEver);
        var wflags = Imgui.ImGuiWindowFlags.NoDocking;
        if(!Imgui.Begin("Developer debug", ref _showPerformance, wflags)) {
            Imgui.End();
            return;
        }
        Imgui.Text($"Frame processing: {Performance.TimeToProcessFrame*1000.0:N3}ms");
        Imgui.Text($"Wait sync: {Performance.TimeToWaitSync*1000.0:N3}ms");
        Imgui.Text($"View rendering: {Performance.TimeToRenderViews*1000.0:N3}ms");
        Imgui.Text($"  Canvas rendering: {Performance.TimeToRenderCanvases*1000.0:N3}ms");
        Imgui.Text($"Number of scene views: {Performance.views}");
        Imgui.Text($"Number of commands in animation: {Performance.CommandCount}");
        if (Imgui.TreeNode("Commands")) {
            for (int i = 0; i < Performance.CommandCount; i++) {
                var cmd = Performance.Commands[i];
                if (Imgui.TreeNode($"{cmd.GetType().Name} {cmd.time} {i}"))
                {
                    if (cmd is WorldPropertyCommand pcmd)
                    {
                        Imgui.Text($"Name: {pcmd.property}");
                    }
                    Imgui.TreePop();
                }
            }
            Imgui.TreePop();
        }

        Imgui.Text($"Last bake time: {Performance.TimeToBake*1000:N3}ms");

        TraversePerfTree(Performance.lastRoot);

        Imgui.End();
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

        if (player.Exporting)
        {
            ShowExportProgress();
        }

        if(_showResources)
            ShowResourceInterface();
        if(_showProperties)
            ShowProperties();

        // select on left click
        if(Imgui.IsMouseClicked(0)
                && UserInterface.MouseEntityId >= 0 
                && player.Scene != null) 
        {
            SceneObject? obj = null;
            EntityState? ent;
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
            } else if(ent is CanvasState c) {
                var mat = c.WorldToNormalizedCanvas;
                Vector2 normPos;
                if(player.Scene != null && view.TryIntersectCanvas(c, Imgui.GetMousePos(), out normPos)) {
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
        } else if(Imgui.IsMouseClicked(0) && UserInterface.MouseEntityId == -1 /*&& !ImGuizmo.IsOver()*/) {
            Selection = null;
        }

        if(Selection != null) {
            ShowItemSelection(Selection!);
        }

        // update once nothing is being changed anymore
        // (updating animation is very expensive)
        if (changePending /*&& !ImGuizmo.IsUsing()*/) {
            player.SetAnimationDirty(true);
            changePending = false;
        }
    }
}
