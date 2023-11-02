using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimLib;

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
        RenderState.currentPlatform.DestroyOwner(hash);
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

public class CanvasSnapshot {
    public CanvasState Canvas;
    public EntityState2D[] Entities;
}

/// <summary>
/// WorldSnapshot is a snapshot of the current world state. Used to render the world in a given point in time.
/// </summary>
public class WorldSnapshot {
    public EntityStateResolver resolver;
    public CameraState Camera;
    public RectangleState[] Rectangles;
    public CubeState[] Cubes;
    public GlyphState[] Glyphs;
    public ColoredTriangleMesh[] Meshes;
    public MeshBackedGeometry[] MeshBackedGeometries;
    public (LabelState, EntityState)[] Labels;
    public BezierState[] Beziers;
    public CanvasSnapshot[] Canvases;
    public double Time;
}

public class EntityResolver {
    public Func<int, VisualEntity> GetEntity;
}

/// <summary>
/// World is a container for all entities and cameras present in the current world instance. Tracks changes made to all containing state and turns the changes into commands that can be executed in the <c>WorldMachine</c>.
/// </summary>
public class World
{
    static int worldId = 0;
    [ThreadStatic]
    static int entityId = 1;
    [ThreadStatic]
    public static World current;

    AnimationSettings settings;

    List<WorldCommand> _commands = new List<WorldCommand>();
    List<WorldSoundCommand> _soundCommands = new List<WorldSoundCommand>();
    List<Label> _labels = new List<Label>();
    List<Func<VisualEntity, bool>> CreationListeners = new List<Func<VisualEntity, bool>>();

    // used by EntityCollection to keep track of children
    private Dictionary<int, List<VisualEntity>> _children = new ();
    private Dictionary<int, VisualEntity> _parents = new ();

    private Dictionary<int, VisualEntity> _entities = new Dictionary<int, VisualEntity>();

    public ITypeSetter ts = new FreetypeSetting();
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

    public World(AnimationSettings settings) {
        this.settings = settings.Clone();
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
        _entities.Clear();
        _commands.Clear();
        _labels.Clear();
        var cam = new PerspectiveCamera();
        cam.Fov = 60.0f;
        cam.ZNear = 0.1f;
        cam.ZFar = 1000.0f;
        cam.Transform.Pos = new Vector3(0.0f, 0.0f, -13.0f);

        var screenCam = new OrthoCamera();
        screenCam.Width = settings.Width;
        screenCam.Height= settings.Height;
        CreateInstantly(screenCam);

        var defaultCanvas = new Canvas(CanvasState.DEFAULTNAME, screenCam);
        CreateInstantly(defaultCanvas);
        Canvas.Default = defaultCanvas;

        CreateInstantly(cam);
        ActiveCamera = cam;
        EndEditing();
    }

