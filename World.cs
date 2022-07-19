using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimLib
{
    public enum LabelStyle {
        None,
        Horizontal, // only horizontal orientation allowed
        Free, // can have any orientation
    }
    public interface Labelable {
        Vector2? GetLabelOffset(CameraState cam, Rect label, LabelStyle style, EntityState state, Vector2 screenSize);
        Vector3? GetLabelWorldCoordinate(LabelStyle style, EntityState state);
    }

    public class AbsorbDestruction {
        public int entityId;
        public Vector3? point;
        public Vector3? screenPoint;
        public float duration;
        public float progress;
    }

    public interface IColored {
        Color Color { get; set; }
    }

    public interface RendererResource {
        string GetOwnerGuid();
    }
    public class WorldResources : IDisposable {
        public WorldResources() {
            Debug.Log("New world resources " + GetGuid());
        }
        public List<ColoredTriangleMeshGeometry> MeshGeometries = new List<ColoredTriangleMeshGeometry>();
        public List<Texture2D> Textures = new List<Texture2D>();
        public List<MeshBackedGeometry> MeshBackedGeometries = new List<MeshBackedGeometry>();

        string hash = Guid.NewGuid().ToString();

        public string GetGuid() {
            return hash;
        }

        public void Dispose()
        {
            MeshGeometries = null;
            Textures = null;
            MeshBackedGeometries = null;
            // make sure renderer knows that everything we allocated is no longer needed
            RenderState.destroyedOwners.Add(hash);
            Debug.Log("World resources destroyed " + GetGuid());
        }
    }

    public class ColoredTriangleMeshGeometry : RendererResource {
        public Vector3[] vertices;
        public uint[] indices;
        public Color[] colors;
        public Vector2[] edgeCoordinates;
        public int VAOHandle = -1;
        public int VBOHandle = -1;
        public int EBOHandle = -1;
        public bool Dirty = true;
        // used by renderer to know who owns the resource (to know what can be deallocated)
        public string ownerGuid;

        public ColoredTriangleMeshGeometry(string ownerGuid) {
            this.ownerGuid  = ownerGuid;
        }

        public string GetOwnerGuid()
        {
            return ownerGuid;
        }
    }

    public class ColoredTriangleMesh/* : VisualEntity*/ {
        public RenderState.BuiltinShader Shader = RenderState.BuiltinShader.LineShader;
        public Color Tint = Color.WHITE;
        /*public Color Outline = Color.BLACK;
        public float OutlineWidth = 0.0f;*/
        public M4x4 modelToWorld;
        public ColoredTriangleMeshGeometry Geometry;
        public List<(string, object)> shaderProperties = new List<(string, object)>();
        public bool is2d = false;
        public int entityId = -1;
    }

    public class RendererHandle {
        public ColoredTriangleMeshGeometry Handle;
    }


    public class RendererAnimation {
        public Vector3? point;
        public Vector3? screenPoint;
        public float progress;
    }

    public class WorldSnapshot {
        public EntityStateResolver resolver;
        public CameraState Camera;
        public CircleState[] Circles;
        public RectangleState[] Rectangles;
        public CubeState[] Cubes;
        public TexRectState[] TexRects;
        //public Text2DState[] Texts;
        public GlyphState[] Glyphs;
        public ColoredTriangleMesh[] Meshes;
        public MeshBackedGeometry[] MeshBackedGeometries;
        public (LabelState, EntityState)[] Labels;
        public BezierState[] Beziers;
        public double Time;
    }

    public class EntityResolver {
        public Func<int, VisualEntity> GetEntity;
    }

    public class World
    {
        static int worldId = 0;
        [ThreadStatic]
        static int entityId = 1;
        [ThreadStatic]
        public static World current;

        List<WorldCommand> _commands = new List<WorldCommand>();
        List<WorldSoundCommand> _soundCommands = new List<WorldSoundCommand>();
        List<Label> _labels = new List<Label>();

        private Dictionary<int, VisualEntity> _entities = new Dictionary<int, VisualEntity>();

        public TypeSetting ts = new TypeSetting();
        object currentEditor = null; // who edits things right now (e.g. scene or animationbehaviour)

        public EntityResolver EntityResolver;
        Color background = Color.WHITE;

        public readonly int Id;

        Camera _activeCamera;
        public Camera ActiveCamera {
            get {
                return _activeCamera;
            } set {
                var cmd = new WorldSetActiveCameraCommand() {
                    oldCamEntId = _activeCamera?.EntityId ?? 0,
                    cameraEntId = value?.EntityId ?? 0,
                    time = Time.T,
                };
                _commands.Add(cmd);
                _activeCamera = value;
            }
        }
        
        public void StartEditing(object editor) {
            if(editor == null) {
                Debug.Error("Use EndEditing() instead of passing null");
            }
            if(currentEditor != null)
            {
                Debug.Error("StartEditing() should always have a matchin EndEditing(). Someone forgot to do that!");
                Debug.Error("This isn't critical but helps avoid certain type of bugs");
            }
            this.currentEditor = editor;
        }

        public void EndEditing() {
            this.currentEditor = null;
        }

        public World() {
            current = this;
            EntityResolver = new EntityResolver {
                GetEntity = entid => {
                    return _entities[entid];
                }
            };
            Id = worldId++;
            //this._activeCamera.Position = new Vector3(0.0f, 0.0f, 13.0f);
            Reset();
        }
        List<AbsorbDestruction> removes = new List<AbsorbDestruction>();

        public int GetUniqueId() {
            return entityId++;
        }

        public void Update(double dt) {
            foreach(var label in _labels) {
                LabelState state = ((LabelState)label.state);
                var val = state.target.GetLabelWorldCoordinate(state.style, ((VisualEntity)state.target).state);
                if(val != null){
                    label.state.position = val.Value;
                }
            }
            
            removes.Clear();
        }

        public void PlaySound(SoundSample sound, float volume = 1.0f) {
            var command = new WorldPlaySoundCommand() {
                time = Time.T,
                volume = volume,
                sound = sound,
            };
            _soundCommands.Add(command);
        }

        public void PlaySound(BuiltinSound sound, float volume = 1.0f) {
            PlaySound(SoundSample.GetBuiltin(sound), volume);
        }

        public void Reset() {
            StartEditing(this);
            Resources?.Dispose();
            Resources = new WorldResources();
            _commands.Clear();
            _labels.Clear();
            var cam = new PerspectiveCamera();
            cam.Fov = 60.0f;
            cam.ZNear = 0.1f;
            cam.ZFar = 1000.0f;
            cam.Transform.Pos = new Vector3(0.0f, 0.0f, -13.0f);
            CreateInstantly(cam);
            ActiveCamera = cam;
            EndEditing();
        }

        public VisualEntity FindEntityByCreator(object creator) {
            foreach(var ent in _entities.Values) {
                if(ent.state.creator == creator) {
                    return ent;
                }
            }
            return null;
        }

        public void AddResource(ColoredTriangleMeshGeometry geometry) {
            Resources.MeshGeometries.Add(geometry);
        }

        public void AddResource(Texture2D texture) {
            Resources.Textures.Add(texture);
        }

        public void SetProperty<T>(VisualEntity entity, string propert, T value, T oldvalue) {
            if(value.Equals(oldvalue))
                return;
            if(entity.created) {
                var cmd = new WorldPropertyCommand {
                    entityId = entity.EntityId,
                    time = Time.T,
                    property = propert,
                    newvalue = value,
                    oldvalue = oldvalue,
                };
                _commands.Add(cmd);
            }
        }

        private void EntityCreated(VisualEntity entity) {
            entity.state.entityId = GetUniqueId();
            if(currentEditor == null) {
                Debug.Error("Entity created when no one is editing!? Use StartEditing() before modifying world.");
            }
            entity.state.creator = currentEditor;
            var cmd = new WorldCreateCommand() {
                time = Time.T,
                entity = entity.state.Clone(),
            };
            _commands.Add(cmd);
            entity.created = true;
            _entities.Add(entity.EntityId, entity);
            switch(entity) {
                case Label l1:
                _labels.Add(l1);
                break;
            }
            entity.EntityCreated();
        }

        public T CreateInstantly<T>(T ent) where T : VisualEntity {
            EntityCreated(ent);
            return ent;
        } 

        public Task CreateFadeIn<T>(T entity, float duration) where T : VisualEntity,IColored {
            CreateInstantly(entity);
            var c = entity.Color;
            return Animate.InterpT<float>(x => {
                    c.a = (byte)Math.Round(x*255.0f);
                    entity.Color = c;
                }, 0.0f, 1.0f, duration);
        }

        /*public VisualEntity CloneInstantly(VisualEntity ent) {
            var ret = (VisualEntity)ent.Clone();
            CreateInstantly(ret);
            return ret;
        }*/
        
         public T Clone<T>(T e) where T : VisualEntity, new() {
            var ret = (T)e.Clone();
            return ret;
        }

        public T CreateClone<T>(T e) where T : VisualEntity {
            var ret = (T)e.Clone();
            CreateInstantly(ret);
            return ret;
        }

        public void Destroy(VisualEntity obj) {
            if(!obj.created) return;
            var cmd = new WorldDestroyCommand() {
                time = Time.T,
                entityId = obj.EntityId,
            };
            _commands.Add(cmd);
        }

        public WorldCommand[] GetCommands() {
            return _commands.Concat(new WorldCommand[]{new WorldEndCommand{time = Time.T}}).ToArray();
        }

        public WorldSoundCommand[] GetSoundCommands() {
            return _soundCommands.ToArray();
        }

        public WorldResources Resources;
    }
}
