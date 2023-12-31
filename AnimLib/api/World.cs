using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace AnimLib;

using CanvasProperties = (string Name, (string Name, object Value)[] Properties);

/// <summary>
/// An interface for entities that can be colored.
/// </summary>
public interface IColored {
    /// <summary>
    /// The (multiplicative) color of the entity.
    /// </summary>
    Color Color { get; set; }
}

internal interface IRendererResource {
    string GetOwnerGuid();
}

internal class WorldResources : IDisposable {
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
        MeshGeometries.Clear();
        Textures.Clear();
        MeshBackedGeometries.Clear();
        // make sure renderer knows that everything we allocated is no longer needed
        RenderState.currentPlatform.DestroyOwner(hash);
        Debug.Log("World resources destroyed " + GetGuid());
    }
}

/// <summary>
/// A solid color triangle mesh.
/// </summary>
public class ColoredTriangleMeshGeometry : IRendererResource {
    /// <summary>
    /// The vertices of the mesh.
    /// </summary>
    public Vector3[] vertices = Array.Empty<Vector3>();
    /// <summary>
    /// The indices referencing the vertices.
    /// </summary>
    public uint[] indices = Array.Empty<uint>();
    /// <summary>
    /// Vertex colors.
    /// </summary>
    public Color[] colors = Array.Empty<Color>();
    /// <summary>
    /// The texture coordinates.
    /// </summary>
    public Vector2[] edgeCoordinates = Array.Empty<Vector2>();

    internal int VAOHandle = -1;
    internal int VBOHandle = -1;
    internal int EBOHandle = -1;
    internal bool Dirty = true;
    // used by renderer to know who owns the resource (to know what can be deallocated)
    internal string ownerGuid;

    internal ColoredTriangleMeshGeometry(string ownerGuid) {
        this.ownerGuid  = ownerGuid;
    }

    string IRendererResource.GetOwnerGuid()
    {
        return ownerGuid;
    }
}

public class ColoredTriangleMesh/* : VisualEntity*/ {
    public BuiltinShader Shader = BuiltinShader.LineShader;
    public Color Tint = Color.WHITE;
    public Color Outline = Color.BLACK;
    /*public Color Outline = Color.BLACK;
    public float OutlineWidth = 0.0f;*/
    public M4x4 modelToWorld;
    public ColoredTriangleMeshGeometry Geometry;
    public List<(string, object)> shaderProperties = new List<(string, object)>();
    public bool is2d = false;
    public int entityId = -1;
}

internal class RendererHandle {
    public ColoredTriangleMeshGeometry Handle;
}

internal class RendererAnimation {
    public Vector3? point;
    public Vector3? screenPoint;
    public float progress;
}

internal class CanvasSnapshot {
    public required CanvasProperties[] Effects;
    public required CanvasState Canvas;
    public required EntityState2D[] Entities;
}

/// <summary>
/// WorldSnapshot is a snapshot of the current world state. Used to render the world in a given point in time.
/// </summary>
internal class WorldSnapshot {
    public required EntityStateResolver resolver;
    public required CameraState? Camera;
    public required RectangleState[] Rectangles;
    public required CubeState[] Cubes;
    public required GlyphState[] Glyphs;
    public required ColoredTriangleMesh[] Meshes;
    public required MeshBackedGeometry[] MeshBackedGeometries;
    public required BezierState[] Beziers;
    public required CanvasSnapshot[] Canvases;
    // NOTE: the first renderbuffer is always the main one
    public required RenderBufferState[] RenderBuffers;
    public double Time;
}

internal class EntityResolver {
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
    static int renderBufferId = 1;
    /// <summary>
    /// Currently active world.
    /// </summary>
    [ThreadStatic]
    public static World current;

    Canvas _activeCanvas;

    /// <summary>
    /// The canvas that entities are created on by default.
    /// </summary>
    public Canvas ActiveCanvas {
        get {
            return _activeCanvas;
        }
        set {
            _activeCanvas = value;
        }
    }

    AnimationSettings settings;

    List<WorldCommand> _commands = new ();
    List<WorldSoundCommand> _soundCommands = new ();
    List<Func<VisualEntity, bool>> CreationListeners = new ();

    // used by EntityCollection to keep track of children
    private Dictionary<int, List<VisualEntity>> _children = new ();
    private Dictionary<int, VisualEntity> _parents = new ();

    private Dictionary<int, VisualEntity> _entities = new ();

    private Dictionary<int, object> _dynamicProperties = new ();

    internal ITypeSetter ts = new FreetypeSetting();
    object? currentEditor = null; // who edits things right now (e.g. scene or animationbehaviour)
    internal EntityResolver EntityResolver;
    Color background = Color.WHITE;

