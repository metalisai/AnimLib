using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using ImGuiNET;

namespace AnimLib
{

    public interface IRenderer {
        void RenderScene(WorldSnapshot ss, SceneView sv, CameraState cam, bool gizmo);
        bool BufferValid(IRenderBuffer buf);
        public IRenderBuffer CreateBuffer(int w, int h);
    }

    public class RenderState 
    {
        public enum BuiltinShader {
            None, 
            LineShader,
            ArrowShader,
            CubeShader,
            MeshShader,
        }

        public static IPlatform currentPlatform;

        public RenderState(IPlatform platform) {
            currentPlatform = platform;
            this.platform = platform;
            uiRenderBuffer = new DepthPeelRenderBuffer(platform);

            platform.OnSizeChanged += UpdateSize;
            platform.OnLoaded += Load;
            platform.mouseDown += mouseDown;
            platform.mouseUp += mouseUp;
            platform.mouseMove += mouseMove;
            platform.mouseScroll += mouseScroll;
            platform.PRenderFrame += RenderFrame;
        }

        readonly internal IPlatform platform;

        string guid = Guid.NewGuid().ToString();

        IRenderer renderer;

        public delegate void OnUpdateDelegate(double dt);
        public OnUpdateDelegate OnUpdate;
        public delegate void OnRenderSceneDelegate();
        public delegate void OnEndRenderSceneDelegate();

        bool _renderGizmos = true;
        public bool RenderGizmos {
            set {
                _renderGizmos = value;
            }
        }

        public OnRenderSceneDelegate OnBeginRenderScene;
        public OnEndRenderSceneDelegate OnEndRenderScene;

        private List<SceneView> views = new List<SceneView>();

        public ColoredTriangleMeshGeometry cubeGeometry;

        IRenderBuffer uiRenderBuffer;

        FontCache _fr;
        ITypeSetter ts = new FreetypeSetting();

        bool mouseLeft;
        bool mouseRight;
        Vector2 mousePos;
        float scrollValue;
        float scrollDelta;

        public bool overrideCamera = false;
        public PerspectiveCameraState debugCamera;
        Vector2 debugCamRot;

        static ImguiContext imgui = null;

        public int WindowWidth {
            get {
                return uiRenderBuffer.Size.Item1;
            }
        }

        public int WindowHeight {
            get {
                return uiRenderBuffer.Size.Item2;
            }
        }

        // resize buffers, UI etc
        public void UpdateSize(int width, int height) {
            if(uiRenderBuffer.Size.Item1 != width || uiRenderBuffer.Size.Item2 != height) {
                Debug.TLog($"Resize window to {width}x{height}");
                uiRenderBuffer.Resize(width, height);
                RectTransform.RootTransform.Size = new Vector2(uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2);
            }
        }

