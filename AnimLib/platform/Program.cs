﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Loader;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace AnimLib;

class SyncCtx : SynchronizationContext {
    static ConcurrentBag<(SendOrPostCallback, object)> postedCallbacks = new ConcurrentBag<(SendOrPostCallback, object)>();
    public override void Post(SendOrPostCallback d, object state) {
        postedCallbacks.Add((d, state));
    }
    public override void Send(SendOrPostCallback d, object state) {
        throw new NotImplementedException();
    }

    public void InvokeAllPosted() {
        (SendOrPostCallback, object) d;
        while(postedCallbacks.TryTake(out d)) {
            d.Item1.Invoke(d.Item2);
        }
    }
}

/// <summary>
/// The entry point of the application.
/// </summary>
internal class Program
{
    static PlayerControls pctrl;
    static AnimationPlayer player;

    static SyncCtx mainCtx = new SyncCtx();

    static void AssemblyPathChanged(string newpath)
    {
        Console.WriteLine($"Assembly path changed to {newpath}");
        var behaviour = LoadAndWatchBehaviour(newpath);
        player.SetBehaviour(behaviour);
    }

    [STAThread]
    static void OnChanged(object sender, FileSystemEventArgs args) {
        AnimationBehaviour plugin = null;
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

    static AnimationBehaviour LoadBehaviour(string fullpath) {
        System.Console.WriteLine($"Trying to load animation assembly {fullpath}");
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
                watcher.EnableRaisingEvents = true;
                System.Console.WriteLine($"Animation behaviour loaded");
                return (AnimationBehaviour)instance;
            }
        }
        catch (Exception e) {
            Debug.Error($"Exception when loading behaviour: {e}\nUsing empty behaviour");
            watcher.EnableRaisingEvents = true;
            return new EmptyBehaviour();
        }
    }

    static AnimationBehaviour LoadAndWatchBehaviour(string fullpath) {
        if (watcher != null)
        {
            Debug.Log($"Current behaviour watch disposed");
            watcher.Dispose();
            watcher = null;
        }
        Debug.Log($"Starting new behaviour watch on {fullpath}");
        watcher = new FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(fullpath);
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Filter = Path.GetFileName(fullpath);
        watcher.Changed += OnChanged;
        //watcher.Created += OnChanged;
        return LoadBehaviour(fullpath);
    }

    static FileSystemWatcher watcher;
    static Sound sound;

    [STAThread]
    static void Main(string[] args)
    {
        var platform = new OpenTKPlatform(1024, 1024);
        var renderState = new RenderState(platform);
        sound = new Sound();

        pctrl = new PlayerControls(renderState);
        //player = new AnimationPlayer(plugin, pctrl, world);
        player = new AnimationPlayer(null, pctrl);
        player.ResourceManager.OnAssemblyChanged += AssemblyPathChanged;
        pctrl.SetPlayer(player);

        player.SetBehaviour(new NoProjectBehaviour());
        
        SynchronizationContext.SetSynchronizationContext(mainCtx);

        //var anim = player.BakeAnimation(plugin, 60.0f, 0.0f, 1000.0f);
        //var cam = game.CreateCamera(90.0f, 0.1f, 1000.0f);

        platform.PFileDrop += (object sender, OpenTK.Input.FileDropEventArgs args) => {
            Debug.Log($"DROP FILE {args.FileName}");
            player.FileDrop(args.FileName);
        };

        platform.PKeyUp += (object sender, OpenTK.Input.KeyboardKeyEventArgs args) => {
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
            WorldSnapshot ret;
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
            var frameStatus = player.NextFrame(1.0/refreshRate, out ret);
            i++;
            if(frameStatus == AnimationPlayer.FrameStatus.New) {
                renderState.SetScene(ret);
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

    static void ExHandler(object sender, UnobservedTaskExceptionEventArgs args) {
        throw args.Exception;
    }
}