    /// <summary>
    /// Id of the world.
    /// </summary>
    public readonly int Id;

    Camera? _activeCamera;
    /// <summary>
    /// The currently active camera. If null nothing is rendered.
    /// </summary>
    public Camera? ActiveCamera {
        get {
            return _activeCamera;
        } set {
            var cmd = new WorldSetActiveCameraCommand(
                cameraEntId: value?.EntityId ?? 0,
                oldCamEntId: _activeCamera?.EntityId ?? 0,
                time: Time.T
            );
            _commands.Add(cmd);
            _activeCamera = value;
        }
    }

    internal int CreateRenderBuffer(int width, int height, bool main = false) {
        var id = renderBufferId++;
        var cmd = new WorldCreateRenderBufferCommand(
            width: width,
            height: height,
            id: id,
            time: Time.T
        );
        _commands.Add(cmd);
        return id;
    }
    
    internal void StartEditing(object editor) {
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

    internal void EndEditing() {
        this.currentEditor = null;
    }

    internal World(AnimationSettings settings) {
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

    internal int GetUniqueId() {
        return entityId++;
    }

    internal void Update(double dt) {
        /*foreach(var label in _labels) {
            LabelState state = ((LabelState)label.state);
            var val = state.target.GetLabelWorldCoordinate(state.style, ((VisualEntity)state.target).state);
            if(val != null){
                label.state.position = val.Value;
            }
        }*/
        
    }

    /// <summary>
    /// Play a specified <c>SoundSample</c>.
    /// </summary>
    public void PlaySound(SoundSample sound, float volume = 1.0f) {
        var command = new WorldPlaySoundCommand(
            volume: volume,
            sound: sound,
            time: Time.T
        );
        _soundCommands.Add(command);
    }

    /// <summary>
    /// Play a built-in sound.
    /// </summary>
    public void PlaySound(BuiltinSound sound, float volume = 1.0f) {
        PlaySound(SoundSample.GetBuiltin(sound), volume);
    }

    internal void Reset() {
        StartEditing(this);
        renderBufferId = 1;
        Resources?.Dispose();
        Resources = new WorldResources();
        _entities.Clear();
        _commands.Clear();
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

        this.ActiveCanvas = defaultCanvas;

        CreateInstantly(cam);
        ActiveCamera = cam;
        EndEditing();
    }

    internal Canvas? FindCanvas(string name) {
        foreach(var ent in _entities) {
            if(ent.Value is Canvas) {
                var canvas = ent.Value as Canvas;
                var state = canvas?.state as CanvasState;
                if(state != null && state.name == name) {
                    return canvas;
                }
            }
        }
        return null;
    }

    internal VisualEntity? FindEntityByCreator(object creator) {
        foreach(var ent in _entities.Values) {
            if(ent.state.creator == creator) {
                return ent;
            }
        }
        return null;
    }

    internal void AddResource(ColoredTriangleMeshGeometry geometry) {
        Resources.MeshGeometries.Add(geometry);
    }

    internal void AddResource(Texture2D texture) {
        Resources.Textures.Add(texture);
    }

    internal delegate void OnPropertyChangedD(VisualEntity ent, string prop, object? newValue);
    internal event OnPropertyChangedD OnPropertyChanged;

    internal void SetProperty<T>(VisualEntity entity, string propert, T value, T oldvalue) {
        if(value != null && value.Equals(oldvalue))
            return;
        if(entity.created) {
            if(OnPropertyChanged != null) {
                OnPropertyChanged(entity, propert, value);
            }
            var cmd = new WorldPropertyCommand(
                entityId: entity.EntityId,
                property: propert,
                newvalue: value,
                oldvalue: oldvalue,
                time: Time.T
            );
            _commands.Add(cmd);
        }
    }

    internal void SetPropertyMulti<T>(IEnumerable<VisualEntity> entity, string propert, T value, T[] oldvalues) where T : notnull {
        var ents = entity.Zip(oldvalues, (f, s) => (f, s)).Where(x => x.f.created);
        var cmd = new WorldPropertyMultiCommand(
            entityIds: ents.Select(x => x.f.EntityId).ToArray(),
            time: Time.T,
            property: propert,
            newvalue: value,
            oldvalue: ents.Select(x => (object)x.s).ToArray()
        );
        _commands.Add(cmd);
    }

    private void EntityCreated(VisualEntity entity) {
        entity.state.entityId = GetUniqueId();
        if(currentEditor == null) {
            Debug.Error("Entity created when no one is editing!? Use StartEditing() before modifying world.");
        }
        if (currentEditor != null) {
            entity.state.creator = currentEditor;
        }
        else {
            Debug.Warning("Entity created without creator. This is not a problem but might make debugging harder.");
        }
        var cmd = new WorldCreateCommand(
            entity: entity.state.Clone(),
            time: Time.T
        );
        _commands.Add(cmd);
        entity.created = true;
        _entities.Add(entity.EntityId, entity);
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

    internal int CreateDynProperty(object vl) {
        var id = GetUniqueId();
        _dynamicProperties.Add(id, vl);
        var cmd = new WorldCreateDynPropertyCommand(
            propertyId: id,
            value: vl,
            time: Time.T
        );
        _commands.Add(cmd);
        return id;
    }

    internal object? GetDynProperty(int id) {
        _dynamicProperties.TryGetValue(id, out var ret);
        return ret;
    }

    internal void SetDynProperty(int id, object value) {
        var cmd = new WorldDynPropertyCommand(
            entityId: id,
            newvalue: value,
            oldvalue: _dynamicProperties[id],
            time: Time.T
        );
        _commands.Add(cmd);
        _dynamicProperties[id] = value;
    }

    /// <summary>
    /// Create an entity without any animations.
    /// </summary>
    public T CreateInstantly<T>(T ent) where T : VisualEntity {
        EntityCreated(ent);
        return ent;
    } 

    /// <summary>
    /// Create an <c>IColored</c> entity with a fade in animation.
    /// </summary>
    public Task CreateFadeIn<T>(T entity, float duration) where T : VisualEntity,IColored {
        CreateInstantly(entity);
        var c = entity.Color;
        var alpha = c.a;
        return Animate.InterpF(x => {
                c.a = x*alpha;
                entity.Color = c;
            }, 0.0f, 1.0f, duration);
    }

    /// <summary>
    /// Create a 2D shape by tracing it's contour, then fading the fill if it has one.
    /// </summary>
    /// <param name="entity">The shape to create.</param>
    /// <param name="duration">The duration of the whole animation.</param>
    /// <returns></returns>
    /// <typeparam name="T">The type of the shape.</typeparam>
    public async Task CreateTraceAndFade<T>(T entity, float duration) where T : Shape, IColored {
        bool fadeIn = entity.Mode == ShapeMode.FilledContour || entity.Mode == ShapeMode.Filled;
        float traceDuration = fadeIn ? duration*0.5f : duration;
        var oldMode = entity.Mode;
        var c = entity.Color;
        var alpha = c.a;
        entity.Mode = ShapeMode.Contour;
        entity.Trim = (0.0f, 0.0f);
        CreateInstantly(entity);
        await Animate.InterpF(x => {
                entity.Trim = (0.0f, x);
            }, 0.0f, 1.0f, traceDuration);
        entity.Mode = oldMode;
        if (fadeIn) {
            await Animate.InterpF(x => {
                entity.Color = c.WithA(x);
            }, 0.0f, 1.0f, duration*0.5f);
        }
    }

    /// <summary>
    /// Create an <c>IColored</c> entity with a fade in animation. Calls the <c>setcolor</c> function each time the color is updated.
    /// </summary>
    public Task CreateFadeIn<T>(T entity, Color startColor, Action<Color> setColor, float duration) where T : VisualEntity,IColored {
        CreateInstantly(entity);
        var c = startColor;
        var alpha = c.a;
        return Animate.InterpF(x => {
                c.a = MathF.Round(x*alpha);
                setColor(c);
            }, 0.0f, 1.0f, duration);
    }

    /// <summary>
    /// Destroy an <c>IColored</c> entity with a fade out animation.
    /// </summary>
    public async Task DestroyFadeOut<T>(T entity, float duration) where T : VisualEntity, IColored {
        var c = entity.Color;
        await Animate.InterpF(x => {
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

    /// <summary>
    /// Create a clone of an entity. The clone is not immediately created in the world.
    /// </summary>
    public T Clone<T>(T e) where T : VisualEntity {
        var ret = (T)e.Clone();
        return ret;
    }

    /// <summary>
    /// Create a clone of an entity. The clone is immediately created in the world.
    /// </summary>
    public T CreateClone<T>(T e) where T : VisualEntity {
        var ret = (T)e.Clone();
        CreateInstantly(ret);
        return ret;
    }

    /// <summary>
    /// Remove an entity from the world.
    /// </summary>
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
        var cmd = new WorldDestroyCommand(
            entityId: obj.EntityId,
            time: Time.T
        );
        obj.created = false;
        _commands.Add(cmd);
    }

    internal WorldCommand[] GetCommands() {
        var wc = new WorldEndCommand(Time.T);
        return _commands.Concat(new WorldCommand[]{wc}).ToArray();
    }

    internal WorldSoundCommand[] GetSoundCommands() {
        return _soundCommands.ToArray();
    }

    internal WorldResources Resources;
}
