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

        public static Animator Current { get; internal set; }

        ResourceManager resourceManager;
        World world;
        public PlayerScene Scene;
        AnimationSettings settings;
        AnimationPlayer.PlayerProperties props;
        internal List<AnimationHandle2D> VectorHandles = new List<AnimationHandle2D>();
        internal List<AnimationHandle3D> VectorHandles3D = new List<AnimationHandle3D>();
        TextPlacement textPlacement;

        internal Animator(ResourceManager resourceManager, World world, PlayerScene scene, AnimationSettings settings, AnimationPlayer.PlayerProperties props, TextPlacement text) {
            this.resourceManager = resourceManager;
            this.world = world;
            this.settings = settings;
            this.Scene = scene;
            this.props = props;
            this.textPlacement = text;
        }

        public void BeginAnimate() {
            if (Current != null) {
                throw new Exception("Animator already in use!");
            }
            Current = this;
        }

        public void EndAnimate() {
            Current = null;
        }

        public Color GetColor(string name) {
            Color col;
            if(props.Values.ColorMap.TryGetValue(name, out col)) {
                return col;
            } else { 
                return default(Color);
            }
        }

        public void LoadFont(Stream stream, string fontname) {
            textPlacement.LoadFont(stream, fontname);
        }

        public void LoadFont(string filename, string fontname) {
            textPlacement.LoadFont(filename, fontname);
        }

        public List<(Shape s, char c)> ShapeText(string texts, Vector2 pos, int size, string font = null) {
            return textPlacement.PlaceTextAsShapes(texts, pos, size, font);
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
        
        public SoundSample? GetSoundResource(string name) {
            string fileName;
            try {
                using (var res = resourceManager.GetResource(name, out fileName)) {
                    var sample = SoundSample.GetFromStream(res);
                    return sample;
                }
            } catch (NullReferenceException) {
                Debug.Error($"Failed to load resource {name}");
            }
            return null;
        }

        public SvgData GetSvgResource(string name) {
            string fileName;
            try {
                using (var res = resourceManager.GetResource(name, out fileName)) {
                    var reader = new StreamReader(res);
                    var data = reader.ReadToEnd();
                    return new SvgData() {
                        handle = -1,
                        svg = data,
                    };
                }
            } catch (NullReferenceException) {
                Debug.Error($"Failed to load SVG resource {name}");
            }
            return null;
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
                                    outFmtGl = Texture2D.TextureFormat.BGRA8;
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
            } catch (Exception e) {
                // TODO: print error to user
                Debug.Warning("Exception during baking before first yield");
                BakeError(e, world, animator);
            }

            End(world, animator);

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
}
