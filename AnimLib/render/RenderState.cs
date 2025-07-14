using System;
using System.Collections.Generic;
using OpenTK.Windowing.Common;

namespace AnimLib;

internal interface IRenderer {
    void RenderScene(WorldSnapshot ss, CameraState cam, bool gizmo, out IBackendRenderBuffer mainBuffer);
    bool BufferValid(IBackendRenderBuffer buf);
}

/// <summary>
/// Legacy shaders, probably will be replaced/removed.
/// </summary>
public enum BuiltinShader
{
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
    /// <summary> Shader to render a textured quad in 3D space. </summary>
    TexturedQuadShader,
}

internal class RenderState 
{
    public static IRendererPlatform? currentPlatform;

    readonly internal IRendererPlatform platform;
    IRenderer? renderer;
    string guid = Guid.NewGuid().ToString();
    bool _renderGizmos = true;
    FontCache _fr;
    ITypeSetter ts = new FreetypeSetting();
    List<SceneView> views = new List<SceneView>();
    System.Diagnostics.Stopwatch sw = new();
    WorldSnapshot? currentScene;

    public ColoredTriangleMeshGeometry? cubeGeometry;

    public delegate void OnUpdateDelegate(double dt);
    public OnUpdateDelegate? OnUpdate;
    public delegate void OnRenderSceneDelegate();
    public delegate void OnEndRenderSceneDelegate(IEnumerable<SceneView> views);
    public OnRenderSceneDelegate? OnPreRender;
    public OnEndRenderSceneDelegate? OnPostRender;
    bool wasOverridden = false;
    public PerspectiveCameraState? OverrideCamera = null;
    
    public IEnumerable<SceneView> Views
    {
        get
        {
            return views;
        }
    }

    public RenderState(IRendererPlatform platform)
    {
        currentPlatform = platform;
        this.platform = platform;


        platform.OnLoaded += Load;
        platform.PRenderFrame += RenderFrame;



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

    public void Load(object? sender, EventArgs args)
    {
        CreateMeshes();

        var renderPlatform = platform as IRendererPlatform;
        if (renderPlatform == null)
        {
            throw new Exception("GlWorldRenderer currently requires IRendererPlatform");
        }
        renderer = new GlWorldRenderer(renderPlatform, this);
        //renderer = new TessallationRenderer(platform as OpenTKPlatform, this);
        Debug.TLog($"Renderer implementation: {renderer}");
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

    public bool SceneDirty = true;
    
    private void CreateMeshes()
    {
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

    private void RenderFrame(FrameEventArgs args)
    {
        Performance.BeginFrame();

        if (OnPreRender != null)
        {
            using var _ = new Performance.Call("RenderState.OnPreRender");
            OnPreRender();
        }

        bool sceneUpdated = OverrideCamera != null || wasOverridden || SceneStatus == AnimationPlayer.FrameStatus.New;
        //bool sceneUpdated = true;
        wasOverridden = OverrideCamera != null;

        {
            using var _ = new Performance.Call("BeginFrame views");
            foreach (var view in views)
            {
                view.BeginFrame();
            }
        }
        if (OnUpdate != null)
        {
            using var _ = new Performance.Call("RenderState.OnUpdate");
            OnUpdate(args.Time);
        }

        // Render scene
        if (currentScene != null && renderer != null)
        {
            using var _ = new Performance.Call("Render views");
            Performance.views = views.Count;
            sw.Restart();
            if (SceneDirty || SceneStatus == AnimationPlayer.FrameStatus.New)
            {
                foreach (var sv in views)
                {
                    var sceneCamera = currentScene.Camera;
                    if (OverrideCamera != null)
                    {
                        sceneCamera = OverrideCamera;
                    }
                    if (sceneCamera == null)
                    {
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

        if (sceneUpdated)
        {
            using var _ = new Performance.Call("OnPostRender views");
            foreach (var view in views)
            {
                view.Buffer?.OnPostRender();
            }
        }

        if (OnPostRender != null)
        {
            using var _ = new Performance.Call("RenderState.OnPostRender");
            OnPostRender(views);
        }

        Performance.EndFrame();

        SceneDirty = false;
    }
}
