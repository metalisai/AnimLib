using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace AnimLib;

/// <summary>
/// A machine that executes a program of <c>WorldCommand</c>s. Allows for playback, seeking, and undo/redo of recorded World modifications. Can output WorldSnapshot for rendering etc.
/// </summary>
internal class WorldMachine {

    public class CanvasEntities
    {
        public List<VisualEntity2D> NewEntities = new();
    }
    List<Glyph> _glyphs = new List<Glyph>();
    List<MeshEntity3D> _meshEntities = new();
    List<VisualEntity> _cameras =  new ();

    List<Shape> _dynShapes = new List<Shape>();

    Dictionary<int, VisualEntity> _dynEntities = new();

    Dictionary<int, CanvasEntities> _canvases = new Dictionary<int, CanvasEntities>();
    //Dictionary<VisualEntity, EntityState> _entities = new Dictionary<VisualEntity, EntityState>();
    Dictionary<int, EntityState> _entities = new Dictionary<int, EntityState>();

    Dictionary<int, EntityState> _destroyedEntities = new Dictionary<int, EntityState>();
    List<RenderBufferState> _renderBuffers = new();

    Dictionary<DynPropertyId, object?> _dynamicPropertyValues = new ();

    Dictionary<DynPropertyId, Func<Dictionary<DynPropertyId, object?>, object?>> _propertyEvaluators = new ();

    List<(DynPropertyId propId, SpecialWorldPropertyType type)> _specialProperties = new ();

    public delegate void MarkerDelegate(string id, bool forward);
    public event MarkerDelegate? OnMarkerExecuted;

    public double fps = 60.0;
    string _lastAction = ""; // this is for debug

    public bool HasProgram {
        get {
            return _program != null;
        }
    }

    private WorldCommand[] Program {
        get {
            if (_program == null)
                return [];
            else
                return _program;
        }
    }

    private WorldCommand[] _program = [];
    int _playCursorCmd = 0;
    double _currentPlaybackTime = 0.0;

    Camera? _activeCamera;

    public void Reset()
    {
        _glyphs.Clear();
        _meshEntities.Clear();
        _cameras.Clear();
        _playCursorCmd = 0;
        _currentPlaybackTime = 0.0;
        _specialProperties.Clear();
        _entities.Clear();
        _dynEntities.Clear();
        _dynamicPropertyValues.Clear();
        _propertyEvaluators.Clear();
        _dynShapes.Clear();
        _canvases.Clear();
        //var cam = new PerspectiveCamera();
        //cam.Position = new Vector3(0.0f, 0.0f, -13.0f);
        //_activeCamera = cam;
        Step(0.0);
    }

    public void SetProgram(WorldCommand[] commands) {
        _program = commands.ToArray();
        _renderBuffers.Clear();
        foreach(var cmd in _program)
        {
            switch (cmd) {
                case WorldCreateRenderBufferCommand createRenderBuffer:
                    _renderBuffers.Add(new RenderBufferState() {
                        Width = createRenderBuffer.width,
                        Height = createRenderBuffer.height,
                        BackendHandle = createRenderBuffer.id
                    });
                    break;
            }
        }
        Reset();
    }

    protected void EvaluateSpecialProperties() {
        foreach(var sp in _specialProperties) {
            _dynamicPropertyValues[sp.propId] = sp.type switch {
                SpecialWorldPropertyType.Time => _currentPlaybackTime,
                _ => throw new NotImplementedException(),
            };
        }
    }

    // returns true if done playing
    public bool Step(double dt) {
        var program = Program;
        if(dt >= 0) { // advance
            _currentPlaybackTime += dt;
            _currentPlaybackTime = Math.Min(_currentPlaybackTime, GetEndTime());
            while(_playCursorCmd < program.Length && program[_playCursorCmd].time <= _currentPlaybackTime) {
                Execute(program[_playCursorCmd]);
                _playCursorCmd++;
            }
        } else if(_currentPlaybackTime > 0.0) { // reversing or still
            _currentPlaybackTime += dt;
            _currentPlaybackTime = Math.Max(0.0, _currentPlaybackTime);
            while(_playCursorCmd-1 > 0 && program[_playCursorCmd-1].time > _currentPlaybackTime) {
                Undo(program[_playCursorCmd-1]);
                _playCursorCmd--;
            }
        }
        EvaluateSpecialProperties();
        DynProperty._evaluationContext = GetDynProp;
        foreach (var kvp in _propertyEvaluators)
        {
            try
            {
                _dynamicPropertyValues[kvp.Key] = kvp.Value(_dynamicPropertyValues);
            }
            catch (Exception e)
            {
                Debug.Error($"Error evaluating property {kvp.Key}: {e.Message}");
            }
        }
        DynProperty._evaluationContext = null;
        return _currentPlaybackTime == 0.0 || _currentPlaybackTime == GetEndTime();
    }

    public double GetPlaybackTime() {
        return _currentPlaybackTime;
    }

