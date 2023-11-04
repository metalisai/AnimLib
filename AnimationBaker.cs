using System;
using System.Collections.Generic;
using ExceptionDispatchInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo;

namespace AnimLib;

/// <summary>
/// A baked animation that can be played back in the WorldMachine.
/// </summary>
internal class BakedAnimation {
    public bool haveError;
    public string error;
    public string stackTrace;

    public World World;
    public WorldCommand[] Commands;
    public List<Animator.AnimationHandle2D> Handles2D;
    public List<Animator.AnimationHandle3D> Handles3D;
    public float FPS;
    public SoundTrack SoundTrack;
}

/// <summary>
/// Executes the animation behaviour, producing a baked animation that can be rendered using a <c>WorldMachine</c>.
/// </summary>
internal class AnimationBaker {

    ResourceManager resourceManager;

    bool haveError;
    string error;
    string stackTrace;
    System.Threading.Tasks.Task anim = null;
    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    TextPlacement text = new TextPlacement("/usr/share/fonts/truetype/ubuntu/Ubuntu-M.ttf", "Ubuntu");

    private void BakeError(Exception e, World world, Animator animator) {
        world.Reset();
        haveError = true;
        error = $"{e.ToString()} {e.Message}";
        stackTrace = e.StackTrace;
        var behaviour = new ErrorBehaviour();
        Begin(behaviour, world, animator);
        anim = behaviour.Animation(world, animator);
        End(world, animator);
    }

    public AnimationBaker(ResourceManager resourceManager) {
        this.resourceManager = resourceManager;
    }

    protected void Begin(AnimationBehaviour beh, World world, Animator animator)
    {
        world.StartEditing(beh);
        animator.BeginAnimate();
    }

    protected void End(World world, Animator animator)
    {
        world.EndEditing();
        animator.EndAnimate();
    }

    public BakedAnimation BakeAnimation(AnimationBehaviour behaviour1, AnimationSettings settings, AnimationPlayer.PlayerProperties props, PlayerScene scene) {
        var world = new World(settings);
        var animator = new Animator(resourceManager, world, scene, settings, props, text);

        haveError = false;
        error = "";
        stackTrace = "";

        bool dummy = scene == null;

        sw.Restart();

        Time.Reset();
        world.Reset();
        double t = 0.0;
        double dt = 1.0 / settings.FPS;
        double start = 0.0;
        double end = settings.MaxLength;
        if(!dummy) { // scene can be null for dummy behaviour
            lock(scene.sceneLock) {
                scene.LastTime = start;
                scene.UpdateEvents();
                scene.ManageSceneObjects(world);
            }
        }

        var terminator = "default";

        Begin(behaviour1, world, animator);

        anim = null;
        try {
            anim = behaviour1.Animation(world, animator);
            End(world, animator);
        } catch (Exception e) {
            // TODO: print error to user
            Debug.Warning("Exception during baking before first yield");
            End(world, animator);
            BakeError(e, world, animator);
        }

        if(anim.Exception != null) {
            Debug.Warning("Exception during baking after first yield");
            var cap = ExceptionDispatchInfo.Capture(anim.Exception.InnerException);
            BakeError(cap.SourceException, world, animator);
        }
        terminator = $"synchronous (state: {anim.Status} c: {anim.IsCompleted})";

        while(t <= end && !anim.IsCompleted) {
            if(anim.Exception != null) {
                Debug.Warning("Exception in the middle of animation");
                var cap = ExceptionDispatchInfo.Capture(anim.Exception.InnerException);
                BakeError(cap.SourceException, world, animator);
                terminator = "exception";
                break;
            }

            Begin(behaviour1, world, animator);
            Time.NewFrame(dt);
            world.Update(dt);
            End(world, animator);

            if(t >= start) {
                if(!dummy) {
                    lock(scene.sceneLock) {
                        scene.ManageSceneObjects(world);
                    }
                }
            }
            t += dt; 
        }
        if(t > end)
            terminator = "max length";
        else if(anim.IsCompleted) {
            terminator = $"completed (state: {anim.Status} c: {anim.IsCompleted})";
            if(anim.Status == System.Threading.Tasks.TaskStatus.Faulted) {
                var cap = ExceptionDispatchInfo.Capture(anim.Exception.InnerException);
                BakeError(cap.SourceException, world, animator);
            }
        }

        Debug.Log($"Done baking animation, t={t}, end cause: {terminator}");

        var cmds = world.GetCommands();
        var scmds = world.GetSoundCommands();

        var track = new SoundTrack(44100, 1, cmds[cmds.Length-1].time);
        foreach(var scmd in scmds) {
            switch(scmd) {
                case WorldPlaySoundCommand ps:
                    track.PushSample(ps.sound, ps.time, ps.volume);
                    break;
            }
        }

        Debug.Log($"Baked animation has {cmds.Length} commands");

        World.current = null;

        sw.Stop();
        Performance.TimeToBake = sw.Elapsed.TotalSeconds;

        return new BakedAnimation() {
            haveError = haveError,
            error = error,
            stackTrace = stackTrace,
            Commands = cmds,
            FPS = (float)settings.FPS,
            Handles2D = animator.VectorHandles,
            Handles3D = animator.VectorHandles3D,
            SoundTrack = track,
        };
    }

}
