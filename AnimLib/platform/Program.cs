using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Loader;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.CommandLine;

namespace AnimLib;

class SyncCtx : SynchronizationContext {
    static ConcurrentBag<(SendOrPostCallback, object?)> postedCallbacks = new ();
    public override void Post(SendOrPostCallback d, object? state) {
        postedCallbacks.Add((d, state));
    }
    public override void Send(SendOrPostCallback d, object? state) {
        throw new NotImplementedException();
    }

    public void InvokeAllPosted() {
        while(postedCallbacks.TryTake(out var d)) {
            d.Item1.Invoke(d.Item2);
        }
    }
}

/// <summary>
/// The entry point of the application.
/// </summary>
internal class Program
{
    static SyncCtx mainCtx = new SyncCtx();

    static void AssemblyPathChanged(string newpath, AnimationPlayer player)
    {
        Console.WriteLine($"Assembly path changed to {newpath}");
        var behaviour = LoadAndWatchBehaviour(newpath, player);
        player.SetBehaviour(behaviour);
    }

    [STAThread]
    static void OnChanged(object sender, FileSystemEventArgs args, AnimationPlayer player) {
        AnimationBehaviour? plugin = null;
        Debug.Log("Behaviour changed, trying to reload");
        for(int i = 0; i < 5; i++) {
            try {
                Thread.Sleep(100);
                plugin = LoadBehaviour(args.FullPath);
                Console.WriteLine("Behaviour changed, reloaded ");
                break;
            } catch (Exception e) {
                Console.WriteLine("Failed to load behaviour. Reason: " + e.Message);
            }
        }
        if(plugin != null) {
            mainCtx.Post(new SendOrPostCallback((o) => { 
                player.SetBehaviour(plugin);
                player.SetAnimationDirty(true);
                Console.WriteLine("Behaviour set!");
            }), null);
        }
    }

    static AnimationBehaviour? LoadBehaviour(string fullpath) {
        System.Console.WriteLine($"Trying to load animation assembly {fullpath}");
        if (watcher == null)
        {
            Debug.Warning($"Behaviour watch not started when loading a behaviour, automatic hot reload will not work.");
        }
        var assemblyLoadContext = new AssemblyLoadContext("asmloadctx", true);
        try {
            using (var fs = new FileStream(fullpath, FileMode.Open, FileAccess.Read))
            {

                // pdb
                System.Reflection.Assembly asm;
                using(var fs2 = new FileStream(fullpath.Replace(".dll", ".pdb"), FileMode.Open, FileAccess.Read)) {
                    asm = assemblyLoadContext.LoadFromStream(fs, fs2);
                }
                Type[] animPlugins = asm.GetExportedTypes().Where(x => !x.IsInterface && !x.IsAbstract && typeof(AnimationBehaviour).IsAssignableFrom(x)).ToArray();
                if(animPlugins.Length == 0) {
                    System.Console.WriteLine($"Assembly did not contain animation behaviour");
                    return null;
                }
                var instance = Activator.CreateInstance(animPlugins[0]);
                //assemblyLoadContext.Unload();
                if (watcher != null) {
                    watcher.EnableRaisingEvents = true;
                }
                System.Console.WriteLine($"Animation behaviour loaded");
                return instance as AnimationBehaviour;
            }
        }
        catch (Exception e) {
            Debug.Error($"Exception when loading behaviour: {e}\nUsing empty behaviour");
            if (watcher != null) {
                watcher.EnableRaisingEvents = true;
            }
            return new EmptyBehaviour();
        }
    }

    static AnimationBehaviour? LoadAndWatchBehaviour(string fullpath, AnimationPlayer player) {
        if (watcher != null)
        {
            Debug.Log($"Current behaviour watch disposed");
            watcher.Dispose();
            watcher = null;
        }
        Debug.Log($"Starting new behaviour watch on {fullpath}");
        var path = Path.GetDirectoryName(fullpath);
        if (path == null) { 
            Debug.Error($"Could not get directory name of {fullpath}");
            return null;
        }
        watcher = new FileSystemWatcher();
        watcher.Path = path;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Filter = Path.GetFileName(fullpath);
        watcher.Changed += (s, args) => OnChanged(s, args, player);
        //watcher.Created += OnChanged;
        return LoadBehaviour(fullpath);
    }