    public double GetEndTime() {
        return Program.LastOrDefault()?.time ?? 0.0;
    }

    // from 0.0 to 1.0
    public double GetProgress() {
        double length = GetEndTime();
        if(length == 0.0) // can't divide by 0
            return 0.0;
        return _currentPlaybackTime / length;
    }

    public void Seek(double progress) {
        _lastAction = $"Seek to {progress} (range 0.0 - 1.0)";
        double endT = GetEndTime();
        double time = progress * endT;
        time = Math.Max(0.0, time);
        time = Math.Min(endT, time);
        double delta = time - _currentPlaybackTime;
        Step(delta);
    }

    public void SeekSeconds(double seconds) {
        seconds = Math.Max(0.0, seconds);
        seconds = Math.Min(GetEndTime(), seconds);
        double delta = seconds - _currentPlaybackTime;
        Step(delta);
    }

    class EntComparer : IComparer<EntityState>
    {
        public int Compare(EntityState? x, EntityState? y)
        {
            if (x == null && y == null) return 0;
            else if (x == null) return -1;
            else if (y == null) return 1;
            if(x.sortKey < y.sortKey)
                return -1;
            else if(x.sortKey == y.sortKey)
                return 0;
            else return 1;
        }
    }

    private object? GetDynProp(DynPropertyId id)
    {
        if (id == DynProperty.Invalid.Id)
        {
            throw new Exception($"DynProperty.Invalid.Id ({id} == {DynProperty.Invalid.Id}) is not a valid DynPropertyId! Don't reference it.");
        }
        if (_dynamicPropertyValues.TryGetValue(id, out var val))
        {
            return val;
        }
        else
        {
            throw new Exception($"DynProperty {id} not found!");
        }
    }

    public WorldSnapshot GetWorldSnapshot()
    {

        var l = new List<CanvasSnapshot>();
        foreach (var c in _canvases.OrderBy(x => _dynEntities[x.Key].SortKey.Value))
        {
            var canvas = (Canvas)_dynEntities[c.Key];
            var canvasState = (CanvasState)canvas.GetState(GetDynProp);
            var effects = canvasState.effects.Select(x =>
                (x.GetType().Name,
                x.properties.Select(x => (x.Key, this._dynamicPropertyValues[x.Value.Id]!)).ToArray())
            ).ToArray();


            var css = new CanvasSnapshot()
            {
                //Entities = c.Value.Entities.Where(x => x.active).Select(x => (EntityState2D)x.Clone()).ToArray(),
                Entities = c.Value.NewEntities.Where(x => x.Active).Select(x => (EntityState2D)x.GetState(GetDynProp)).ToArray(),
                Canvas = canvasState,
                Effects = effects,
            };

            //var dynShapes = _dynShapes.Select(x => (ShapeState)x.GetState(getDynProp)).ToArray();
            //css.Entities = css.Entities.Concat(dynShapes).ToArray();

            Array.Sort(css.Entities, new EntComparer());
            l.Add(css);
            //foreach(var s in css.Entities) s.canvas = canvas;
        }

        var ret = new WorldSnapshot()
        {
            Glyphs = _glyphs.Where(x => x.Active).Select(x => (GlyphState)x.GetState(GetDynProp)).ToArray(),
            NewMeshes = _meshEntities.Where(x => x.Active).Select(x => (MeshBackedGeometry)x.GetState(GetDynProp)).ToArray(),
            resolver = new EntityStateResolver(
                GetEntityState: entid =>
                {
                    //return _entities.ContainsKey(entid) ? _entities[entid] : null;
                    _dynEntities.TryGetValue(entid, out var ret);
                    if (ret != null)
                    {
                        return (EntityState)ret.GetState(GetDynProp);
                    }
                    return null;
                }
            ),
            Canvases = l.ToArray(),
            Camera = (_activeCamera?.GetState(GetDynProp) as CameraState) ?? null,
            RenderBuffers = _renderBuffers.ToArray(),
            // TODO: populate these
            Rectangles = Array.Empty<RectangleState>(),
            DynamicProperties = _dynamicPropertyValues.ToDictionary(),
        };
        Array.Sort(ret.NewMeshes, new EntComparer());
        return ret;
    }

    private void CreateDynEntity(VisualEntity ent)
    {
        switch (ent)
        {
            case Glyph g1:
                _glyphs.Add(g1);
                break;
            case VisualEntity2D ent2d:
                var canvas = (Canvas)_dynEntities[ent2d.CanvasId];
                _canvases[canvas.Id].NewEntities.Add(ent2d);
                break;
            case MeshEntity3D ent3d:
                _meshEntities.Add(ent3d);
                break;
            case Canvas canv:
                _canvases.Add(canv.Id, new CanvasEntities());
                break;
            case Camera cam:
                _cameras.Add(cam);
                break;
            default:
                Debug.Error($"Creating unknown Dyn entity {ent}");
                break;
        }
        _dynEntities.Add(ent.Id, ent);
    }

