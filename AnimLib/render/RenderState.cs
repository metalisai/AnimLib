using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;

namespace AnimLib;

internal interface IRenderer {
    void RenderScene(WorldSnapshot ss, CameraState cam, bool gizmo, out IBackendRenderBuffer mainBuffer);
    bool BufferValid(IBackendRenderBuffer buf);
}

/// <summary>
/// Legacy shaders, probably will be replaced/removed.
/// </summary>
public enum BuiltinShader {
    /// <summary> No shader </summary>
    None, 
    /// <summary> Shader for rendering lines </summary>
    LineShader,
    /// <summary> Shader for rendering arrows </summary>
    ArrowShader,
    /// <summary> Shader for rendering cubes </summary>
    CubeShader,
    /// <summary> Shader for rendering generic meshes </summary>
    MeshShader,
    /// <summary> Shader for rendering quads </summary>
    QuadShader,
    /// <summary> Shader to render a triangle mesh with a single color </summary>
    SolidColorShader,
}

internal class RenderState 
{
    public static IPlatform? currentPlatform;

    readonly internal IPlatform platform;
    IRenderer? renderer;
    string guid = Guid.NewGuid().ToString();
    bool _renderGizmos = true;
    IBackendRenderBuffer uiRenderBuffer;
    FontCache _fr;
    ITypeSetter ts = new FreetypeSetting();
    bool mouseLeft;
    bool mouseRight;
    Vector2 mousePos;
    float scrollValue;
    float scrollDelta;
    List<SceneView> views = new List<SceneView>();
    Vector2 debugCamRot;
    System.Diagnostics.Stopwatch sw = new();
    WorldSnapshot? currentScene;

    public ColoredTriangleMeshGeometry? cubeGeometry;
    public bool overrideCamera = false;
    public PerspectiveCameraState? debugCamera;
    public Imgui imgui;

    public delegate void OnUpdateDelegate(double dt);
    public OnUpdateDelegate? OnUpdate;
    public delegate void OnRenderSceneDelegate();
    public delegate void OnEndRenderSceneDelegate(IEnumerable<SceneView> views);
    public OnRenderSceneDelegate? OnPreRender;
    public OnEndRenderSceneDelegate? OnPostRender;

    public RenderState(IPlatform platform) {
        currentPlatform = platform;
        this.platform = platform;
        uiRenderBuffer = new DepthPeelRenderBuffer(platform, platform.PresentedColorSpace, false);

        platform.OnSizeChanged += UpdateSize;
        platform.OnLoaded += Load;
        platform.mouseDown += mouseDown;
        platform.mouseUp += mouseUp;
        platform.mouseMove += mouseMove;
        platform.mouseScroll += mouseScroll;
        platform.PRenderFrame += RenderFrame;

        imgui = new Imgui((int)uiRenderBuffer.Size.Item1, (int)uiRenderBuffer.Size.Item2, platform);

        _fr = new FontCache(ts, platform);
    }

    public bool RenderGizmos {
        set {
            _renderGizmos = value;
        }
    }