    static FileSystemWatcher? watcher;

    static void LaunchEditor(bool useSkiaSoftware = false) {
        var platform = new OpenTKPlatform(1024, 1024, 
            skiaSoftware: useSkiaSoftware
        );
        var renderState = new RenderState(platform);

        AnimationPlayer player = new (new NoProjectBehaviour());
        PlayerControls pctrl = new (renderState, player);


        player.ResourceManager.OnAssemblyChanged += (path) => AssemblyPathChanged(path, player);
        pctrl.SetPlayer(player);
        
        SynchronizationContext.SetSynchronizationContext(mainCtx);

        platform.PFileDrop += (object? sender, OpenTK.Input.FileDropEventArgs args) => {
            Debug.Log($"DROP FILE {args.FileName}");
            player.FileDrop(args.FileName);
        };

        platform.PKeyUp += (object? sender, OpenTK.Input.KeyboardKeyEventArgs args) => {
            if(args.Key == OpenTK.Input.Key.Delete) {
                pctrl.Delete();
            }
            if(args.Control && args.Key == OpenTK.Input.Key.C) {
                pctrl.Copy();
            }
            if(args.Control && args.Key == OpenTK.Input.Key.V) {
                pctrl.Paste();
            }
            if(args.Control && args.Key == OpenTK.Input.Key.S) {
                pctrl.Save();
            }
        };

        double refreshRate = 60.0;
        platform.OnDisplayChanged += (int w, int h, double rate) => {
            Debug.Log($"Resolution changed to {w}x{h}@{rate}");
            refreshRate = rate;
        };

        renderState.OnPreRender += () => {
            mainCtx.InvokeAllPosted();

            // render animation handle UI
            foreach(var h in player.GetAnimationHandles()) {
                if(h.StartTime <= Time.T) {
                    bool update;
                    h.Position = pctrl.Show2DHandle(h.Identifier, h.Position, h.Anchor, out update);
                    if(update) {
                        player.Update2DHandle(h.Identifier, h.Position);
                        player.SetAnimationDirty(true);
                        break;
                    }
                }
            }
            int i = 0;
            foreach(var h in player.GetAnimationHandles3D()) {
                if(h.StartTime <= Time.T) {
                    //if((UserInterface.WorldCamera.position - h.Position).Length > 1.0f) {
                        bool update;
                        h.Position = pctrl.Show3DHandle(h.Identifier, h.Position, out update);
                        if(update) {
                            player.Update3DHandle(h.Identifier, h.Position);
                            player.SetAnimationDirty(true);
                            break;
                        }
                    //}
                }
                i++;
            }

            // render editor UI
            pctrl.DoInterface();
            var frameStatus = player.NextFrame(1.0/refreshRate, out var ret);
            i++;
            if(frameStatus == AnimationPlayer.FrameStatus.New) {
                renderState.SetScene(ret!);
            }
            renderState.SceneStatus = frameStatus;
            renderState.RenderGizmos = !player.Exporting;
        };
           
        renderState.OnPostRender += player.OnEndRenderScene;

        platform.Run(144.0, 144.0);
        player.Close();
        Console.WriteLine("Application closing");
        if(watcher != null) watcher.Dispose();
    }

    [STAThread]
    static void Main(string[] args)
    {
        // Please don't let interns write system libraries
        // Why is System.CommandLine so bad?
        // Something this trivial should be intuitive to write
        // Don't force me to read the docs
        // Don't force me to use your control flow
        // Just parse the damn args and give me the values
        //
        var skiaSoftwareOption = new Option<bool>(
            "--skia-software", 
            "Use SkiaSharp software rendering"
        );
        var rootCommand = new RootCommand("Animation editor");
        rootCommand.AddOption(skiaSoftwareOption);
        skiaSoftwareOption.IsRequired = false;
        rootCommand.SetHandler( (useSw) => {
                Debug.Log($"Using SkiaSharp software rendering: {useSw}");
                LaunchEditor(useSw);
            }, skiaSoftwareOption
        );
        var code = rootCommand.Invoke(args);
        Debug.Log($"Root command returned {code}. Exiting");
    }

    static void ExHandler(object sender, UnobservedTaskExceptionEventArgs args) {
        throw args.Exception;
    }
}