    private void DestroyDynEntity(VisualEntity ent)
    {
        switch (ent)
        {
            case Glyph g1:
                _glyphs.RemoveAll(x => x.Id == ent.Id);
                break;
            case MeshEntity3D ent3d:
                _meshEntities.RemoveAll(x => x.Id == ent.Id);
                break;
            case VisualEntity2D ent2d:
                foreach (var s in _canvases) s.Value.NewEntities.RemoveAll(x => x.Id == ent.Id);
                break;
            case Canvas canv:
                _canvases.Remove(canv.Id);
                break;
            case Camera cam:
                _cameras.RemoveAll(x => x.Id == cam.Id);
                break;
            default:
                Debug.Error($"Destroying unknown Dyn entity {ent}");
                break;
        }
        _dynEntities.Remove(ent.Id);
    }

    private void Execute(WorldCommand cmd) {
        switch(cmd) {
            case WorldCreateCommand dynCreate:
                CreateDynEntity(dynCreate.entity);
            break;
            case WorldDestroyCommand dynDestroy:
            DestroyDynEntity(dynDestroy.entity);
            break;
            case WorldSetActiveCameraCommand setActiveCameraCommand:
            Debug.Assert(_dynEntities[setActiveCameraCommand.cameraEntId] is Camera);
            _activeCamera = _dynEntities[setActiveCameraCommand.cameraEntId] as Camera;
            break;
            case WorldCreateDynPropertyCommand createDynPropertyCommand:
            _dynamicPropertyValues.Add(createDynPropertyCommand.propertyId, createDynPropertyCommand.value);
            break;
            case WorldDynPropertyCommand dynPropertyCommand:
            _dynamicPropertyValues[dynPropertyCommand.propertyId] = dynPropertyCommand.newvalue;
            break;
            case WorldPropertyEvaluatorCreate evaluatorCreate:
            _propertyEvaluators.Add(evaluatorCreate.propertyId, evaluatorCreate.evaluator);
            _dynamicPropertyValues[evaluatorCreate.propertyId] = evaluatorCreate.oldValue;
            break;
            case WorldPropertyEvaluatorDestroy evaluatorDestroy:
            _propertyEvaluators.Remove(evaluatorDestroy.propertyId);
            _dynamicPropertyValues[evaluatorDestroy.propertyId] = evaluatorDestroy.finalValue;
            break;
            case WorldSpecialPropertyCommand specialPropertyCommand:
            _specialProperties.Add((specialPropertyCommand.propertyId, specialPropertyCommand.property));
            break;
            case WorldMarkerCommand markerCmd:
            OnMarkerExecuted?.Invoke(markerCmd.id, true);
            break;
            default:
            Debug.Warning($"Unknown world command {cmd}");
            break;
        }
    }

    private void Undo(WorldCommand cmd) {
        switch (cmd)
        {
            case WorldCreateCommand dynCreate:
                DestroyDynEntity(dynCreate.entity);
                break;
            case WorldDestroyCommand dynDestroy:
                CreateDynEntity(dynDestroy.entity);
                break;
            case WorldSetActiveCameraCommand setActiveCameraCommand:
                _activeCamera = setActiveCameraCommand.oldCamEntId == 0 ? null : _dynEntities[setActiveCameraCommand.oldCamEntId] as Camera;
                break;
            case WorldCreateDynPropertyCommand createDynPropertyCommand:
                var removed = _dynamicPropertyValues.Remove(createDynPropertyCommand.propertyId);
                if (!removed)
                {
                    throw new Exception("Destroying a dyn property that does not exist!");
                }
                break;
            case WorldDynPropertyCommand dynPropertyCommand:
                _dynamicPropertyValues[dynPropertyCommand.propertyId] = dynPropertyCommand.oldvalue;
                break;
            case WorldPropertyEvaluatorCreate evaluatorCreate:
                _propertyEvaluators.Remove(evaluatorCreate.propertyId);
                _dynamicPropertyValues[evaluatorCreate.propertyId] = evaluatorCreate.oldValue;
                break;
            case WorldPropertyEvaluatorDestroy evaluatorDestroy:
                _propertyEvaluators.Add(evaluatorDestroy.propertyId, evaluatorDestroy.evaluator);
                EvaluateSpecialProperties();
                // TODO: this is bugged when the evaluator depends on other non-special properties
                _dynamicPropertyValues[evaluatorDestroy.propertyId] = evaluatorDestroy.finalValue;
                break;
            case WorldMarkerCommand markerCmd:
            OnMarkerExecuted?.Invoke(markerCmd.id, false);
            break;
        }
    }

    public IEnumerable<EntityState> Entities {
        get {
            return _entities.Values;
        }
    }

    public EntityState? GetEntityState(int entityId) {
        EntityState? ret = null;
        _entities.TryGetValue(entityId, out ret);
        return ret;
    }
}