    public AnimationPlayer.FrameStatus frameStatus;
    public AnimationPlayer.FrameStatus SceneStatus {
        get {
            return frameStatus;
        }
        set {
            frameStatus = value;
        }
    }

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
        }
    }

    public void Load(object? sender, EventArgs args) {
        CreateMeshes();

        var glPlatform = platform as OpenTKPlatform;
        if(glPlatform == null) {
            throw new Exception("GlWorldRenderer currently requires OpenTKPlatform");
        }
        renderer = new GlWorldRenderer(glPlatform, this);
        //renderer = new TessallationRenderer(platform as OpenTKPlatform, this);
        Debug.TLog($"Renderer implementation: {renderer}");
        uiRenderBuffer.Resize(1024, 1024);

        platform.PKeyDown += (object? sender, KeyboardKeyEventArgs args) => {
            if(args.Key == Key.F && !args.IsRepeat) {
                overrideCamera = !overrideCamera;
                if(overrideCamera) {
                    debugCamera = new PerspectiveCameraState();
                    debugCamera.position.z = -13.0f;
                } else {
                    if (debugCamera != null) {
                        debugCamera.position = Vector3.ZERO;
                    }
                    this.debugCamRot = Vector2.ZERO;
                }
            }
            Imgui.KeyEdge((uint)args.Key, true);
        };
        platform.PKeyUp += (object? sender, KeyboardKeyEventArgs args) => {
            Imgui.KeyEdge((uint)args.Key, false);
        };
        platform.PKeyPress += (object? sender, KeyPressEventArgs args) => {
            Imgui.AddInputCharacter(args.KeyChar);
        };

        this.OnUpdate += (double dtd) => {
            float dt = (float)dtd;
            var kstate = Keyboard.GetState();
            if(overrideCamera && debugCamera != null) {
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

        platform.mouseMove += (object? s, MouseMoveEventArgs args) => {
            var state = OpenTK.Input.Keyboard.GetState();
            if(overrideCamera && !state[OpenTK.Input.Key.ControlLeft]) {
                this.debugCamRot.x += args.XDelta*0.01f;
                this.debugCamRot.y += args.YDelta*0.01f;
                this.debugCamRot.x %= 2*MathF.PI;
                this.debugCamRot.y %= 2*MathF.PI;
                var qx = Quaternion.AngleAxis(debugCamRot.x, Vector3.UP);
                var qy = Quaternion.AngleAxis(debugCamRot.y, Vector3.RIGHT);
                if (debugCamera != null) {
                    debugCamera.rotation = qy * qx;
                }
            }
        };
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

    private void mouseDown(object? sender, MouseButtonEventArgs args) {
        if(args.Button == MouseButton.Left) {
            mouseLeft = true;
        }
        if(args.Button == MouseButton.Right) {
            mouseRight = true;
        }
    }

    private void mouseUp(object? sender, MouseButtonEventArgs args) {
        if(args.Button == MouseButton.Left) {
            mouseLeft = false;
        }
        if(args.Button == MouseButton.Right) {
            mouseRight = false;
        }
    }

    private void mouseMove(object? sender, MouseMoveEventArgs args) {
        mousePos = new Vector2(args.Position.X, args.Position.Y);
    }

    private void mouseScroll(object? sender, MouseWheelEventArgs args) {
        scrollValue = args.ValuePrecise;
        scrollDelta = args.DeltaPrecise;
    }

    public int GetGuiEntityAtPixel(IBackendRenderBuffer pb, Vector2 pixel) {
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

    public void SetScene(WorldSnapshot ss) {
        currentScene = ss;
    }

    bool wasOverridden = false;

    private void RenderFrame(object? sender, FrameEventArgs args) {
        Performance.BeginFrame();

        // TODO: use actual frame rate
        imgui.Update(uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2, 1.0f/60.0f, mousePos, mouseLeft, mouseRight, false, scrollDelta);
        scrollDelta = 0.0f;

        if(OnPreRender != null) {
            using var _ = new Performance.Call("RenderState.OnPreRender");
            OnPreRender();
        }

        bool sceneUpdated = overrideCamera || wasOverridden || SceneStatus == AnimationPlayer.FrameStatus.New;
        wasOverridden = overrideCamera;
        //bool sceneUpdated = true;
        uiRenderBuffer.Clear();

        platform.ClearBackbuffer(0, 0, uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2);
        
        {
            using var _ = new Performance.Call("BeginFrame views");
            foreach(var view in views) {
                view.BeginFrame();
            }
        }
        if(OnUpdate != null) {
            using var _ = new Performance.Call("RenderState.OnUpdate");
            OnUpdate(args.Time);
        }
        
        // Render scene
        if(currentScene != null && renderer != null)
        {
            using var _ = new Performance.Call("Render views");
            Performance.views = views.Count;
            sw.Restart();
            if(sceneUpdated) {
                foreach(var sv in views) {
                    var sceneCamera = currentScene.Camera;
                    if(overrideCamera) {
                        sceneCamera = debugCamera;
                    }
                    if (sceneCamera == null) {
                        Debug.Warning("No camera in scene");
                        continue;
                    }
                    renderer.RenderScene(currentScene, sceneCamera, _renderGizmos, out var mainBuffer);
                    sv.Buffer = mainBuffer;
                }
            }
            sw.Stop();
            Performance.TimeToRenderViews = sw.Elapsed.TotalSeconds;
        }

        if(sceneUpdated) {
            using var _ = new Performance.Call("OnPostRender views");
            foreach(var view in views) {
                view.Buffer?.OnPostRender();
            }
        }

        if(OnPostRender != null) {
            using var _ = new Performance.Call("RenderState.OnPostRender");
            OnPostRender(views);
        }

        // Render UI
        {
            using var _ = new Performance.Call("Render UI");
            var drawList = imgui.Render();
            platform.RenderGUI(drawList, views, uiRenderBuffer);
        }

        int sceneEntity = -2;
        if (views.Count > 0) {
            sceneEntity = views[0].GetEntityIdAtPixel(Imgui.GetMousePos());
        }
        var guiEntity = GetGuiEntityAtPixel(uiRenderBuffer, Imgui.GetMousePos());
        if(sceneEntity == -2) { // out of scene viewport
            UserInterface.MouseEntityId = guiEntity;
        } else {
            if (guiEntity == -1)
                UserInterface.MouseEntityId = sceneEntity;
            else
                UserInterface.MouseEntityId = guiEntity;
        }

        Performance.EndFrame();
    }
}