    internal Canvas FindCanvas(string name) {
        foreach(var ent in _entities) {
            if(ent.Value is Canvas) {
                var canvas = ent.Value as Canvas;
                var state = canvas.state as CanvasState;
                if(state.name == name) {
                    return canvas;
                }
            }
        }
        return null;
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

    public delegate void OnPropertyChangedD(VisualEntity ent, string prop, object newValue);
    public event OnPropertyChangedD OnPropertyChanged;

    public void SetProperty<T>(VisualEntity entity, string propert, T value, T oldvalue) {
        if(value.Equals(oldvalue))
            return;
        if(entity.created) {
            if(OnPropertyChanged != null) {
                OnPropertyChanged(entity, propert, value);
            }
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

    public void SetPropertyMulti<T>(IEnumerable<VisualEntity> entity, string propert, T value, T[] oldvalues) {
        var ents = entity.Zip(oldvalues, (f, s) => (f, s)).Where(x => x.f.created);
        var cmd = new WorldPropertyMultiCommand {
            entityIds = ents.Select(x => x.f.EntityId).ToArray(),
            time = Time.T,
            property = propert,
            newvalue = value,
            oldvalue = ents.Select(x => (object)x.s).ToArray(),
        };
        _commands.Add(cmd);
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
        CheckDependantEntities(entity);
    }

    private void CheckDependantEntities(VisualEntity newent) {
        for(int i = CreationListeners.Count - 1; i >= 0; i--) {
            var wd = CreationListeners[i];
            if(wd.Invoke(newent)) {
                CreationListeners.RemoveAt(i);
            }
        }
    }

    internal void AttachChild(VisualEntity parent, VisualEntity child)
    {
        if (!_children.ContainsKey(parent.EntityId))
        {
            _children.Add(parent.EntityId, new List<VisualEntity>());
        }
        child.managedLifetime = true;
        _children[parent.EntityId].Add(child);
        _parents.Add(child.EntityId, parent);
    }

    internal void DetachChild(VisualEntity parent, VisualEntity child)
    {
        if (!_children.ContainsKey(parent.EntityId))
        {
            Debug.Error("Parent does not have any children");
            return;
        }
        child.managedLifetime = false;
        _children[parent.EntityId].Remove(child);
        _parents.Remove(child.EntityId);
    }

    // when entity is created the Func is invoked, if the Func returns true it is deleted
    // useful for listening for dependencies
    internal T MatchCreation<T>(T ent, Func<VisualEntity, bool> match) where T : VisualEntity{
        CreationListeners.Add(match);
        // check if the entity already exists
        foreach(var dent in _entities.Values.ToList()) {
            CheckDependantEntities(dent);
        }
        return ent;
    }

    public T CreateInstantly<T>(T ent) where T : VisualEntity {
        EntityCreated(ent);
        return ent;
    } 

    public Task CreateFadeIn<T>(T entity, float duration) where T : VisualEntity,IColored {
        CreateInstantly(entity);
        var c = entity.Color;
        var alpha = c.a;
        return Animate.InterpT<float>(x => {
                c.a = (byte)Math.Round(x*((float)alpha));
                entity.Color = c;
            }, 0.0f, 1.0f, duration);
    }

    public Task CreateFadeIn<T>(T entity, Color startColor, Action<Color> setColor, float duration) where T : VisualEntity,IColored {
        CreateInstantly(entity);
        var c = startColor;
        var alpha = c.a;
        return Animate.InterpT<float>(x => {
                c.a = (byte)Math.Round(x*((float)alpha));
                setColor(c);
            }, 0.0f, 1.0f, duration);
    }

    public async Task DestroyFadeOut<T>(T entity, float duration) where T : VisualEntity, IColored {
        var c = entity.Color;
        await Animate.InterpT<float>(x => {
                c.a = (byte)Math.Round((1.0f-x)*c.a);
                entity.Color = c;
            }, 0.0f, 1.0f, duration);
        Destroy(entity);
    }

    /*public async Task DestroyFadeOut<T>(T entity, float duration) where T : Shape {
        var c = entity.Color;
        var cc = entity.ContourColor;
        await Animate.InterpT<float>(x => {
                c.a = (byte)Math.Round((1.0f-x)*c.a);
                cc.a = (byte)Math.Round((1.0f-x)*cc.a);
                entity.Color = c;
                entity.ContourColor = cc;
            }, 0.0f, 1.0f, duration);
        Destroy(entity);
    }*/

     public T Clone<T>(T e) where T : VisualEntity {
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

        if (obj.managedLifetime)
        {
            Debug.Error("Attempting to destroy managed entity. Detach the entity from it's parent EntityCollection first.");
            return;
        }

        // destroy all children
        if(_children.ContainsKey(obj.EntityId)) {
            var localC = _children[obj.EntityId].ToArray();
            foreach(var child in localC) {
                DetachChild(obj, child);
                Destroy(child);
            }
        }
        var cmd = new WorldDestroyCommand() {
            time = Time.T,
            entityId = obj.EntityId,
        };
        obj.created = false;
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
