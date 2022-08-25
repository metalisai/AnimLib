using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using ExceptionDispatchInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo;

namespace AnimLib {

    public class BakedAnimation {
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

    // This is the API available to animation
    public class Animator {
        public class AnimationHandle2D {
            public string Identifier;
            public double StartTime;
            public double EndTime;
            public Vector2 Position;
            public Vector2 Anchor;
        }

        public class AnimationHandle3D {
            public string Identifier;
            public double StartTime;
            public double EndTime;
            public Vector3 Position;
        }

        ResourceManager resourceManager;
        World world;
        public PlayerScene Scene;
        AnimationSettings settings;
        AnimationPlayer.PlayerProperties props;
        internal List<AnimationHandle2D> VectorHandles = new List<AnimationHandle2D>();
        internal List<AnimationHandle3D> VectorHandles3D = new List<AnimationHandle3D>();

        internal Animator(ResourceManager resourceManager, World world, PlayerScene scene, AnimationSettings settings, AnimationPlayer.PlayerProperties props) {
            this.resourceManager = resourceManager;
            this.world = world;
            this.settings = settings;
            this.Scene = scene;
            this.props = props;
        }

        public Color GetColor(string name) {
            Color col;
            if(props.Values.ColorMap.TryGetValue(name, out col)) {
                return col;
            } else { 
                return default(Color);
            }
        }

        public Vector2 CreateHandle2D(string name, Vector2 pos, Vector2 anchor = default) {
            string key = settings.Name + "/" + name;
            Vector2 storedPos;
            if(props.VectorHandleMap.TryGetValue(key, out storedPos)) {
                pos = storedPos;
            }
            var handle = new AnimationHandle2D {
                Identifier = key,
                StartTime = Time.T,
                EndTime = Time.T + 1000.0,
                Position = pos,
                Anchor = anchor,
            };
            VectorHandles.Add(handle);
            return pos;
        }

        public Vector3 CreateHandle3D(string name, Vector3 pos) {
            string key = settings.Name + "/" + name;
            Vector3 storedPos;
            if(props.VectorHandleMap3D.TryGetValue(key, out storedPos)) {
                pos = storedPos;
            }
            var handle = new AnimationHandle3D {
                Identifier = key,
                StartTime = Time.T,
                EndTime = Time.T + 1000.0,
                Position = pos,
            };
            VectorHandles3D.Add(handle);
            return pos;
        }

        public Texture2D GetTextureResource(string name) {
            string fileName;
            try {
                using (var res = resourceManager.GetResource(name, out fileName)) {
                    if(res != null) {
                        var ext = Path.GetExtension(fileName);
                        switch(ext.ToLower()) {
                            case ".jpg":
                            case ".png":
                            case ".bmp":
                            case ".gif":
                            case ".exif":
                            case ".tiff":
                            var image = new Bitmap(res);
                            var pxsize = Image.GetPixelFormatSize(image.PixelFormat);
                            System.Drawing.Imaging.PixelFormat outFmt = default;
                            Texture2D.TextureFormat outFmtGl = default;
                            switch(pxsize) {
                                case 8:
                                    outFmt = System.Drawing.Imaging.PixelFormat.Alpha;
                                    outFmtGl = Texture2D.TextureFormat.R8;
                                    break;
                                case 24:
                                    outFmt = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                                    outFmtGl = Texture2D.TextureFormat.BGR8;
                                    break;
                                case 32:
                                    outFmt = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                                    outFmtGl = Texture2D.TextureFormat.ARGB8;
                                    break;
                                default:
                                    System.Console.WriteLine("Unsupported pixel format " + image.PixelFormat);
                                    return null;
                            }
                            var data = image.LockBits(
                                new System.Drawing.Rectangle(new Point(0, 0), new Size(image.Width, image.Height)), 
                                System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                                outFmt
                            );
                            var len = data.Stride * data.Height;

                            byte[] bytes = new byte[len];
                            Marshal.Copy(data.Scan0, bytes, 0, len);
                            image.UnlockBits(data);

                            // TODO: some image formats (and opengl by default) add padding for alignment
                            System.Diagnostics.Debug.Assert(bytes.Length >= image.Width * image.Height * (pxsize/8));

                            var ret = new Texture2D(world.Resources.GetGuid()) {
                                RawData = bytes,
                                Format = outFmtGl,
                                Width = image.Width,
                                Height = image.Height,
                            };
                            world.AddResource(ret);
                            return ret;
                        }
                    }
                    return null;
                }
            } catch (NullReferenceException)
            {
                // TODO: use fallback texture instead of failing?
                System.Diagnostics.Debug.Fail($"Resource {name} doesn't exist or no project loaded!");
                return null;
            }
        }

    }

    class AnimationBaker {

        ResourceManager resourceManager;

        bool haveError;
        string error;
        string stackTrace;
        System.Threading.Tasks.Task anim = null;

        private void BakeError(Exception e, World world, Animator animator) {
            haveError = true;
            error = $"{e.ToString()} {e.Message}";
            stackTrace = e.StackTrace;
            anim = (new ErrorBehaviour()).Animation(world, animator);
        }

        public AnimationBaker(ResourceManager resourceManager) {
            this.resourceManager = resourceManager;
        }

        public BakedAnimation BakeAnimation(AnimationBehaviour behaviour1, AnimationSettings settings, AnimationPlayer.PlayerProperties props, PlayerScene scene) {
            var world = new World(settings);
            var animator = new Animator(resourceManager, world, scene, settings, props);

            haveError = false;
            error = "";
            stackTrace = "";

            bool dummy = scene == null;

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

            world.StartEditing(behaviour1);
            anim = null;
            try {
                anim = behaviour1.Animation(world, animator);
            } catch (Exception e) {
                // TODO: print error to user
                Debug.Warning("Exception during baking before first yield");
                BakeError(e, world, animator);
            }
            world.EndEditing();

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

                world.StartEditing(behaviour1);
                Time.NewFrame(dt);
                world.Update(dt);
                world.EndEditing();

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
}
