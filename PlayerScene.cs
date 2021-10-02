using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AnimLib {

    public class ShowInEditorAttribute : Attribute {

    }

    [Serializable]
    public class SceneTransform {
        public Vector3 Pos {get; set; }
        public Quaternion Rot {get; set; }
        public SceneTransform() {
            
        }
        public SceneTransform(Vector3 p, Quaternion r) {
            this.Pos = p;
            this.Rot = r;
        }
    }
    [Serializable]
    public abstract class SceneObject {
        [ShowInEditor]
        [JsonIgnore]
        public Vector3 position {
            get {
                return transform.Pos;
            }
            set {
                transform.Pos = value;
            }
        }
        [ShowInEditor]
        public double startTime {get; set; }
        [ShowInEditor]
        public double endTime {get; set; }
        [ShowInEditor]
        public string name {get; set; } = Guid.NewGuid().ToString();
        [JsonIgnore]
        public abstract bool Is2D {get;}
        [JsonIgnore]
        public (double,double) timeslice { 
            get { return (startTime, endTime); }
            set { startTime = value.Item1; endTime = value.Item2; }
        }
        // get 2D handles that can be dragged on a surface in the editor
        // (the handles are in world space)
        public abstract Vector3[] GetHandles2D();
        public abstract void SetHandle(int id, Vector3 wpos);
        public abstract object Clone();
        // get surface if 2D object or null if not
        public abstract Plane? GetSurface();
        public SceneTransform transform {get; set; }
        [NonSerialized]
        public VisualEntity worldRef;
        [JsonIgnore]
        public (string, Func<object>, Action<object>)[] Properties { get; protected set;} = new (string, Func<object>, Action<object>)[]{};

        public SceneObject() {
            var props = this.GetType().GetProperties();
            var showProps = props.Where(x => Attribute.IsDefined(x, typeof(ShowInEditorAttribute))).ToArray();
            var propsArr = new (string, Func<object>, Action<object>)[showProps.Length];
            int i = 0;
            foreach(var prop in showProps) {
                (string, Func<object>, Action<object>) cprop = (prop.Name, () => {
                    return prop.GetGetMethod().Invoke(this, new object[]{});
                }, (newvalue) => {
                    prop.GetSetMethod().Invoke(this, new object[] {newvalue});
                });
                propsArr[i] = cprop;
                i++;
            }
            Properties = propsArr;
        }
    }

    [Serializable]
    public class PlayerCircle : SceneObject {
        [ShowInEditor]
        public float radius { get; set; }
        [ShowInEditor]
        public Color color { get; set; } = Color.RED;
        [JsonIgnore]
        public override bool Is2D { get { return false; } }

        public override Vector3[] GetHandles2D() {
            var handle = transform.Pos + transform.Rot*(radius*Vector3.RIGHT);
            return new Vector3[] {handle};
        }
        public override Plane? GetSurface() {
            var norm = this.transform.Rot * -Vector3.FORWARD;
            return new Plane {
                n = norm,
                // TODO: is this right?
                o = -Vector3.Dot(transform.Pos, norm),
            };
        }

        public override void SetHandle(int id, Vector3 wpos) {
            System.Diagnostics.Debug.Assert(id == 0); // circle only has one handle for radius
            float r = (wpos - transform.Pos).Length;
            this.radius = r;
        }

        public override object Clone() {
            return new PlayerCircle() {
                radius = radius,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                color = color,
                timeslice = timeslice,
            };
        }
    }
    public class PlayerArrow : SceneObject {
        [ShowInEditor]
        public float width {get; set; }
        [ShowInEditor]
        public Vector3 start {get ; set; }
        [ShowInEditor]
        public Vector3 end {get ; set; }
        [ShowInEditor]
        public Color startColor {get; set; } = Color.BLACK;
        [ShowInEditor]
        public Color endColor {get; set; } = Color.BLACK;
        [JsonIgnore]
        public override bool Is2D { get { return false; } }
        public override Vector3[] GetHandles2D() {
            var h1 = transform.Pos + start;
            var h2 = transform.Pos + end;
            return new Vector3[] {h1, h2};
        }
        public override Plane? GetSurface() {
            var norm = this.transform.Rot * -Vector3.FORWARD;
            return new Plane {
                n = norm,
                // TODO: is this right?
                o = -Vector3.Dot(transform.Pos, norm),
            };
        }
        public override void SetHandle(int id, Vector3 wpos) {
            if(id == 0) {
                start = wpos - transform.Pos;
            } else {
                end = wpos - transform.Pos;
            }
        }

        public override object Clone() {
            return new PlayerArrow() {
                timeslice = timeslice,
                width = width,
                start = start, 
                end = end,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                startColor = startColor,
                endColor = endColor,
            };
        }
    }

    public class Player2DText : SceneObject {
        [ShowInEditor]
        public float size {get; set;}
        [ShowInEditor]
        public Color color{get; set;}
        [ShowInEditor]
        public string text {get; set;}
        [JsonIgnore]
        public override bool Is2D { get { return true; } }

        public override object Clone()
        {
            return new Player2DText() {
                timeslice = timeslice,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                size = size,
                color = color,
                text = text,
            };
        }

        public override Vector3[] GetHandles2D()
        {
            return new Vector3[0];
        }

        public override Plane? GetSurface()
        {
            return new Plane() {
                o = 0.0f,
                n = new Vector3(0.0f, 0.0f, -1.0f),
            };
        }

        public override void SetHandle(int id, Vector3 wpos)
        {
        }
    }

    public class PlayerLine : SceneObject {
        [ShowInEditor]
        public float width {get; set; }
        [ShowInEditor]
        public Vector3 start {get ; set; }
        [ShowInEditor]
        public Vector3 end {get ; set; }
        [ShowInEditor]
        public Color color {get; set; } = Color.BLACK;
        [JsonIgnore]
        public override bool Is2D { get { return false; } }
        public override Vector3[] GetHandles2D() {
            var h1 = transform.Pos + start;
            var h2 = transform.Pos + end;
            return new Vector3[] {h1, h2};
        }
        public override Plane? GetSurface() {
            var norm = this.transform.Rot * -Vector3.FORWARD;
            var ret = new Plane {
                n = norm,
                // TODO: is this right?
                o = -Vector3.Dot(transform.Pos, norm),
            };
            return ret;
        }
        public override void SetHandle(int id, Vector3 wpos) {
            if(id == 0) {
                start = wpos - transform.Pos;
            } else {
                end = wpos - transform.Pos;
            }
        }

        public override object Clone() {
            return new PlayerLine() {
                timeslice = timeslice,
                color = color,
                start = start, 
                end = end, 
                width = width, 
                transform = new SceneTransform(transform.Pos, transform.Rot)
            };
        }
    }

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
        public IList<PlayerArrow> Arrows { get; set; } = new List<PlayerArrow>();
        public IList<PlayerLine> Lines {get ; set; } = new List<PlayerLine>();
        public IList<Player2DText> Texts2D {get ; set ;} = new List<Player2DText>();

        [NonSerialized]
        public Dictionary<int, SceneObject> sceneObjects = new Dictionary<int, SceneObject>();

        public void Add(SceneObject obj) {
            switch(obj) {
                case PlayerCircle c1:
                Circles.Add(c1);
                break;
                case PlayerArrow a1:
                Arrows.Add(a1);
                break;
                case PlayerLine l1:
                Lines.Add(l1);
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
        public double LastTime = 0.0;
        SceneObject[] GetObjects() {
            return Circles.Select(x => (SceneObject)x)
                .Concat(Arrows.Select(x => (SceneObject)x))
                .Concat(Lines.Select(x => (SceneObject)x))
                .Concat(Texts2D.Select(x => (SceneObject)x)).ToArray();
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
                case PlayerArrow a1:
                Arrows.Remove(a1);
                break;
                case PlayerLine l1:
                Lines.Remove(l1);
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
            /*var objs = GetObjects();
            foreach(var obj in objs) {
                if(obj.worldRef is VisualEntity) {
                    var ent = obj.worldRef as VisualEntity;
                    if(ent.EntityId == id) {
                        return obj;
                    }
                }
            }*/
            return null;
        }

        /*public VisualEntity[] GetSceneEntitiesByName(string start) {
            var objs = GetObjects();
            List<SceneObject> ents = new List<SceneObject>();
            foreach(var obj in objs) {
                if(obj.name.StartsWith(start)){
                    ents.Add(obj);
                }
            }
            return ents.OrderBy(x => x.name).Select(x => x.worldRef).ToArray();;
        }*/

        public VisualEntity GetSceneEntityByName(string name) {
            // TODO: use hash map!
            var objs = GetObjects();
            foreach(var obj in objs) {
                if(obj.name == name){ 
                    return obj.worldRef;
                }
            }
            return null;
        }

        public VisualEntity[] GetSceneEntitiesByName(string pattern) {
            Regex ex = new Regex(pattern);
            var objs = GetObjects();
            var sos = objs.Where(x => ex.IsMatch(x.name)).OrderBy(x => x.name);
            return sos.Select(x => x.worldRef).ToArray();
        }

        public void ManageSceneObjects(World world) {
            if(SceneEvents == null) {
                UpdateEvents();
            }
            foreach(var e in SceneEvents) {
                if(e.time < LastTime) {
                    continue;
                } else if(e.time >= LastTime && e.time < AnimationTime.Time && e.time != 0.0 || e.time == 0.0 && AnimationTime.Time == 0.0) {
                    if(e.type == SceneEventType.Create) {
                        VisualEntity ent = null;
                        switch(e.obj) {
                            case PlayerCircle pc:
                            var c = new Circle(false);
                            c.Radius = pc.radius;
                            c.Color = pc.color;
                            c.Transform.Pos = pc.transform.Pos;
                            c.Transform.Rot = pc.transform.Rot;
                            world.CreateInstantly(c);
                            pc.worldRef = c;
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
                            pa.worldRef = a;
                            ent = a;
                            break;
                            case PlayerLine pl:
                            var l = new SolidLine();
                            l.Points = new Vector3[] {pl.transform.Pos+ pl.start, pl.transform.Pos + pl.end};
                            l.Width = pl.width;
                            l.Color = pl.color;
                            world.CreateInstantly(l);
                            pl.worldRef = l;
                            ent = l;
                            break;
                            case Player2DText t1:
                            var t = new Text2D();
                            t.Transform.Pos = t1.transform.Pos;
                            t.Size = t1.size;
                            t.Color = t1.color;
                            t.Text = t1.text;
                            world.CreateInstantly(t);
                            t1.worldRef = t;
                            ent = t;
                            break;
                        }
                        sceneObjects[ent.EntityId] = e.obj;
                    } else if(e.type == SceneEventType.Delete) {
                        world.Destroy(e.obj.worldRef);
                    }
                } else {
                    break;
                }
            }
            LastTime = AnimationTime.Time;
        }
    }
}
