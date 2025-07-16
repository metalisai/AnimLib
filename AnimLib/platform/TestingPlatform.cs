using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AnimLib;

public class TestingPlatform : IDisposable
{
    HeadlessGlPlatform glPlatform;
    static SyncCtx mainCtx = new();
    List<string> screenshotMarkers = new();

    List<string> pendingScreenshotCaptures = new();

    public delegate void SSDelegate(string markerId, CapturedFrame frame);
    public event SSDelegate? ScreenshotCaptured;

    bool exit = false;
    string exitReason = "None";
    AnimationPlayer? player;
    RenderState? renderState;
    private bool disposedValue;

    public TestingPlatform()
    {
        glPlatform = new HeadlessGlPlatform();
    }

    public void AddScreenshotMarker(string markerName)
    {
        screenshotMarkers.Add(markerName);
    }

    public void ClearMarkers()
    {
        screenshotMarkers.Clear();
    }

    // callback called by renderer (for capturing view images)
    private void OnEndRenderScene(IEnumerable<SceneView> views)
    {
        if (pendingScreenshotCaptures.Count > 0)
        {
            foreach (var markerId in pendingScreenshotCaptures)
            {
                var img = views.First().CaptureScene(Texture2D.TextureFormat.RGB16)!.Value;
                ScreenshotCaptured?.Invoke(markerId, img);
            }
            pendingScreenshotCaptures.Clear();
        }
    }

    private void OnMarker(string id, bool forward)
    {
        if (screenshotMarkers.Contains(id) && forward)
        {
            pendingScreenshotCaptures.Add(id);
        }
    }

    public void Init()
    {
        renderState = new RenderState(glPlatform);

        var view = new SceneView(0, 0, 100, 100, 1920, 1080);
        renderState.AddSceneView(view);

        player = new(new NoProjectBehaviour(), useThreads: false);
        player.OnMarker += OnMarker;
        player.ResourceManager.OnAssemblyChanged += (path) =>
        {
            var beh = Program.LoadBehaviour(path);
            if (beh == null) throw new System.Exception("Failed to load behaviour");
            player.SetBehaviour(beh);
        };
        SynchronizationContext.SetSynchronizationContext(mainCtx);

        renderState.OnPreRender += () =>
        {
            mainCtx.InvokeAllPosted();

            float refreshRate = 60.0f;
            // render editor UI
            var frameStatus = player.NextFrame(1.0 / refreshRate, out var ret);
            if (frameStatus == AnimationPlayer.FrameStatus.New)
            {
                renderState.SetScene(ret!);
            }
            renderState.SceneStatus = frameStatus;

            if (frameStatus == AnimationPlayer.FrameStatus.Still)
            {
                exit = true;
                exitReason = "No more frames.";
            }
        };
        renderState.OnPostRender += player.OnEndRenderScene;
        renderState.OnPostRender += OnEndRenderScene;

        glPlatform.Load();
    }

    public void Run(string projectFilePath)
    {
        if (player == null)
        {
            throw new Exception("Call Init() before running!");
        }

        player.ResourceManager.SetProject(projectFilePath);
        player.Bake();
        player.Play();

        while (!exit)
        {
            glPlatform.RenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        Debug.Log($"Animation done playing. Reason: {exitReason}");
    }

    public void Run(AnimationBehaviour behaviour)
    {
        if (player == null)
        {
            throw new Exception("Call Init() before running!");
        }

        player.SetBehaviour(behaviour);

        player.Bake();
        player.Play();

        while (!exit)
        {
            glPlatform.RenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        Debug.Log($"Animation done playing. Reason: {exitReason}");
    }

    public void Close()
    {
        player?.Close();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                glPlatform.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TestingPlatform()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}