        public void Load(object sender, EventArgs args) {
            CreateMeshes();

            renderer = new DistanceFieldRenderer(platform as OpenTKPlatform, this);
            //renderer = new TessallationRenderer(platform as OpenTKPlatform, this);
            Debug.TLog($"Renderer implementation: {renderer}");
            _fr = new FontCache(ts, platform);
            uiRenderBuffer.Resize(1024, 1024);

            platform.PKeyDown += (object sender, KeyboardKeyEventArgs args) => {
                if(args.Key == Key.F && !args.IsRepeat) {
                    overrideCamera = !overrideCamera;
                    if(overrideCamera) {
                        debugCamera = new PerspectiveCameraState();
                        debugCamera.position.z = -13.0f;
                    } else {
                        debugCamera.position = Vector3.ZERO;
                        this.debugCamRot = Vector2.ZERO;
                    }
                }
                if(ImGui.GetCurrentContext() != IntPtr.Zero) {
                    var io = ImGui.GetIO();
                    io.KeysDown[(int)args.Key] = true;
                }
            };
            platform.PKeyUp += (object sender, KeyboardKeyEventArgs args) => {
                if(ImGui.GetCurrentContext() != IntPtr.Zero) {
                    var io = ImGui.GetIO();
                    io.KeysDown[(int)args.Key] = false;
                }
            };
            platform.PKeyPress += (object sender, KeyPressEventArgs args) => {
                if(ImGui.GetCurrentContext() != IntPtr.Zero) {
                    var io = ImGui.GetIO();
                    io.AddInputCharacter(args.KeyChar);
                }
            };

            this.OnUpdate += (double dtd) => {
                float dt = (float)dtd;
                var kstate = Keyboard.GetState();
                if(overrideCamera) {
                    float s = 5.0f;
                    if(kstate.IsKeyDown(Key.ShiftLeft)) {
                        s = 0.01f;
                    }
                    if(kstate.IsKeyDown(Key.W)) {
                        debugCamera.position += debugCamera.rotation*Vector3.FORWARD*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.S)) {
                        debugCamera.position -= debugCamera.rotation*Vector3.FORWARD*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.A)) {
                        debugCamera.position -= debugCamera.rotation*Vector3.RIGHT*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.D)) {
                        debugCamera.position += debugCamera.rotation*Vector3.RIGHT*dt*s;
                    }
                }
            };

            platform.mouseMove += (object s, MouseMoveEventArgs args) => {
                var state = OpenTK.Input.Keyboard.GetState();
                if(overrideCamera && !state[OpenTK.Input.Key.ControlLeft]) {
                    this.debugCamRot.x += args.XDelta*0.01f;
                    this.debugCamRot.y += args.YDelta*0.01f;
                    this.debugCamRot.x %= 2*MathF.PI;
                    this.debugCamRot.y %= 2*MathF.PI;
                    var qx = Quaternion.AngleAxis(debugCamRot.x, Vector3.UP);
                    var qy = Quaternion.AngleAxis(debugCamRot.y, Vector3.RIGHT);
                    debugCamera.rotation = qy * qx;
                }
            };

            imgui = new ImguiContext((int)uiRenderBuffer.Size.Item1, (int)uiRenderBuffer.Size.Item2);

        }

        public FontCache FontCache {
            get { 
                return _fr;
            }
        }

        public ITypeSetter TypeSetting {
            get {
                return ts;
            }
        }

        public void AddSceneView(SceneView view) {
            views.Add(view);
        }

        public void RemoveSceneView(SceneView view) {
            views.Remove(view);
        }

        private void mouseDown(object sender, MouseButtonEventArgs args) {
            if(args.Button == MouseButton.Left) {
                mouseLeft = true;
            }
            if(args.Button == MouseButton.Right) {
                mouseRight = true;
            }
        }

        private void mouseUp(object sender, MouseButtonEventArgs args) {
            if(args.Button == MouseButton.Left) {
                mouseLeft = false;
            }
            if(args.Button == MouseButton.Right) {
                mouseRight = false;
            }
        }

        private void mouseMove(object sender, MouseMoveEventArgs args) {
            mousePos = new Vector2(args.Position.X, args.Position.Y);
        }

        private void mouseScroll(object sender, MouseWheelEventArgs args) {
            scrollValue = args.ValuePrecise;
            scrollDelta = args.DeltaPrecise;
        }

        public int GetGuiEntityAtPixel(IRenderBuffer pb, Vector2 pixel) {
            if(pixel.x >= 0.0f && pixel.x < pb.Size.Item1 && pixel.y >= 0.0f && pixel.y < pb.Size.Item2) {
                return pb.GetEntityAtPixel((int)pixel.x, pb.Size.Item2-(int)pixel.y-1);
            } else {
                return -2;
            }
        }
        
        private void CreateMeshes() {
            cubeGeometry = new ColoredTriangleMeshGeometry(guid);
            cubeGeometry.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
            };
            cubeGeometry.indices = new uint[] {
                0,1,2, 1,3,2, 0,4,1, 1,4,5, 2,7,6, 2,3,7, 1,7,3, 1,5,7, 4,2,6, 4,0,2, 5,6,7, 5,4,6
            };
            cubeGeometry.colors = new Color[] {
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
            };
            cubeGeometry.Dirty = true;
            cubeGeometry.edgeCoordinates = new Vector2[] {
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
            };
        }

        WorldSnapshot currentScene;
        public void SetScene(WorldSnapshot ss) {
            currentScene = ss;
        }

        private void RenderFrame(object sender, FrameEventArgs args) {
            foreach(var view in views) {
                view.Buffer?.Clear();
                view.Buffer?.OnPreRender();
            }
            uiRenderBuffer.Clear();

            platform.ClearBackbuffer(0, 0, uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2);
            
            imgui.Update(uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2, 1.0f/60.0f, mousePos, mouseLeft, mouseRight, false, scrollDelta);

            foreach(var view in views) {
                view.BeginFrame();
            }
            if(OnUpdate != null) {
                OnUpdate(args.Time);
            }
            
            if(OnBeginRenderScene != null) {
                OnBeginRenderScene();
            }
            // Render scene
            if(currentScene != null)
            {
                Performance.views = views.Count;
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                foreach(var sv in views) {
                    // different renderer can use diffent types of buffers
                    // create the buffer if its wrong type or not created yet
                    bool valid = renderer.BufferValid(sv.Buffer);
                    if(sv.Buffer == null || !valid) {
                        if(sv.Buffer != null && !valid) {
                            sv.Buffer.Dispose();
                        }
                        sv.Buffer = renderer.CreateBuffer(sv.BufferWidth, sv.BufferHeight);
                    }
                    var pbSize = sv.Buffer.Size;
                    var sceneCamera = currentScene.Camera;
                    if(overrideCamera) sceneCamera = debugCamera;
                    renderer.RenderScene(currentScene, sv, sceneCamera, _renderGizmos);
                    sv.PostRender(sceneCamera, currentScene);
                }
                sw.Stop();
                Performance.TimeToRenderViews = sw.Elapsed.TotalSeconds;
            }

            if(OnEndRenderScene != null) {
                OnEndRenderScene();
            }

            foreach(var view in views) {
                view.Buffer?.OnPostRender();
            }

            // Render UI
            //var uiData = UserInterface.EndFrame();
            var uiData = imgui.Render();
            platform.RenderGUI(uiData, views, uiRenderBuffer);

            int sceneEntity = -2;
            if (views.Count > 0) {
                sceneEntity = views[0].GetEntityIdAtPixel(ImGui.GetMousePos());
            }
            var guiEntity = GetGuiEntityAtPixel(uiRenderBuffer, ImGui.GetMousePos());
            if(sceneEntity == -2) { // out of scene viewport
                UserInterface.MouseEntityId = guiEntity;
            } else {
                if (guiEntity == -1)
                    UserInterface.MouseEntityId = sceneEntity;
                else
                    UserInterface.MouseEntityId = guiEntity;
            }
        }
    }
}
