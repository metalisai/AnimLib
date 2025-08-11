using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Loader;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.CommandLine;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AnimLib;

// NOTE: this is needed in case editor kicks off a task that runs on different thread
//  and wants to run completion on main thread (ConfigureAwait(true) which is default)
class SyncCtx : SynchronizationContext
{
    static ConcurrentQueue<(SendOrPostCallback, object?)> postedCallbacks = new();
    public override void Post(SendOrPostCallback d, object? state)
    {
        postedCallbacks.Enqueue((d, state));
    }
    public override void Send(SendOrPostCallback d, object? state)
    {
        throw new NotImplementedException();
    }

    public void InvokeAllPosted()
    {
        while (postedCallbacks.TryDequeue(out var d))
        {
            d.Item1.Invoke(d.Item2);
        }
    }
}

internal class LoadedPluginBehaviour
{
    AnimationBehaviour behaviour;
    // Note the entire purpose of this object is to keep reference to this
    AssemblyLoadContext? context;
    private bool hasLoadCtx;

    public AnimationBehaviour Behaviour => behaviour;
    public bool HasLoadCtx => hasLoadCtx;

    public LoadedPluginBehaviour(AnimationBehaviour behv, AssemblyLoadContext ctx)
    {
        this.behaviour = behv;
        this.context = ctx;
        this.hasLoadCtx = true;
    }

    public LoadedPluginBehaviour(AnimationBehaviour behv)
    {
        this.behaviour = behv;
        this.hasLoadCtx = false;
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
        Debug.Log($"Assembly path changed to {newpath}");
        var behaviour = LoadAndWatchBehaviour(newpath, player);
        if (behaviour != null)
        {
            player.SetBehaviour(behaviour);
        }
    }

    [STAThread]
    static void OnChanged(object sender, FileSystemEventArgs args, AnimationPlayer player)
    {
        LoadedPluginBehaviour? plugin = null;
        Debug.Log("Behaviour changed, trying to reload");
        for (int i = 0; i < 5; i++)
        {
            try
            {
                Thread.Sleep(100);
                plugin= LoadBehaviour(args.FullPath);
                Console.WriteLine("Behaviour changed, reloaded ");
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load behaviour. Reason: " + e.Message);
            }
        }
        if (plugin != null)
        {
            mainCtx.Post(new SendOrPostCallback((o) =>
            {
                player.SetBehaviour(plugin);
                player.SetAnimationDirty(true);
                Console.WriteLine("Behaviour set!");
            }), null);
        }
    }

