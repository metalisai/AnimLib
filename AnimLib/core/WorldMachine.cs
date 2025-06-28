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
        // TODO: remove
        public List<EntityState2D> Entities = new List<EntityState2D>();
        public List<DynVisualEntity2D> NewEntities = new();
    }
    List<Glyph> _glyphs = new List<Glyph>();
    List<MeshBackedGeometry> _mbgeoms = new List<MeshBackedGeometry>();
    List<CubeState> _cubes = new List<CubeState>();
    List<EntityState> _cameras =  new List<EntityState>();
    List<BezierState> _beziers = new List<BezierState>();

    List<DynShape> _dynShapes = new List<DynShape>();

    Dictionary<int, CanvasEntities> _canvases = new Dictionary<int, CanvasEntities>();
    //Dictionary<VisualEntity, EntityState> _entities = new Dictionary<VisualEntity, EntityState>();
    Dictionary<int, EntityState> _entities = new Dictionary<int, EntityState>();

    Dictionary<int, EntityState> _destroyedEntities = new Dictionary<int, EntityState>();
    List<RenderBufferState> _renderBuffers = new();

    Dictionary<DynPropertyId, object?> _dynamicProperties = new ();

    Dictionary<DynPropertyId, Func<Dictionary<DynPropertyId, object?>, object?>> _propertyEvaluators = new ();

    List<(DynPropertyId propId, SpecialWorldPropertyType type)> _specialProperties = new ();

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
                return new WorldCommand[0];
            else
                return _program;
        }
    }

    private WorldCommand[] _program = Array.Empty<WorldCommand>();
    int _playCursorCmd = 0;
    double _currentPlaybackTime = 0.0;

    CameraState? _activeCamera;

    public void Reset() {
        _glyphs.Clear();
        _mbgeoms.Clear();
        //_rectangles.Clear();
        _cubes.Clear();
        _cameras.Clear();
        _playCursorCmd = 0;
        _currentPlaybackTime = 0.0;
        _specialProperties.Clear();
        _entities.Clear();
        _dynamicProperties.Clear();
        _propertyEvaluators.Clear();
        _dynShapes.Clear();
        _beziers.Clear();
        _canvases.Clear();
        _dynamicProperties.Clear();
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
            _dynamicProperties[sp.propId] = sp.type switch {
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
        foreach(var kvp in _propertyEvaluators) {
            try {
                _dynamicProperties[kvp.Key] = kvp.Value(_dynamicProperties);
            } catch(Exception e) {
                Debug.Error($"Error evaluating property {kvp.Key}: {e.Message}");
            }
        }
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
        if (_dynamicProperties.TryGetValue(id, out var val))
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
        foreach (var c in _canvases.OrderBy(x => _entities[x.Key].sortKey))
        {
            var canvas = (CanvasState)_entities[c.Key];
            var effects = canvas.effects.Select(x =>
                (x.GetType().Name,
                x.properties.Select(x => (x.Key, this._dynamicProperties[x.Value.Id])).ToArray())
            ).ToArray();


            var css = new CanvasSnapshot()
            {
                //Entities = c.Value.Entities.Where(x => x.active).Select(x => (EntityState2D)x.Clone()).ToArray(),
                Entities = c.Value.NewEntities.Where(x => x.Active).Select(x => (EntityState2D)x.GetState(GetDynProp)).Concat(c.Value.Entities.Where(x => x.active).Select(x => (EntityState2D)x.Clone())).ToArray(),
                Canvas = canvas,
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
            MeshBackedGeometries = _mbgeoms.Where(x => x.active).ToArray(),
            Cubes = _cubes.Where(x => x.active).ToArray(),
            Beziers = _beziers.Where(x => x.active).ToArray(),
            resolver = new EntityStateResolver(
                GetEntityState: entid =>
                {
                    return _entities.ContainsKey(entid) ? _entities[entid] : null;
                }
            ),
            Canvases = l.ToArray(),
            Camera = (_activeCamera?.Clone() as CameraState) ?? null,
            RenderBuffers = _renderBuffers.ToArray(),
            // TODO: populate these
            Rectangles = Array.Empty<RectangleState>(),
            Meshes = Array.Empty<ColoredTriangleMesh>(),
            DynamicProperties = _dynamicProperties.ToDictionary(),
        };
        return ret;
    }

    private void CreateDynEntity(DynVisualEntity ent) {
        switch (ent)
        {
            case Glyph g1:
                _glyphs.Add(g1);
                break;
            case DynVisualEntity2D ent2d:
                var canvas = (CanvasState)_entities[ent2d.CanvasId];
                _canvases[canvas.entityId].NewEntities.Add(ent2d);
                break;
            default:
                Debug.Error($"Creating unknown Dyn entity {ent}");
                break;
        }
    }

    private void DestroyDynEntity(DynVisualEntity ent) {
        switch (ent)
        {
            case Glyph g1:
                _glyphs.RemoveAll(x => x.Id == ent.Id);
                break;
            case DynVisualEntity2D ent2d:
                foreach(var s in _canvases) s.Value.NewEntities.RemoveAll(x => x.Id == ent.Id);
                break;
            default:
                Debug.Error($"Destroying unknown Dyn entity {ent}");
                break;
        }
    }

    private void CreateEntity(object entity) {
        EntityState state;
        switch(entity) {
            case MeshBackedGeometry a1:
            state = (EntityState)a1.Clone();
            //state = a1;
            _mbgeoms.Add((MeshBackedGeometry)state);
            break;
            case CubeState c2:
            state = (EntityState)c2.Clone();
            //state = c2;
            _cubes.Add((CubeState)state);
            break;
            case BezierState bz:
            state = (EntityState)bz.Clone();
            _beziers.Add((BezierState)state);
            break;
            case PerspectiveCameraState p1:
            state = (EntityState)p1.Clone();
            _cameras.Add((PerspectiveCameraState)state);
            break;
            case OrthoCameraState oc1:
            state = (EntityState)oc1.Clone();
            _cameras.Add((OrthoCameraState)state);
            break;
            case CanvasState ca1:
            state = (EntityState)ca1.Clone();
            _canvases.Add(state.entityId, new CanvasEntities());
            break;
            case EntityState2D ent2d:
            var canvas = (CanvasState)_entities[ent2d.canvasId];
            state = (EntityState)ent2d.Clone();
            _canvases[canvas.entityId].Entities.Add((EntityState2D)state);
            break;
            default:
            throw new NotImplementedException();
        }
        _entities.Add(state.entityId, state);
    }

    private void DestroyEntity(int entityId) {
        var ent = _entities[entityId];
        switch(ent) {
            case ArrowState a1:
            _mbgeoms.RemoveAll(x => x.entityId == entityId);
            break;
            case CubeState c2:
            _cubes.RemoveAll(x => x.entityId == entityId);
            break;
            case BezierState bz:
            _beziers.RemoveAll(x => x.entityId == entityId);
            break;
            case SolidLineState l2:
            _mbgeoms.RemoveAll(x => x.entityId == entityId);
            break;
            case MeshBackedGeometry mb1:
            _mbgeoms.RemoveAll(x => x.entityId == entityId);
            break;
            case CameraState c3:
            _cameras.RemoveAll(x => x.entityId == entityId);
            break;
            case CanvasState ca1:
            _canvases.Remove(entityId);
            break;
            case EntityState2D ss1:
            foreach(var s in _canvases) s.Value.Entities.RemoveAll(x => x.entityId == entityId);
            break;
            default:
            throw new Exception("Destroying unknown entity!");
        }
        var state = _entities[entityId];
        _destroyedEntities[entityId] = state;
        var removed = _entities.Remove(entityId);
        if(!removed) {
            throw new Exception("Destroying an entity that does not exist!");
        }
    }

    private void Execute(WorldCommand cmd) {
        switch(cmd) {
            case WorldCreateCommand worldCreate:
                CreateEntity(worldCreate.entity);
            break;
            case WorldDynCreateCommand dynCreate:
                CreateDynEntity(dynCreate.entity);
            break;
            case WorldDestroyCommand worldDestroy:
            DestroyEntity(worldDestroy.entityId);
            break;
            case WorldDynDestroyCommand dynDestroy:
            DestroyDynEntity(dynDestroy.entity);
            break;
            case WorldPropertyMultiCommand wpm:
            {
                var lower = char.ToLower(wpm.property[0]) + wpm.property.Substring(1);
                for(int i = 0; i < wpm.entityIds.Length; i++) {
                    var eid = wpm.entityIds[i];
                    var oldValue = wpm.oldvalue[i];
                    if(!_entities.ContainsKey(eid)) {
                        Debug.Error($"Setting property of an entity that doesnt exist. Entity {eid}, property {wpm.property}");
                        Debug.Error($"Last worldmachine action: {_lastAction}");
                        Debug.Error($"Command {_playCursorCmd}/{Program.Length}");
                    }
                    var state = _entities[eid];
                    // move 2D entity from 1 canvas to another if its changed
                    if(state is EntityState2D && wpm.property.ToLower() == "canvasid") {
                        var oldCanvas = _canvases[(int)oldValue];
                        var newCanvas = _canvases[(int)wpm.newvalue];
                        switch(state) {
                            case ShapeState:
                            case MorphShapeState:
                            oldCanvas.Entities.RemoveAll(x => x.entityId == eid);
                            newCanvas.Entities.Add((EntityState2D)state);
                            break;
                        }
                    }
                    var field = state.GetType().GetField(lower);
                    if (field != null) {
                        field.SetValue(state, wpm.newvalue);
                    } else {
                        var prop = state.GetType().GetProperty(lower);
                        prop?.SetValue(state, wpm.newvalue);
                    }
                }
            }
            break;
            case WorldPropertyCommand worldProperty:
            {
                //Console.WriteLine($"Property {worldProperty.property} set!");
                var lower = char.ToLower(worldProperty.property[0]) + worldProperty.property.Substring(1);
                if(!_entities.ContainsKey(worldProperty.entityId)) {
                    Debug.Error($"Setting property of an entity that doesnt exist. Entity {worldProperty.entityId}, property {worldProperty.property}");
                    Debug.Error($"Last worldmachine action: {_lastAction}");
                    Debug.Error($"Command {_playCursorCmd}/{Program.Length}");
                }

                var state = _entities[worldProperty.entityId];
                // move 2D entity from 1 canvas to another if its changed
                if(state is EntityState2D && worldProperty.property.ToLower() == "canvasid") {
                    var oldCanvas = _canvases[(int)(worldProperty.oldvalue ?? throw new Exception("oldvalue is null"))];
                    var newCanvas = _canvases[(int)(worldProperty.newvalue ?? throw new Exception("newvalue is null"))];
                    switch(state) {
                        case ShapeState:
                        case MorphShapeState:
                        oldCanvas.Entities.RemoveAll(x => x.entityId == worldProperty.entityId);
                        newCanvas.Entities.Add((EntityState2D)state);
                        break;
                    }
                }
                var field = state.GetType().GetField(lower);
                if (field != null) {
                    field.SetValue(state, worldProperty.newvalue);
                } else {
                    var prop = state.GetType().GetProperty(lower);
                    prop?.SetValue(state, worldProperty.newvalue);
                }
            }
            break;
            case WorldSetActiveCameraCommand setActiveCameraCommand:
            _activeCamera = (CameraState)_entities[setActiveCameraCommand.cameraEntId];
            break;
            case WorldCreateDynPropertyCommand createDynPropertyCommand:
            _dynamicProperties.Add(createDynPropertyCommand.propertyId, createDynPropertyCommand.value);
            break;
            case WorldDynPropertyCommand dynPropertyCommand:
            _dynamicProperties[dynPropertyCommand.propertyId] = dynPropertyCommand.newvalue;
            break;
            case WorldPropertyEvaluatorCreate evaluatorCreate:
            _propertyEvaluators.Add(evaluatorCreate.propertyId, evaluatorCreate.evaluator);
            _dynamicProperties[evaluatorCreate.propertyId] = evaluatorCreate.oldValue;
            break;
            case WorldPropertyEvaluatorDestroy evaluatorDestroy:
            _propertyEvaluators.Remove(evaluatorDestroy.propertyId);
            _dynamicProperties[evaluatorDestroy.propertyId] = evaluatorDestroy.finalValue;
            break;
            case WorldSpecialPropertyCommand specialPropertyCommand:
            _specialProperties.Add((specialPropertyCommand.propertyId, specialPropertyCommand.property));
            break;
            default:
            Debug.Warning($"Unknown world command {cmd}");
            break;
        }
    }

    private void Undo(WorldCommand cmd) {
        switch(cmd) {
            case WorldCreateCommand worldCreate:
            DestroyEntity(((EntityState)worldCreate.entity).entityId);
            break;
            case WorldDynCreateCommand dynCreate:
            DestroyDynEntity(dynCreate.entity);
            break;
            case WorldDestroyCommand worldDestroy:
            // TODO: this will be created with wrong properties (they are latest in world not from when the entity was destroyed)
            // NOTE: but if the object gets destroyed, aren't the latest the right ones?
            CreateEntity(_destroyedEntities[worldDestroy.entityId]);
            _destroyedEntities.Remove(worldDestroy.entityId);
            break;
            case WorldDynDestroyCommand dynDestroy:
            CreateDynEntity(dynDestroy.entity);
            break;
            case WorldPropertyMultiCommand wpm:
            {
                for(int i = 0; i < wpm.entityIds.Length; i++) {
                    var eid = wpm.entityIds[i];
                    var oval = wpm.oldvalue[i];
                    var lower = char.ToLower(wpm.property[0]) + wpm.property.Substring(1);
                    var state = _entities[eid];
                    if(state is EntityState2D && wpm.property.ToLower() == "canvasid") {
                        var newCanvas = _canvases[(int)oval];
                        var oldCanvas = _canvases[(int)wpm.newvalue];
                        switch(state) {
                            case ShapeState:
                            case MorphShapeState:
                            oldCanvas.Entities.RemoveAll(x => x.entityId == eid);
                            newCanvas.Entities.Add((EntityState2D)state);
                            break;
                        }
                    }
                    var field = state.GetType().GetField(lower);
                    if (field != null) {
                        field.SetValue(state, oval);
                    } else {
                        var prop = state.GetType().GetProperty(lower);
                        prop?.SetValue(state, oval);
                    }
                }
            }
            break;
            case WorldPropertyCommand worldProperty:
            {
                var lower = char.ToLower(worldProperty.property[0]) + worldProperty.property.Substring(1);
                var state = _entities[worldProperty.entityId];
                if(state is EntityState2D && worldProperty.property.ToLower() == "canvasid") {
                    var newCanvas = _canvases[(int)(worldProperty.oldvalue ?? throw new Exception("oldvalue is null"))];
                    var oldCanvas = _canvases[(int)(worldProperty.newvalue ?? throw new Exception("newvalue is null"))];
                    switch(state) {
                        case ShapeState:
                        case MorphShapeState:
                        oldCanvas.Entities.RemoveAll(x => x.entityId == worldProperty.entityId);
                        newCanvas.Entities.Add((EntityState2D)state);
                        break;
                    }
                }
                var field = state.GetType().GetField(lower);
                if (field != null) {
                    field.SetValue(state, worldProperty.oldvalue);
                } else {
                    var prop = state.GetType().GetProperty(lower);
                    prop?.SetValue(state, worldProperty.oldvalue);
                }
            }
            break;
            case WorldSetActiveCameraCommand setActiveCameraCommand:
            _activeCamera = (setActiveCameraCommand.oldCamEntId == 0 ? null : (CameraState)_entities[setActiveCameraCommand.oldCamEntId]);
            break;
            case WorldCreateDynPropertyCommand createDynPropertyCommand:
            var removed = _dynamicProperties.Remove(createDynPropertyCommand.propertyId);
            if(!removed) {
                throw new Exception("Destroying a dyn property that does not exist!");
            }
            break;
            case WorldDynPropertyCommand dynPropertyCommand:
            _dynamicProperties[dynPropertyCommand.propertyId] = dynPropertyCommand.oldvalue;
            break;
            case WorldPropertyEvaluatorCreate evaluatorCreate:
                _propertyEvaluators.Remove(evaluatorCreate.propertyId);
                _dynamicProperties[evaluatorCreate.propertyId] = evaluatorCreate.oldValue;
            break;
            case WorldPropertyEvaluatorDestroy evaluatorDestroy:
                _propertyEvaluators.Add(evaluatorDestroy.propertyId, evaluatorDestroy.evaluator);
                EvaluateSpecialProperties();
                // TODO: this is bugged when the evaluator depends on other non-special properties
                _dynamicProperties[evaluatorDestroy.propertyId] = evaluatorDestroy.finalValue;
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
