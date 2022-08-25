using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib {
    internal class WorldMachine {

        List<CircleState> _circles = new List<CircleState>();
        List<Text2DState> _texts = new List<Text2DState>();
        List<GlyphState> _glyphs = new List<GlyphState>();
        List<MeshBackedGeometry> _mbgeoms = new List<MeshBackedGeometry>();
        List<RectangleState> _rectangles = new List<RectangleState>();
        List<TexRectState> _trects = new List<TexRectState>();
        List<CubeState> _cubes = new List<CubeState>();
        List<EntityState> _cameras =  new List<EntityState>();
        List<LabelState> _labels = new List<LabelState>();
        List<BezierState> _beziers = new List<BezierState>();
        List<CanvasState> _canvases = new List<CanvasState>();
        List<ShapeState> _shapes = new List<ShapeState>();
        //Dictionary<VisualEntity, EntityState> _entities = new Dictionary<VisualEntity, EntityState>();
        Dictionary<int, EntityState> _entities = new Dictionary<int, EntityState>();
        Dictionary<int, AbsorbDestruction> _absorbs = new Dictionary<int, AbsorbDestruction>();

        Dictionary<int, EntityState> _destroyedEntities = new Dictionary<int, EntityState>();

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

        private WorldCommand[] _program;
        int _playCursorCmd = 0;
        double _currentPlaybackTime = 0.0;

        CameraState _activeCamera;


        public void Reset() {
            _circles.Clear();
            _texts.Clear();
            _glyphs.Clear();
            _mbgeoms.Clear();
            _rectangles.Clear();
            _trects.Clear();
            _cubes.Clear();
            _cameras.Clear();
            _labels.Clear();
            _playCursorCmd = 0;
            _currentPlaybackTime = 0.0;
            _entities.Clear();
            _beziers.Clear();
            _canvases.Clear();
            _shapes.Clear();
            //var cam = new PerspectiveCamera();
            //cam.Position = new Vector3(0.0f, 0.0f, -13.0f);
            //_activeCamera = cam;
            Step(0.0);
        }
        public void SetProgram(WorldCommand[] commands) {
            _program = commands.ToArray();
            Reset();
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

        public WorldSnapshot GetWorldSnapshot() {
            var ret = new WorldSnapshot();
            //ret.Texts = _texts.Where(x => x.active).ToArray();
            ret.Glyphs = _glyphs.Where(x => x.active).ToArray();
            ret.Circles = _circles.Where(x => x.active).ToArray();
            ret.Rectangles = _rectangles.Where(x => x.active).ToArray();
            ret.TexRects = _trects.Where(x => x.active).ToArray();
            ret.MeshBackedGeometries = _mbgeoms.Where(x => x.active).ToArray();
            ret.Cubes = _cubes.Where(x => x.active).ToArray();
            ret.Beziers = _beziers.Where(x => x.active).ToArray();
            ret.Canvases = _canvases.Where(x => x.active).ToArray();
            ret.Shapes = _shapes.Where(x => x.active).ToArray();
            ret.resolver = new EntityStateResolver {
                GetEntityState = entid => {
                    return _entities.ContainsKey(entid) ? _entities[entid] : null;
                }
            };

            // TODO: better way to do this?
            foreach(var rect in ret.Rectangles) {
                EntityState ca = null;
                if(_entities.TryGetValue(rect.canvasId, out ca) && ca is CanvasState) {
                    rect.canvas = ca as CanvasState;
                } else {
                    Debug.Error($"Visual2DEntity's canvas {rect.canvasId} could not be found!");
                }
            }
            foreach(var shape in ret.Shapes) {
                EntityState ca = null;
                if(_entities.TryGetValue(shape.canvasId, out ca) && ca is CanvasState) {
                    shape.canvas = ca as CanvasState;
                } else {
                    Debug.Error($"Visual2DEntity's canvas {shape.canvasId} could not be found!");
                }
            }
            /*foreach(var label in ret.Labels) {
                label.Item1.entity.state.entity.
            }*/
            /*ret.Labels = _labels.Where(x => x.active).Select(x => (x, _entities[x.target as VisualEntity])).ToArray();*/
            ret.Camera = _activeCamera;
            return ret;
        }

        private void CreateEntity(object entity) {
            EntityState state;
            switch(entity) {
                case CircleState c1:
                state = (EntityState)c1.Clone();
                //state = c1;
                _circles.Add((CircleState)state);
                break;
                case Text2DState t1:
                state = (EntityState)t1.Clone();
                //state = t1;
                _texts.Add((Text2DState)state);
                break;
                case GlyphState g1:
                state = (EntityState)g1.Clone();
                _glyphs.Add((GlyphState)state);
                break;
                case MeshBackedGeometry a1:
                state = (EntityState)a1.Clone();
                //state = a1;
                _mbgeoms.Add((MeshBackedGeometry)state);
                break;
                case RectangleState r1:
                state = (EntityState)r1.Clone();
                //state = r1;
                if(r1 is TexRectState) {
                    _trects.Add((TexRectState)state);
                } else {
                    _rectangles.Add((RectangleState)state);
                }
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
                case LabelState l1:
                state = (EntityState)l1.Clone();
                _labels.Add((LabelState)state);
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
                _canvases.Add((CanvasState)state);
                break;
                case ShapeState ss1:
                state = (EntityState)ss1.Clone();
                _shapes.Add((ShapeState)state);
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
                case CircleState c1:
                _circles.RemoveAll(x => x.entityId == entityId);
                break;
                case RectangleState r1:
                if(r1 is TexRectState) {
                    _trects.RemoveAll(x => x.entityId == entityId);
                } else {
                    _rectangles.RemoveAll(x => x.entityId == entityId);
                }
                break;
                case Text2DState t1:
                _texts.RemoveAll(x => x.entityId == entityId);
                break;
                case GlyphState g1:
                    _glyphs.RemoveAll(x => x.entityId == entityId);
                break;
                case CubeState c2:
                _cubes.RemoveAll(x => x.entityId == entityId);
                break;
                case BezierState bz:
                _beziers.RemoveAll(x => x.entityId == entityId);
                break;
                case LabelState l1:
                _labels.RemoveAll(x => x.entityId == entityId);
                break;
                case SolidLineState l2:
                _mbgeoms.RemoveAll(x => x.entityId == entityId);
                break;
                case CameraState c3:
                _cameras.RemoveAll(x => x.entityId == entityId);
                break;
                case CanvasState ca1:
                _canvases.RemoveAll(x => x.entityId == entityId);
                break;
                case ShapeState ss1:
                _shapes.RemoveAll(x => x.entityId == entityId);
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
                case WorldDestroyCommand worldDestroy:
                DestroyEntity(worldDestroy.entityId);
                break;
                case WorldPropertyCommand worldProperty:
                //Console.WriteLine($"Property {worldProperty.property} set!");
                var lower = char.ToLower(worldProperty.property[0]) + worldProperty.property.Substring(1);
                if(!_entities.ContainsKey(worldProperty.entityId)) {
                    Debug.Error($"Setting property of an entity that doesnt exist. Entity {worldProperty.entityId}, property {worldProperty.property}");
                    Debug.Error($"Last worldmachine action: {_lastAction}");
                    Debug.Error($"Command {_playCursorCmd}/{Program.Length}");
                }

                var state = _entities[worldProperty.entityId];
                var field = state.GetType().GetField(lower);
                field.SetValue(state, worldProperty.newvalue);
                break;
                case WorldSetActiveCameraCommand setActiveCameraCommand:
                _activeCamera = (CameraState)_entities[setActiveCameraCommand.cameraEntId];
                break;
                case WorldAbsorbCommand abscmd:
                if(abscmd.progress > 0.0f && abscmd.progress < 1.0f) {
                    if(!_absorbs.TryAdd(abscmd.entityId, new AbsorbDestruction() {
                        entityId = abscmd.entityId,
                        progress = abscmd.progress,
                    })) {
                        _absorbs[abscmd.entityId].progress = abscmd.progress;
                        _entities[abscmd.entityId].anim.progress = abscmd.progress;
                    } else {
                        _entities[abscmd.entityId].anim = new RendererAnimation() {
                            point = abscmd.absorbPoint,
                            screenPoint = abscmd.absorbScreenPoint,
                            progress = 0.0f,
                        };
                    }
                } else if(abscmd.progress >= 1.0f) {
                    _absorbs.Remove(abscmd.entityId);
                    _entities[abscmd.entityId].anim = null;
                }
                break;
            }
        }

        private void Undo(WorldCommand cmd) {
            switch(cmd) {
                case WorldCreateCommand worldCreate:
                DestroyEntity(((EntityState)worldCreate.entity).entityId);
                break;
                case WorldDestroyCommand worldDestroy:
                // TODO: this will be created with wrong properties (they are latest in world not from when the entity was destroyed)
                // NOTE: but if the object gets destroyed, aren't the latest the right ones?
                CreateEntity(_destroyedEntities[worldDestroy.entityId]);
                _destroyedEntities.Remove(worldDestroy.entityId);
                break;
                case WorldPropertyCommand worldProperty:
                var lower = char.ToLower(worldProperty.property[0]) + worldProperty.property.Substring(1);
                var state = _entities[worldProperty.entityId];
                var field = state.GetType().GetField(lower);
                field.SetValue(state, worldProperty.oldvalue);
                break;
                case WorldSetActiveCameraCommand setActiveCameraCommand:
                _activeCamera = (setActiveCameraCommand.oldCamEntId == 0 ? null : (CameraState)_entities[setActiveCameraCommand.oldCamEntId]);
                break;
                case WorldAbsorbCommand abscmd:
                    if(_absorbs.ContainsKey(abscmd.entityId)) {
                        _absorbs[abscmd.entityId].progress = abscmd.oldprogress;
                        _entities[abscmd.entityId].anim.progress = abscmd.progress;
                        if(abscmd.oldprogress == 0.0f){
                            _absorbs.Remove(abscmd.entityId);
                            _entities[abscmd.entityId].anim = null;
                        }
                    } else {
                        _absorbs.Add(abscmd.entityId, new AbsorbDestruction() {
                            entityId = abscmd.entityId,
                            progress = abscmd.oldprogress,
                        });
                        _entities[abscmd.entityId].anim = new RendererAnimation(){
                            point = abscmd.absorbPoint,
                            screenPoint = abscmd.absorbScreenPoint,
                            progress = 1.0f,
                        };;
                    }
                break;
            }
        }

        public EntityState GetEntityState(int entityId) {
            EntityState ret = null;
            _entities.TryGetValue(entityId, out ret);
            return ret;
        }
    }
}
