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

    string hash = Guid.NewGuid().ToString();

    public string GetGuid() {
        return hash;
    }

    public void Dispose()
    {
        MeshGeometries.Clear();
        Textures.Clear();
        // make sure renderer knows that everything we allocated is no longer needed
        RenderState.currentPlatform?.DestroyOwner(hash);
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

    internal MeshVertexMode vertexMode = MeshVertexMode.Triangles;

    internal int copiedVertices = 0;
    internal int copiedIndices = 0;
    internal int copiedColors = 0;

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

/// <summary>
/// A colored triangle mesh.
/// </summary>
public class ColoredTriangleMesh {
    /// <summary> Shader to use for rendering. </summary>
    public BuiltinShader Shader = BuiltinShader.LineShader;
    /// <summary> The color of the mesh. </summary>
    public Color Tint = Color.WHITE;
    /// <summary> The homogeneous transformation matrix. </summary>
    public M4x4 modelToWorld;
    /// <summary> The geometry of the mesh. </summary>
    public required ColoredTriangleMeshGeometry Geometry;
    /// <summary> The properties of the shader.</summary>
    public List<(string, object)> shaderProperties = new List<(string, object)>();
    internal Dictionary<string, DynProperty> properties = new ();
    /// <summary> Whether the mesh is 2D or 3D. </summary>
    public bool is2d = false;
    /// <summary> The entity id of the mesh. </summary>
    public int entityId = -1;
}

internal class RendererHandle {
    public ColoredTriangleMeshGeometry? Handle;
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
    public required GlyphState[] Glyphs;
    public required MeshBackedGeometry[] NewMeshes;
    public required ColoredTriangleMesh[] Meshes;
    public required CanvasSnapshot[] Canvases;
    // NOTE: the first renderbuffer is always the main one
    public required RenderBufferState[] RenderBuffers;
    public Dictionary<DynPropertyId, object?> DynamicProperties = new ();

    public double Time;
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

    static World() {
        current = new World(new AnimationSettings());
        current.VoidWorld = true;
    }

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

    Dictionary<DynPropertyId, Func<Dictionary<DynPropertyId, object?>, object?>> _activeDynEvaluators = new ();

    // used by EntityCollection to keep track of children
    private Dictionary<int, List<VisualEntity>> _children = new ();
    private Dictionary<int, VisualEntity> _parents = new();

    private Dictionary<int, VisualEntity> _entities = new ();

    private Dictionary<DynPropertyId, object?> _dynamicPropertyValues = new ();
    private Dictionary<DynPropertyId, DynProperty> _dynamicProperties = new ();

    private Stack<List<VisualEntity>> _captureStack = new();

    object? currentEditor = null; // who edits things right now (e.g. scene or animationbehaviour)

    /// <summary>
    /// The time dynamic property.
    /// </summary>
    public DynProperty<double> CurrentTime;

    /// <summary>
    /// Fake world.
    /// </summary>
    public bool VoidWorld = false;

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
                cameraEntId: value?.Id ?? 0,
                oldCamEntId: _activeCamera?.Id ?? 0,
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
        Id = worldId++;
        _activeCanvas = new Canvas(CanvasState.DEFAULTNAME, new OrthoCamera());
        Resources = new WorldResources();
        this.CurrentTime = DynProperty<double>.CreateEmpty(0.0);
        //this._activeCamera.Position = new Vector3(0.0f, 0.0f, 13.0f);
        Reset();
    }

    internal int GetUniqueId() {
        return entityId++;
    }

    internal DynPropertyId GetUniqueDynId() {
        return new DynPropertyId(entityId++);
    }

    internal void Update(double dt)
    {
        // NOTE: WorldMachine has evaluator for special properties like time, but it doesn't change property during baking.
        //   This evaluates the property during baking, so it will be visible to AnimationBehaviour code.
        //   There is no need to add a command (setting .Value) because of the reason mentioned above, only the state is assigned.
        CurrentTime._value = Time.T;
        _dynamicPropertyValues[CurrentTime.Id] = Time.T;
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
        _commands.Clear();
        _activeDynEvaluators.Clear();
        var cam = new PerspectiveCamera();
        cam.Fov = 60.0f;
        cam.ZNear = 0.1f;
        cam.ZFar = 1000.0f;
        cam.Position = new Vector3(0.0f, 0.0f, -13.0f);

        var screenCam = new OrthoCamera();
        screenCam.Width = settings.Width;
        screenCam.Height= settings.Height;
        CreateInstantly(screenCam);

        var defaultCanvas = new Canvas(CanvasState.DEFAULTNAME, screenCam);
        CreateInstantly(defaultCanvas);
        Canvas.Default = defaultCanvas;

        this.ActiveCanvas = defaultCanvas;

        this.CurrentTime = new DynProperty<double>("time", 0.0f, CurrentTime);
        var sprop = new WorldSpecialPropertyCommand(
            propertyId: CurrentTime.Id,
            SpecialWorldPropertyType.Time,
            time: Time.T
        );
        _commands.Add(sprop);

        CreateInstantly(cam);
        ActiveCamera = cam;
        EndEditing();
    }

    internal Canvas? FindCanvas(string name) {
        foreach(var ent in _entities) {
            if(ent.Value is Canvas) {
                var canvas = ent.Value as Canvas;
                if(canvas?.Name == name) {
                    return canvas;
                }
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
    
    /// <summary>
    /// Begin capturing creation of entities. All entities created after this call will be added to the returned list.
    /// </summary>
    public List<VisualEntity> BeginCapture() {
        var ret = new List<VisualEntity>();
        _captureStack.Push(ret);
        return ret;
    }

    /// <summary>
    /// End capturing creation of entities.
    /// </summary>
    public void EndCapture() {
        _captureStack.Pop();
    }

    internal void BeginEvaluator(DynProperty prop, Func<Dictionary<DynPropertyId, object?>, object?> evaluator)
    {
        var id = prop.Id;
        if (id.Id == 0)
        {
            return;
        }
        if (_activeDynEvaluators.ContainsKey(id))
        {
            Debug.Error($"Dyn property {id} already has active evaluator. Make sure you don't evaluate a property from multiple places at the same time.");
            return;
        }

        var cmd = new WorldPropertyEvaluatorCreate(
            propertyId: id,
            evaluator: evaluator,
            oldValue: _dynamicPropertyValues[id],
            time: Time.T
        );
        _commands.Add(cmd);
        _activeDynEvaluators.Add(id, evaluator);
        prop.Evaluator = () =>
        {
            return evaluator(_dynamicPropertyValues);
        };
    }

    internal void EndEvaluator(DynProperty prop, object? finalValue) {
        var id = prop.Id;
        if (id.Id == 0) {
            return;
        }
        if (!_activeDynEvaluators.ContainsKey(id)) {
            Debug.Error($"Dyn property {id} does not have active evaluator. Maybe it was ignored due to race condition?");
            return;
        }
        var cmd = new WorldPropertyEvaluatorDestroy(
            propertyId: id,
            evaluator: _activeDynEvaluators[id],
            finalValue: finalValue,
            time: Time.T
        );
        _commands.Add(cmd);
        prop.Evaluator = null;
        _dynamicPropertyValues[id] = finalValue;
        // use the internal version to avoid queueing another command
        prop._value = finalValue;
        _activeDynEvaluators.Remove(id);
    }

    private void EntityCreated(VisualEntity entity)
    {
        entity.Id.Value = GetUniqueId();
        if (currentEditor == null)
        {
            Debug.Error("Entity created when no one is editing!? Use StartEditing() before modifying world.");
        }
        /*if (currentEditor != null) {
            entity.state.creator = currentEditor;
        }
        else {
            Debug.Warning("Entity created without creator. This is not a problem but might make debugging harder.");
        }*/
        var cmd = new WorldCreateCommand(
            entity: entity,
            time: Time.T
        );
        _commands.Add(cmd);
        entity.OnCreated();
        _entities.Add(entity.Id, entity);

        if (_captureStack.Count > 0)
        {
            foreach (var capture in _captureStack)
            {
                capture.Add(entity);
            }
        }
        
        CheckDependantEntities(entity);
    }

    private void EntityDestroyed(VisualEntity entity)
    {
        foreach(var list in _captureStack) {
            list.RemoveAll(x => x.Id == entity.Id);
        }
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
        if (!_children.ContainsKey(parent.Id))
        {
            _children.Add(parent.Id, new List<VisualEntity>());
        }
        child.ManagedLifetime = true;
        _children[parent.Id].Add(child);
        _parents.Add(child.Id, parent);
    }
    
    internal void DetachChild(VisualEntity parent, VisualEntity child)
    {
        if (!_children.ContainsKey(parent.Id))
        {
            Debug.Error("Parent does not have any children");
            return;
        }
        child.ManagedLifetime = false;
        _children[parent.Id].Remove(child);
        _parents.Remove(child.Id);
    }
    
    internal T MatchCreation<T>(T ent, Func<VisualEntity, bool> match) where T : VisualEntity
    {
        CreationListeners.Add(match);
        // check if the entity already exists
        foreach (var dent in _entities.Values.ToList())
        {
            CheckDependantEntities(dent);
        }
        return ent;
    }

    public void Marker(string id)
    {
        var cmd = new WorldMarkerCommand(id, Time.T);
        _commands.Add(cmd);
    }

    internal DynPropertyId CreateDynProperty(object? vl, DynProperty prop)
    {
        var id = GetUniqueDynId();
        _dynamicPropertyValues.Add(id, vl);
        _dynamicProperties.Add(id, prop);
        var cmd = new WorldCreateDynPropertyCommand(
            propertyId: id,
            value: vl,
            time: Time.T
        );
        _commands.Add(cmd);
        return id;
    }

    internal object? GetDynProperty(DynPropertyId id) {
        _dynamicPropertyValues.TryGetValue(id, out var ret);
        return ret;
    }

    internal void SetDynProperty(DynPropertyId id, object? value) {
        if (id.Id == 0) {
            return;
        }
        if (_activeDynEvaluators.ContainsKey(id)) {
            Debug.Error("Can't set dyn property while it has an evaluator. SetDynProperty ignored. Make sure you don't control single property from multiple places.");
            return;
        }
        var cmd = new WorldDynPropertyCommand(
            propertyId: id,
            newvalue: value,
            oldvalue: _dynamicPropertyValues[id],
            time: Time.T
        );
        _commands.Add(cmd);
        _dynamicPropertyValues[id] = value;
    }

    public T CreateInstantly<T>(T ent) where T : VisualEntity {
        EntityCreated(ent);
        return ent;
    }

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

    /// <summary>
    /// Clone a dynamic entity. Only state will be copied, the entity will not be created implicitly.
    /// </summary>
    public T Clone<T>(T e) where T : VisualEntity {
        var ret = e.Clone();
        return (T)ret;
    }

    public T CreateClone<T>(T e) where T : VisualEntity {
        var ret = (T)e.Clone();
        CreateInstantly(ret);
        return ret;
    }

    public void Destroy<T>(T[] objs) where T : VisualEntity
    {
        foreach (var obj in objs)
        {
            Destroy(obj);
        }
    }

    public void Destroy(VisualEntity obj)
    {
        if (!obj.Created) return;

        // TODO:
        /*if (obj.managedLifetime)
        {
            Debug.Error("Attempting to destroy managed entity. Detach the entity from it's parent EntityCollection first.");
            return;
        }*/

        // destroy all children
        if (_children.ContainsKey(obj.Id))
        {
            var localC = _children[obj.Id].ToArray();
            foreach (var child in localC)
            {
                DetachChild(obj, child);
                Destroy(child);
            }
        }
        var cmd = new WorldDestroyCommand(
            entity: obj,
            time: Time.T
        );
        obj.Created.Value = false;
        _commands.Add(cmd);

        EntityDestroyed(obj);
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
