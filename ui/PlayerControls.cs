using System;
using System.Collections.Generic;
using ImGuiNET;
using ImGuizmoNET;
using ImVec2 = System.Numerics.Vector2;
using System.Runtime.InteropServices;
using System.Linq;

namespace AnimLib {

    public class Gizmo3DObj : SceneObject {
        public override bool Is2D { get { return false; } }
        public override Vector3[] GetHandles2D() { return new Vector3[] {}; }
        public override Plane? GetSurface() { return null; }
        public override void SetHandle(int id, Vector3 wpos) { }
        public override object Clone() {
            return new Gizmo3DObj() {
                transform = new SceneTransform(transform.Pos, transform.Rot),
                timeslice = timeslice,
            };
        }
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
                var cam = UserInterface.WorldCamera as PerspectiveCameraState;
                var plane = new Plane() {
                    n = new Vector3(0.0f, 0.0f, -1.0f),
                    o = 0.0f,
                };
                var ray = cam.RayFromClip(new Vector2((UserInterface.mousePosition.x/view.Buffer.Width)*2.0f-1.0f, (UserInterface.mousePosition.y/view.Buffer.Height)*-2.0f+1.0f), view.Buffer.Width/view.Buffer.Height);
                var pos3 = ray.Intersect(plane);
                if(pos3 != null) {
                    switch(Clipboard.Object) {
                        case SceneObject s1:
                        if(!s1.Is2D) {
                            var obj = (SceneObject) s1.Clone();
                            obj.transform.Pos = new Vector3(pos3.Value.x, pos3.Value.y, obj.transform.Pos.z);
                            lock(player.Scene.sceneLock) {
                                player.Scene.Add(obj);
                                player.Scene.UpdateEvents();
                            }
                            player.SetAnimationDirty(true);
                        } else {
                            var obj = (SceneObject) s1.Clone();
                            obj.transform.Pos = new Vector3(UserInterface.mousePosition, obj.transform.Pos.z);
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

        private void SceneDropTarget() {
            //ImGuiDragDropFlags target_flags = 0;
            var cam = UserInterface.WorldCamera as PerspectiveCameraState;
            var plane = new Plane() {
                n = new Vector3(0.0f, 0.0f, -1.0f),
                o = 0.0f,
            };
            if (cam == null)
                return;
            if(ImGui.BeginDragDropTarget()) {
                ImGuiPayloadPtr payloadPtr = ImGui.AcceptDragDropPayload("DND_CREATE_ITEM");
                unsafe {
                    if(payloadPtr.NativePtr != null)
                    {
                        int idx = Marshal.ReadInt32(payloadPtr.Data);
                        Vector2 dropPos = ImGui.GetMousePos();
                        var ray = cam.RayFromClip(new Vector2((dropPos.x/UserInterface.Width)*2.0f-1.0f, (dropPos.y/UserInterface.Height)*-2.0f+1.0f), UserInterface.Width/UserInterface.Height);
                        var pos3 = ray.Intersect(plane); 
                        if(pos3 != null) {
                            switch(idx) {
                                case 0:
                                lock(player.Scene.sceneLock) {
                                    player.Scene.Circles.Add(new PlayerCircle() {
                                        radius = 1.0f,
                                        transform = new SceneTransform(pos3.Value, Quaternion.IDENTITY),
                                        timeslice = (0.0, 99999.0),
                                    });
                                }
                                break;
                                case 1:
                                lock(player.Scene.sceneLock) {
                                    player.Scene.Lines.Add(new PlayerLine() {
                                        start = Vector3.ZERO,
                                        end = Vector3.RIGHT,
                                        width = 0.1f,
                                        transform = new SceneTransform(pos3.Value, Quaternion.IDENTITY),
                                        timeslice = (0.0, 99999.0),
                                        color = Color.BLACK,
                                    });
                                }
                                break;
                                case 2:
                                lock(player.Scene.sceneLock) {
                                    player.Scene.Arrows.Add(new PlayerArrow() {
                                        start = Vector3.ZERO,
                                        end = Vector3.RIGHT,
                                        width = 0.1f,
                                        transform = new SceneTransform(pos3.Value, Quaternion.IDENTITY),
                                        timeslice = (0.0, 99999.0),
                                    });
                                }
                                break;
                                case 3:
                                var text = new Player2DText() {
                                    text = "New text",
                                    size = 14.0f,
                                    color = Color.BLACK,
                                    transform = new SceneTransform(new Vector3(dropPos.x, dropPos.y, 0.0f), Quaternion.IDENTITY),
                                    timeslice = (0.0, 99999.0),
                                };
                                lock(player.Scene.sceneLock) {
                                    player.Scene.Add(text);
                                }
                                break;
                                case 4:
                                var qs = new PlayerQSpline() {
                                    width = 1.0f,
                                    color = Color.BLACK,
                                    transform = new SceneTransform(pos3.Value, Quaternion.IDENTITY),
                                    timeslice = (0.0, 9999999.0),
                                    points = new Vector3[] { Vector3.ZERO, new Vector3(1.0f, 0.0f, 0.0f), new Vector3(2.0f, 1.0f, 0.0f) },
                                };
                                lock(player.Scene.sceneLock) {
                                    player.Scene.Add(qs);
                                }
                                break;
                            }
                            lock(player.Scene.sceneLock) {
                                player.Scene.UpdateEvents();
                            }
                            player.SetAnimationDirty(true);
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


            ImGui.Dummy(ImGui.GetWindowSize()); // needed for ImGui.BeginDragDropTarget
            SceneDropTarget();

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
                if(ImGui.Button(playText)) {
                    OnPlay();
                }
            }
            else {
                if(ImGui.Button(pauseText)) {
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
                        exportfileName = "animation-"+DateTime.Now.ToString("yyyy_MMdd_HHmmss")+".mp4";
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
                ImGui.EndMenu();
            }
            if(ImGui.BeginMenu("Create")) {
                var cam = UserInterface.WorldCamera as PerspectiveCameraState;
                if(cam != null) 
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                    ImGui.Text("(Drag and drop)");
                    ImGui.PopStyleColor();
                    Action<string, int> createItem = (string name, int idx) => {
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
                            ImGui.SetDragDropPayload("DND_CREATE_ITEM", mem, sizeof(int));
                            Marshal.FreeHGlobal(mem);
                            ImGui.EndDragDropSource();
                        }
                    };
                    createItem("Circle", 0);
                    createItem("Line", 1);
                    createItem("Arrow", 2);
                    createItem("Text", 3);
                    createItem("Quadratic spline", 4);
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

                    //ImGui.DockBuilderRemoveNode(dockspace_id); // clear any previous layout
                    //ImGui.DockBuilderAddNode(dockspace_id, dockspace_flags | ImGuiDockNodeFlags_DockSpace);
                    //ImGui.DockBuilderSetNodeSize(dockspace_id, viewport->Size);

                    // split the dockspace into 2 nodes -- DockBuilderSplitNode takes in the following args in the following order
                    //   window ID to split, direction, fraction (between 0 and 1), the final two setting let's us choose which id we want (which ever one we DON'T set as NULL, will be returned by the function)
                    //                                                              out_id_at_dir is the id of the node in the direction we specified earlier, out_id_at_opposite_dir is in the opposite direction
                    //var dockIdLeft = ImGui.DockBuilderSplitNode(dockspace_id, ImGuiDir_Left, 0.2f, nullptr, &dockspace_id);
                    //var dockIdDown = ImGui.DockBuilderSplitNode(dockspace_id, ImGuiDir_Down, 0.25f, nullptr, &dockspace_id);

                    // we now dock our windows into the docking node we made above
                    //ImGui::DockBuilderDockWindow("Down", dock_id_down);
                    //ImGui::DockBuilderDockWindow("Left", dock_id_left);
                    //ImGui::DockBuilderFinish(dockspace_id);
                }
            }

            ImGui.End();

            if(currentError != null) {
                ImGuiWindowFlags wf = ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.Begin("Animation error", wf);
                ImGui.Text(currentError);
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
                if(!Selection.Is2D) { // world space (3D) selection
                    var mt = view.DoSelectionGizmo(Selection.transform.Pos, Quaternion.IDENTITY, Vector3.ONE);
                    if (mt != null) {
                        var t = mt.Value;
                        if (t.posChanged) {
                            Selection.transform.Pos = t.newPosition;
                            changePending = true;
                        }
                    }
                } else { // screen space selection (text etc)
                    var mt = view.DoOrthoGizmo(Selection.transform.Pos, 0.0f);
                    if (mt != null) {
                        var t = mt.Value;
                        if (t.posChanged) {
                            Selection.transform.Pos = (Vector3)t.newPosition;
                            changePending = true;
                        }
                    }
                }
                // Object specific 2D handles (resizing etc)
                var h2 = Selection.GetHandles2D();
                if(h2 != null) {
                    int i = 0;
                    foreach(var hnd in h2) {
                        var uid = "obj"+Selection.GetHashCode()+"-"+i;
                        bool endupdate;
                        var newp = view.DoWorldSurfPointGizmo(uid, hnd, Selection.GetSurface().Value, out endupdate);
                        if(newp != null && newp.Value != hnd) {
                            Selection.SetHandle(i, newp.Value);
                        }
                        if(endupdate) {
                            player.SetAnimationDirty(true);
                        }
                        i++;
                    }
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
                    transform = new SceneTransform(pos, Quaternion.IDENTITY),
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
                updateDone = Selection.position != pos && !ImGuizmo.IsUsing();
                return Selection.position;
            }
            return pos;
        }

        public void DoInterface() {

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
                lock(player.Scene.sceneLock) {
                    var entId = UserInterface.MouseEntityId;
                    var ent = player.Machine.GetEntityState(entId);
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
                } else {
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
