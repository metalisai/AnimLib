using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimLib {
    public class PlayerScene {
        public enum SceneEventType {
            None,
            Create,
            Delete,
        }

        public class SceneEvent {
            public SceneEventType type;
            public SceneObject obj;
            public double time;
        }

        public class SEComparer : IComparer<SceneEvent>
        {
            public int Compare([AllowNull] SceneEvent x, [AllowNull] SceneEvent y)
            {
                var dif = x.time - y.time;
                if(dif == 0.0) {
                    return 0;
                } else {
                    return Math.Sign(dif);
                }
            }
        }

        public IList<PlayerCircle> Circles { get; set; } = new List<PlayerCircle>();
        public IList<PlayerRect> Rects { get; set; } = new List<PlayerRect>();
        public IList<PlayerArrow> Arrows { get; set; } = new List<PlayerArrow>();
        public IList<PlayerLine> Lines {get ; set; } = new List<PlayerLine>();
        public IList<PlayerQSpline> QSplines { get; set; } = new List<PlayerQSpline>();
        public IList<Player2DText> Texts2D {get ; set ;} = new List<Player2DText>();

        [NonSerialized]
        public Dictionary<int, SceneObject> sceneObjects = new Dictionary<int, SceneObject>();

        public void Add(SceneObject obj) {
            switch(obj) {
                case PlayerCircle c1:
                Circles.Add(c1);
                break;
                case PlayerRect pr1:
                Rects.Add(pr1);
                break;
                case PlayerArrow a1:
                Arrows.Add(a1);
                break;
                case PlayerLine l1:
                Lines.Add(l1);
                break;
                case PlayerQSpline q1:
                QSplines.Add(q1);
                break;
                case Player2DText t1:
                Texts2D.Add(t1);
                break;
                default:
                throw new ArgumentException("Unhandled scene object");
            }
        }

        [NonSerialized]
        public object sceneLock = new object();

        [NonSerialized]
        private List<SceneEvent> SceneEvents;
        public double LastTime = 0.0; // AnimationTime.T scene is currently set up for
        SceneObject[] GetObjects() {
            return Circles.Select(x => (SceneObject)x)
                .Concat(Arrows.Select(x => (SceneObject)x))
                .Concat(Lines.Select(x => (SceneObject)x))
                .Concat(Texts2D.Select(x => (SceneObject)x)).ToArray()
                .Concat(QSplines.Select(x => (SceneObject)x)).ToArray()
                .Concat(Rects.Select(x => (SceneObject)x)).ToArray();
        }

        public List<SceneEvent> GenerateEvents() {
            var ret = new List<SceneEvent>();
            var objs = GetObjects();

            foreach(var obj in objs) {
                var createe = new SceneEvent() {
                    type = SceneEventType.Create,
                    obj = obj,
                    time = obj.timeslice.Item1,
                };
                var dele = new SceneEvent() {
                    type = SceneEventType.Delete,
                    obj = obj,
                    time = obj.timeslice.Item2,
                };
                ret.Add(createe);
                ret.Add(dele);
            }
            ret.Sort(new SEComparer());

            return ret;
        }

        public void DestroyObject(SceneObject obj) {
            switch(obj) {
                case PlayerCircle c1:
                Circles.Remove(c1);
                break;
                case PlayerRect pr1:
                Rects.Remove(pr1);
                break;
                case PlayerArrow a1:
                Arrows.Remove(a1);
                break;
                case PlayerLine l1:
                Lines.Remove(l1);
                break;
                case PlayerQSpline q1:
                QSplines.Remove(q1);
                break;
                case Player2DText t1:
                Texts2D.Remove(t1);
                break;
            }
            UpdateEvents();
        }

        public void UpdateEvents() {
            SceneEvents = GenerateEvents();
        }

        public SceneObject GetSceneObjectById(int id) {
            SceneObject ret = null;
            if(sceneObjects.TryGetValue(id, out ret)) {
                return ret;
            }
            return null;
        }

        public VisualEntity GetSceneEntityByName(string name) {
            // TODO: use hash map!
            var objs = GetObjects();
            foreach(var obj in objs) {
                if(obj.name == name){ 
                    var ent = World.current.FindEntityByCreator(obj);
                    if(ent == null) {
                        // TODO: player error
                        Debug.Error($"Scene entity {name} exists, but is either already destroyed or not created yet!");
                    }
                    return ent;
                }
            }
            Debug.Error($"Scene entity {name} does not exist");
            return null;
        }

        public VisualEntity[] GetSceneEntitiesByName(string pattern) {
            throw new NotImplementedException();
            /*Regex ex = new Regex(pattern);
            var objs = GetObjects();
            var sos = objs.Where(x => ex.IsMatch(x.name)).OrderBy(x => x.name);
            return sos.Select(x => x.worldRef).ToArray();*/
        }

        // TODO: this needs to be refactored
        public void ManageSceneObjects(World world) {
            if(SceneEvents == null) {
                UpdateEvents();
            }
            foreach(var e in SceneEvents) {
                if(e.time < LastTime) {
                    continue;
                } else if(e.time >= LastTime && e.time < Time.T && e.time != 0.0 || e.time == 0.0 && Time.T == 0.0) {
                    if(e.type == SceneEventType.Create) {
                        VisualEntity ent = null;
                        world.StartEditing(e.obj);
                        switch(e.obj) {
                            case PlayerCircle pc:
                            var c = new Circle();
                            c.Radius = pc.radius;
                            c.Color = pc.color;
                            c.Transform.Pos = pc.transform.Pos;
                            c.Transform.Rot = pc.transform.Rot;
                            world.CreateInstantly(c);
                            ent = c;
                            break;
                            case PlayerArrow pa:
                            var a = new Arrow2D();
                            a.StartPoint = pa.transform.Pos + pa.start;
                            a.EndPoint = pa.transform.Pos + pa.end;
                            a.Width = pa.width;
                            a.StartColor = pa.startColor;
                            a.EndColor= pa.endColor;
                            world.CreateInstantly(a);
                            ent = a;
                            break;
                            case PlayerLine pl:
                            var l = new SolidLine();
                            l.Points = new Vector3[] {pl.transform.Pos+ pl.start, pl.transform.Pos + pl.end};
                            l.Width = pl.width;
                            l.Color = pl.color;
                            world.CreateInstantly(l);
                            ent = l;
                            break;
                            case Player2DText t1:
                            var t = new Text2D();
                            t.Transform.Pos = t1.transform.Pos;
                            t.Size = t1.size;
                            t.Color = t1.color;
                            t.Text = t1.text;
                            world.CreateInstantly(t);
                            ent = t;
                            break;
                            case PlayerQSpline q1:
                            var qs = new BezierSpline();
                            qs.Points = q1.points.ToArray();
                            qs.Color = q1.color;
                            qs.Width = q1.width;
                            qs.Transform.Pos = q1.transform.Pos;
                            world.CreateInstantly(qs);
                            ent = qs;
                            break;
                            case PlayerRect pr1:
                            var pr = new Rectangle();
                            pr.Width = pr1.size.x;
                            pr.Height = pr1.size.y;
                            pr.Color = pr1.color;
                            pr.Transform.Pos = pr1.transform.Pos;
                            world.CreateInstantly(pr);
                            ent = pr;
                            break;
                        }
                        world.EndEditing();
                        sceneObjects[ent.EntityId] = e.obj;
                    } else if(e.type == SceneEventType.Delete) {
                        var ent = world.FindEntityByCreator(e.obj);
                        if(ent == null) {
                            Debug.Error($"Scene destroying entity that isn't created by us {e.obj.name}");
                        }
                        world.Destroy(ent);
                    }
                } else {
                    break;
                }
            }
            LastTime = Time.T;
        }
    }
}