    internal static LoadedPluginBehaviour? LoadBehaviour(string fullpath)
    {
        System.Console.WriteLine($"Trying to load animation assembly {fullpath}");
        if (watcher == null)
        {
            Debug.Warning($"Behaviour watch not started when loading a behaviour, automatic hot reload will not work.");
        }
        var assemblyLoadContext = new AssemblyLoadContext("asmloadctx", true);
        assemblyLoadContext.Resolving += (ctx, name) =>
        {
            var pluginDir = Path.GetDirectoryName(fullpath);
            if (pluginDir == null)
            {
                return null;
            }
            string depPath = Path.Combine(pluginDir, $"{name.Name}.dll");
            if (File.Exists(depPath))
            {
                return ctx.LoadFromAssemblyPath(depPath);
            }
            return null;
        };

        try
        {
            using var fs = new FileStream(fullpath, FileMode.Open, FileAccess.Read);
            // pdb
            System.Reflection.Assembly asm;
            var pdbFile = fullpath.Replace(".dll", ".pdb");
            if (File.Exists(pdbFile))
            {
                using var fs2 = new FileStream(pdbFile, FileMode.Open, FileAccess.Read);
                asm = assemblyLoadContext.LoadFromStream(fs, fs2);
            }
            else
            {
                asm = assemblyLoadContext.LoadFromStream(fs, null);
            }
            Type[] animPlugins = asm.GetExportedTypes().Where(x => !x.IsInterface && !x.IsAbstract && typeof(AnimationBehaviour).IsAssignableFrom(x)).ToArray();
            if (animPlugins.Length == 0)
            {
                Console.WriteLine($"Assembly did not contain animation behaviour");
                return null;
            }
            var instance = Activator.CreateInstance(animPlugins[0]);
            if (instance == null) return null;
            //assemblyLoadContext.Unload();
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = true;
            }
            Console.WriteLine($"Animation behaviour loaded");
            return new LoadedPluginBehaviour((AnimationBehaviour)instance, assemblyLoadContext);
        }
        catch (Exception e)
        {
            Debug.Error($"Exception when loading behaviour: {e}\nUsing empty behaviour");
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = true;
            }
            return new LoadedPluginBehaviour(new EmptyBehaviour());
        }
    }

    static LoadedPluginBehaviour? LoadAndWatchBehaviour(string fullpath, AnimationPlayer player)
    {
        if (watcher != null)
        {
            Debug.Log($"Current behaviour watch disposed");
            watcher.Dispose();
            watcher = null;
        }
        Debug.Log($"Starting new behaviour watch on {fullpath}");
        var path = Path.GetDirectoryName(fullpath);
        if (path == null)
        {
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

    static void LaunchHeadless(bool useSkiaSoftware = false, string? projectPath = null)
    {
        Debug.Log("Launching headless");
        var platform = new HeadlessGlPlatform();
        Debug.Log("Exiting");
        var renderState = new RenderState(platform);

        var view = new SceneView(0, 0, 100, 100, 1920, 1080);
        renderState.AddSceneView(view);

        AnimationPlayer player = new(new LoadedPluginBehaviour(new NoProjectBehaviour()), useThreads: false);

        player.ResourceManager.OnAssemblyChanged += (path) => AssemblyPathChanged(path, player);

        SynchronizationContext.SetSynchronizationContext(mainCtx);

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


        if (projectPath != null)
        {
            Debug.Log($"Loading project {projectPath}");
            player.ResourceManager.SetProject(projectPath);
        }

        platform.Load();

        //player.Seek(0.0);
        player.Bake();
        var filename = "animation-" + DateTime.Now.ToString("yyyy_MM_dd_HHmmss") + ".mp4";
        player.ExportAnimation(filename, 0.0, null);
        player.Play();

        while (!exit)
        {
            platform.RenderFrame(new FrameEventArgs(1.0 / 60.0));
        }
        player.Close();
        Console.WriteLine($"Application closing. Reason: {exitReason}");
        if (watcher != null) watcher.Dispose();
    }

    static void LaunchEditor(bool useSkiaSoftware = false, string? projectPath = null)
    {
        var platform = new OpenTKPlatform(1024, 1024,
            skiaSoftware: useSkiaSoftware
        );
        var renderState = new RenderState(platform);
        var ui = new UserInterface(platform, renderState);

        AnimationPlayer player = new(new LoadedPluginBehaviour(new NoProjectBehaviour()));
        PlayerControls pctrl = new(renderState, ui, player);


        player.ResourceManager.OnAssemblyChanged += (path) => AssemblyPathChanged(path, player);
        pctrl.SetPlayer(player);

        SynchronizationContext.SetSynchronizationContext(mainCtx);

        platform.PFileDrop += (FileDropEventArgs args) =>
        {
            Debug.Log($"DROP FILE {args.FileNames[0]}");
            player.FileDrop(args.FileNames[0]);
        };

        platform.PKeyUp += (KeyboardKeyEventArgs args) =>
        {
            if (args.Key == Keys.Delete)
            {
                pctrl.Delete();
            }
            if (args.Control && args.Key == Keys.C)
            {
                pctrl.Copy();
            }
            if (args.Control && args.Key == Keys.V)
            {
                pctrl.Paste();
            }
            if (args.Control && args.Key == Keys.S)
            {
                pctrl.Save();
            }
        };

        platform.PRenderFrame += ui.OnUpdate;
        renderState.OnPreRender += ui.OnPreRender;
        renderState.OnPostRender += ui.OnPostRender;

        double refreshRate = 60.0;
        platform.OnDisplayChanged += (int w, int h, double rate) =>
        {
            Debug.Log($"Resolution changed to {w}x{h}@{rate}");
            refreshRate = rate;
        };

        renderState.OnPreRender += () =>
        {
            mainCtx.InvokeAllPosted();

            // render animation handle UI
            foreach (var h in player.GetAnimationHandles())
            {
                if (h.StartTime <= Time.T)
                {
                    bool update;
                    h.Position = pctrl.Show2DHandle(h.Identifier, h.Position, h.Anchor, out update);
                    if (update)
                    {
                        player.Update2DHandle(h.Identifier, h.Position);
                        player.SetAnimationDirty(true);
                        break;
                    }
                }
            }
            int i = 0;
            foreach (var h in player.GetAnimationHandles3D())
            {
                if (h.StartTime <= Time.T)
                {
                    //if((UserInterface.WorldCamera.position - h.Position).Length > 1.0f) {
                    bool update;
                    h.Position = pctrl.Show3DHandle(h.Identifier, h.Position, out update);
                    if (update)
                    {
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
            var frameStatus = player.NextFrame(1.0 / refreshRate, out var ret);
            i++;
            if (frameStatus == AnimationPlayer.FrameStatus.New)
            {
                renderState.SetScene(ret!);
            }
            renderState.SceneStatus = frameStatus;
            renderState.RenderGizmos = !player.Exporting;
        };

        renderState.OnPostRender += player.OnEndRenderScene;

        if (projectPath != null)
        {
            Debug.Log($"Loading project {projectPath}");
            player.ResourceManager.SetProject(projectPath);
        }

        //platform.Run(144.0, 144.0);
        platform.Run();
        player.Close();
        Console.WriteLine("Application closing");
        if (watcher != null) watcher.Dispose();
    }

    [STAThread]
    static void Main(string[] args)
    {
        var skiaSoftwareOption = new Option<bool>(
            "--skia-software",
            "Use SkiaSharp software rendering"
        );
        var projectOption = new Option<string>(
            "--project",
            "Project file to load"
        );
        var rootCommand = new RootCommand("Animation editor");
        rootCommand.AddOption(skiaSoftwareOption);
        skiaSoftwareOption.IsRequired = false;
        rootCommand.AddOption(projectOption);
        projectOption.IsRequired = false;
        rootCommand.SetHandler((useSw, project) =>
        {
            Debug.Log($"Using SkiaSharp software rendering: {useSw}");
            LaunchEditor(useSw, project);
        }, skiaSoftwareOption, projectOption
        );

        var projectOption2 = new Option<string>(
            "--project",
            "Project file to load"
        );
        projectOption2.IsRequired = true;
        var exportCommand = new Command("export", "Export an animation.");
        exportCommand.AddOption(projectOption2);
        exportCommand.AddOption(skiaSoftwareOption);
        exportCommand.SetHandler((useSw, project) =>
        {
            Debug.Log($"Headless export mode. Using SkiaSharp software rendering: {useSw}");
            LaunchHeadless(useSw, project);
        }, skiaSoftwareOption, projectOption2);
        rootCommand.AddCommand(exportCommand);

        var code = rootCommand.Invoke(args);
        Debug.Log($"Root command returned {code}. Exiting");
    }

    static void ExHandler(object sender, UnobservedTaskExceptionEventArgs args)
    {
        throw args.Exception;
    }
}
