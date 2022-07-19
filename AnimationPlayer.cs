using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace AnimLib {
    public class AnimationPlayer {

        public class AnimationExport {
            public string fileName;
            public double startTime;
            public double endTime;
            public double currentProgress;
            public FfmpegExporter exporter;
        }

        [Serializable]
        internal class PlayerValues {
            public IDictionary<string, Color> ColorMap { get; set; } = new Dictionary<string, Color>();
            public PlayerValues Clone() {
                return new PlayerValues() {
                    ColorMap = new Dictionary<string, Color>(ColorMap),
                };
            }
        };

        [Serializable]
        internal class PlayerProperties {
            public IDictionary<string, Vector2> VectorHandleMap { get; set; } = new Dictionary<string, Vector2>();
            public IDictionary<string, Vector3> VectorHandleMap3D { get; set; } = new Dictionary<string, Vector3>();
            public PlayerValues Values { get; set; }
        };

        object handleLock = new object();
        Dictionary<string, Vector2> VectorHandleMap = new Dictionary<string, Vector2>();
        Dictionary<string, Vector3> VectorHandleMap3D = new Dictionary<string, Vector3>();
        PlayerValues Values = new PlayerValues(); 

        AnimationSettings settings;
        public PlayerScene Scene;

        TrackPlayer trackPlayer;

        volatile BakedAnimation preparedAnimation;

        AnimationBehaviour currentBehaviour;
        volatile BakedAnimation currentAnimation;
        PlayerControls controls;

        internal delegate void BakeD();
        internal delegate void ErrorD(string error, string stackTrace);
        internal event ErrorD OnError;
        internal event BakeD OnAnimationBaked;

        volatile bool mustUpdate = false;
        volatile bool animationDirty = true;
        volatile bool sceneDirty = true;
        volatile bool running = false;

        WorldMachine machine = new WorldMachine();

        internal WorldMachine Machine {
            get {
                return machine;
            }
        }

        public ResourceManager ResourceManager = new ResourceManager();

        AnimationExport export = null;
        
        Thread bakeThread;

        volatile int frameId = 0;
        volatile int setDirtyAt = 0;

        bool paused {
            get {
                return _paused;
            }
            set {
                controls.SetPlaying(!value);
                _paused = value;
            }
        }
        bool _paused = true;

        bool haveProject {
            get {
                return currentBehaviour != null;
            }
        }

        public bool CaptureFrame {
            get {
                return export != null;
            }
        }

        public bool RenderUI {
            get {
                return export == null;
            }
        }

        public void SetBehaviour(AnimationBehaviour behaviour) {
            var settings = new AnimationSettings();
            behaviour.Init(settings);
            this.settings = settings;
            this.currentBehaviour = behaviour;
            Scene = ResourceManager.GetScene();
            DeserializeHandles();
            SetAnimationDirty(true);
        }

        public void BakeProc() {
            AnimationBaker baker = new AnimationBaker(ResourceManager);
            while(running) {
                var beh = currentBehaviour;
                bool settled = (frameId - setDirtyAt) > 15;
                bool shouldUpdate = settled | mustUpdate;
                if (animationDirty && !sceneDirty && beh != null && shouldUpdate) {
                    animationDirty = false;
                    mustUpdate = false;
                    // create clean instance
                    beh = (AnimationBehaviour)Activator.CreateInstance(currentBehaviour.GetType());
                    PlayerProperties props;
                    lock(handleLock) {
                        props = new PlayerProperties() {
                            VectorHandleMap = new Dictionary<string, Vector2>(VectorHandleMap),
                            VectorHandleMap3D = new Dictionary<string, Vector3>(VectorHandleMap3D),
                            Values = Values.Clone(),
                        };
                    }
                    var animation = baker.BakeAnimation(beh, settings, props, Scene);
                    preparedAnimation = animation;
                }
                Thread.Sleep(50);
            }
        }

        public void Close() {
            running = false;
        }

        public AnimationPlayer(string projectPath, PlayerControls ctrl) {
            trackPlayer = new TrackPlayer();
            ctrl.OnPlay += () => {
                Play();
                trackPlayer.Play();
            };
            ctrl.OnStop += () => {
                Stop();
                trackPlayer.Pause();
            };
            ctrl.OnSeek += (double p) => {
                Seek(p);
                trackPlayer.Seek(p);
            };
            controls = ctrl;

            running = true;
            // Create a thread and call a background method   
            bakeThread = new Thread(new ThreadStart(BakeProc));  
            // Start thread  
            bakeThread.Start(); 
        }

        /*public T GetSceneEntityByName<T>(string name) where T : VisualEntity {
            var obj = Scene.GetSceneEntityByName(name);
            return (T)obj;
        }

        public T[] GetSceneEntitiesByName<T>(string pattern) where T : VisualEntity {
            var obj = Scene.GetSceneEntitiesByName(pattern);
            return obj.Cast<T>().ToArray();
        }*/

        public void OnEndRenderScene() {
            if(CaptureFrame) {
                var tex = controls.MainView.CaptureScene();
                FrameCaptured(tex);
            }
        }


        internal void ExportAnimation(string filename, double start, double end)
        {
            Console.WriteLine($"Export animation {filename} from time {start} to {end}.");
            var path = "Videos/"+filename;
            export = new AnimationExport() {
                fileName = path,
                startTime = start,
                endTime = end,
                exporter = new FfmpegExporter()
            };

            machine.SeekSeconds(start);
            var root = Path.GetDirectoryName(path);
            if(!string.IsNullOrEmpty(root)) {
                Directory.CreateDirectory(root);
            }
            export.exporter.Start(path, settings.Width, settings.Height, (int)Math.Round(settings.FPS));
        }

        internal void FrameCaptured(CapturedFrame cap)
        {
            if(export != null) {
                export.exporter.PushData(cap.data);
                Console.WriteLine($"Pushed {cap.data.Length} bytes of data to exporter");
            }
        }

        public void Update2DHandle(string Id, Vector2 newValue) {
            lock(handleLock) {
                Vector2 curVal;
                if(VectorHandleMap.TryGetValue(Id, out curVal)) {
                    VectorHandleMap[Id] = newValue;
                } else {
                    VectorHandleMap.Add(Id, newValue);
                }
                SerializeHandles();
            }
        }

        public void Update3DHandle(string Id, Vector3 newValue) {
            lock(handleLock) {
                Vector3 curVal;
                if(VectorHandleMap3D.TryGetValue(Id, out curVal)) {
                    VectorHandleMap3D[Id] = newValue;
                } else {
                    VectorHandleMap3D.Add(Id, newValue);
                }
                SerializeHandles();
            }
        }

        public void Stop() {
            paused = true;
        }

        public void Play() {
            paused = false;
        }


        public void FileDrop(string filename) {
            ResourceManager.AddResource(filename);
        }

        public void Seek(double progress) {
            /*double totalFrames = this.currentAnimation.Snapshots.Length;
            int setFrame = (int)(totalFrames*progress);
            playbackFrame = setFrame;
            Math.Min(currentAnimation.Snapshots.Length-1, playbackFrame);
            playbackTime = playbackFrame * (1.0 / settings.FPS);
            controls.SetProgress((float)playbackFrame/(float)this.currentAnimation.Snapshots.Length);*/
            machine.Seek(progress);
            controls.SetProgress((float)machine.GetProgress(), machine.GetPlaybackTime());
        }

        // mark that animation needs update
        public void SetAnimationDirty(bool mustUpdate = false) {
            animationDirty = true; 
            sceneDirty = true;
            setDirtyAt = frameId;
            this.mustUpdate = mustUpdate;
        }

        internal PlayerValues GetValues() {
            if(!haveProject) 
                return null;
            return Values;
        }

        public WorldSnapshot NextFrame(double dt) {
            frameId++;

            if(!haveProject)
            {
                return null;
            }

            if(sceneDirty) {
                SerializeHandles();
                sceneDirty = false;
            }

            var prep = preparedAnimation;
            if(prep != null) { // new animation set
                var progress = machine.HasProgram ? machine.GetProgress() : 0.0;
                preparedAnimation = null;

                if(OnAnimationBaked != null) {
                    OnAnimationBaked();
                }
                if(prep.haveError) {
                    if(OnError != null) {
                        OnError(prep.error, prep.stackTrace);
                    }
                    if (currentAnimation == null) {
                        currentAnimation = prep;
                    }                
                } else {
                    currentAnimation = prep;
                }

                DeserializeHandles(); // Load stored handles
                foreach(var handle in prep.Handles2D) {
                    Vector2 pos;
                    lock(handleLock) {
                        if(!VectorHandleMap.TryGetValue(handle.Identifier, out pos)) {
                            VectorHandleMap.Add(handle.Identifier, handle.Position);
                        } else {
                            VectorHandleMap[handle.Identifier] = handle.Position;
                        }
                    }
                }
                foreach(var handle in prep.Handles3D) {
                    Vector3 pos;
                    lock(handleLock) {
                        if(!VectorHandleMap3D.TryGetValue(handle.Identifier, out pos)) {
                            VectorHandleMap3D.Add(handle.Identifier, handle.Position);
                        } else {
                            VectorHandleMap3D[handle.Identifier] = handle.Position;
                        }
                    }
                }
                // NOTE: machine.Reset() sideeffect when setting new program
                machine.SetProgram(currentAnimation.Commands);
                trackPlayer.Track = currentAnimation.SoundTrack;
                trackPlayer.Seek(progress);
                machine.Seek(progress);

                if((controls.MainView.BufferWidth != settings.Width || controls.MainView.BufferHeight != settings.Height) && controls.MainView.Buffer != null) {
                    controls.MainView.ResizeBuffer(settings.Width, settings.Height);
                }
            }

            if(!machine.HasProgram) {
                return null;
            }

            if(export == null) {
                if(!paused) {
                    paused = machine.Step(dt);
                }
                var progress = machine.GetProgress();
                controls.SetProgress((float)progress, machine.GetPlaybackTime());
                return machine.GetWorldSnapshot();
            } else {
                machine.Step(1.0 / (double)settings.FPS);
                var endTime = Math.Min(export.endTime, machine.GetEndTime());
                Debug.Log($"Time {machine.GetPlaybackTime()}/{endTime}");
                var frame = machine.GetWorldSnapshot();
                if(machine.GetPlaybackTime() >= endTime) {
                    export.exporter.Stop();
                    var sound = currentAnimation.SoundTrack;
                    var count = Math.Min(sound.samples[0].Length, (int)Math.Round(export.endTime * sound.sampleRate));
                    var samples = sound.samples[0].Take(count).ToArray();
                    export.exporter.AddAudio(export.fileName, samples, sound.sampleRate);
                    export = null;
                }
                return frame;
            }
        }

        public void SaveProject() {
            SerializeHandles();
            ResourceManager.SaveScene(Scene);
        }

        private void SerializeHandles() {
            lock(handleLock) {
                var data = new PlayerProperties() {
                    VectorHandleMap = VectorHandleMap,
                    VectorHandleMap3D = VectorHandleMap3D,
                    Values = Values,
                };
                ResourceManager.SaveProperties(data);
                Debug.Log("AnimationPlayer properties serialized");
            }
        }

        private void DeserializeHandles() {
            lock(handleLock) {
                var props = ResourceManager.GetProperties();
                if(props == null) // dummy behaviour most likely
                    return;
                VectorHandleMap = new Dictionary<string, Vector2>(props.VectorHandleMap);
                VectorHandleMap3D = new Dictionary<string, Vector3>(props.VectorHandleMap3D);
                Values = props.Values ?? new PlayerValues();
                Debug.Log("AnimationPlayer properties deserialized");
            }
        }

        public List<Animator.AnimationHandle2D> GetAnimationHandles() {
            lock(handleLock) {
                if(!haveProject)
                    return new List<Animator.AnimationHandle2D>();
                return currentAnimation == null ? new List<Animator.AnimationHandle2D>() : currentAnimation.Handles2D;
            }
        }
        public List<Animator.AnimationHandle3D> GetAnimationHandles3D() {
            lock(handleLock) {
                if(!haveProject)
                    return new List<Animator.AnimationHandle3D>();
                return currentAnimation == null ? new List<Animator.AnimationHandle3D>() : currentAnimation.Handles3D;
            }
        }

    }

}
