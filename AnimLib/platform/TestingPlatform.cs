using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTK.Windowing.Common;

namespace AnimLib;

public class TestingPlatform
{
    HeadlessGlPlatform glPlatform;
    static SyncCtx mainCtx = new ();
    List<string> screenshotMarkers = new();

    List<string> pendingScreenshotCaptures = new();

    public delegate void SSDelegate(string markerId, CapturedFrame frame);
    public event SSDelegate ScreenshotCaptured;


    public TestingPlatform()
    {
        glPlatform = new HeadlessGlPlatform();
    }

    public void AddScreenshotMarker(string markerName)
    {
        screenshotMarkers.Add(markerName);
    }

    // callback called by renderer (for capturing view images)
    private void OnEndRenderScene(IEnumerable<SceneView> views)
    {
        if (pendingScreenshotCaptures.Count > 0)
        {
            foreach (var markerId in pendingScreenshotCaptures)
            {
                var img = views.First().CaptureScene(Texture2D.TextureFormat.RGB16)!.Value;
                ScreenshotCaptured(markerId, img);
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

    public void Run(string projectFilePath)
    {
        throw new NotImplementedException();
    }

    public void Run(AnimationBehaviour behaviour)
    {
        var renderState = new RenderState(glPlatform);

        var view = new SceneView(0, 0, 100, 100, 1920, 1080);
        renderState.AddSceneView(view);

        AnimationPlayer player = new(new NoProjectBehaviour(), useThreads: false);
        player.OnMarker += OnMarker;
        player.ResourceManager.OnAssemblyChanged += (path) =>
        {
            var beh = Program.LoadBehaviour(path);
            if (beh == null) throw new System.Exception("Failed to load behaviour");
            player.SetBehaviour(beh);
        };
        SynchronizationContext.SetSynchronizationContext(mainCtx);

        // TODO: this code is exactly the same as in Program.LaunchHeadless
        bool exit = false;
        string exitReason = "None";

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

        //player.ResourceManager.SetProject(projectFilePath);
        player.SetBehaviour(behaviour);

        glPlatform.Load();

        //player.Seek(0.0);
        player.Bake();
        player.Play();

        while (!exit)
        {
            glPlatform.RenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        player.Close();
        Console.WriteLine($"Application closing. Reason: {exitReason}");
    }
}