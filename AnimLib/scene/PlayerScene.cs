using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnimLib;

internal class PlayerScene {
    internal enum SceneEventType {
        None,
        Create,
        Delete,
    }

    internal record SceneEvent(SceneEventType type, SceneObject obj, double time);

    internal class SEComparer : IComparer<SceneEvent>
    {
        public int Compare([AllowNull] SceneEvent x, [AllowNull] SceneEvent y)
        {
            if(x == null || y == null) {
                throw new ArgumentNullException();
            }
            var dif = x.time - y.time;
            if(dif == 0.0) {
                return 0;
            } else {
                return Math.Sign(dif);
            }
        }
    }

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    public PlayerScene() {
        SceneEvents = new List<SceneEvent>();
    }

    [JsonConstructor]
    internal PlayerScene(IList<PlayerCircle> circles, IList<PlayerRect> rectangles, IList<PlayerShape> shapes) {
        SceneEvents = new List<SceneEvent>();
        if(circles != null) {
            this.Circles = circles;
            foreach(var c in circles) Objects2D.Add(c);
        }
        if(rectangles != null) {
            this.Rectangles = rectangles;
            foreach(var c in rectangles) Objects2D.Add(c);
        }
        if(shapes != null) {
            this.Shapes = shapes;
            foreach(var c in shapes) Objects2D.Add(c);
        }
        Debug.TLog($"SceneObjects contains {sceneObjects.Count} objects, while objects2d contains {Objects2D.Count} objects");
    }

    [NonSerialized] // this list is only used for optimization (to get all 2d objects at once)
    internal IList<SceneObject2D> Objects2D = new List<SceneObject2D>();

    [JsonInclude]
    internal IList<PlayerCircle> Circles { get; set; } = new List<PlayerCircle>();
    [JsonInclude]
    internal IList<PlayerRect> Rectangles { get; set; } = new List<PlayerRect>();
    [JsonInclude]
    internal IList<PlayerShape> Shapes { get; set; } = new List<PlayerShape>();

    /*public IList<PlayerArrow> Arrows { get; set; } = new List<PlayerArrow>();
    public IList<PlayerLine> Lines {get ; set; } = new List<PlayerLine>();
    public IList<PlayerQSpline> QSplines { get; set; } = new List<PlayerQSpline>();
    public IList<Player2DText> Texts2D {get ; set ;} = new List<Player2DText>();*/

    [NonSerialized]
    internal Dictionary<int, SceneObject> sceneObjects = new Dictionary<int, SceneObject>();

    internal void Add(SceneObject obj) {
        switch(obj) {
            case SceneObject2D o2d:
                Objects2D.Add(o2d);
                switch(o2d) {
                    case PlayerCircle pc:
                        Circles.Add(pc);
                        break;
                    case PlayerRect pr:
                        Rectangles.Add(pr);
                        break;
                    case PlayerShape ps:
                        Shapes.Add(ps);
                        break;
                    default: 
                        throw new ArgumentException("Unknown 2D sceneobject");
                }
                break;
            default:
            throw new ArgumentException("Unhandled scene object");
        }
    }

    [NonSerialized]
    internal object sceneLock = new object();

    [NonSerialized]
    private List<SceneEvent> SceneEvents;
    public double LastTime = 0.0; // AnimationTime.T scene is currently set up for
    SceneObject[] GetObjects() {
        /*return Circles.Select(x => (SceneObject)x)
            .Concat(Arrows.Select(x => (SceneObject)x))
            .Concat(Lines.Select(x => (SceneObject)x))
            .Concat(Texts2D.Select(x => (SceneObject)x)).ToArray()
            .Concat(QSplines.Select(x => (SceneObject)x)).ToArray()
            .Concat(Rects.Select(x => (SceneObject)x)).ToArray();*/
        return Objects2D.Select(x => (SceneObject)x).ToArray();
    }

    internal void GenerateEvents() {
        var ret = new List<SceneEvent>();
        var objs = GetObjects();

        foreach(var obj in objs) {
            var createe = new SceneEvent(SceneEventType.Create, obj, obj.timeslice.Item1);
            var dele = new SceneEvent(SceneEventType.Delete, obj, obj.timeslice.Item2);
            ret.Add(createe);
            ret.Add(dele);
        }
        ret.Sort(new SEComparer());
        this.SceneEvents = ret;
    }

    internal void DestroyObject(SceneObject obj) {
        switch(obj) {
            case SceneObject2D o2d:
            Objects2D.Remove(o2d);
            switch(o2d) {
                case PlayerCircle pc:
                    Circles.Remove(pc);
                    break;
                case PlayerRect pr:
                    Rectangles.Remove(pr);
                    break;
                case PlayerShape ps:
                    Shapes.Remove(ps);
                    break;
            }
            break;
            /*case PlayerCircle c1:
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
            break;*/
        }
        UpdateEvents();
    }

    public void UpdateEvents() {
        GenerateEvents();
    }

    internal SceneObject? GetCanvasObject(string canvasName, Vector2 canvasPos) {
        foreach(var so in sceneObjects.Values) {
            switch(so) {
                case SceneObject2D s2d:
                    if(s2d.CanvasName == canvasName) {
                        if(s2d.Intersects(canvasPos)) {
                            return s2d;
                        }
                    }
                    break;
            }
        }
        return null;
    }

    internal SceneObject? GetSceneObjectById(int id) {
        SceneObject? ret = null;
        if(sceneObjects.TryGetValue(id, out ret)) {
            return ret;
        }
        return null;
    }

    public VisualEntity? GetSceneEntityByName(string name) {
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
        UpdateEvents();
        foreach(var e in SceneEvents) {
            if(e.time < LastTime) {
                continue;
            } else if(e.time >= LastTime && e.time < Time.T && e.time != 0.0 || e.time == 0.0 && Time.T == 0.0) {
                if(e.type == SceneEventType.Create) {
                    world.StartEditing(e.obj);
                    switch(e.obj) {
                        case SceneObject2D so2d:
                            if(so2d is PlayerShape) {
                                Debug.TLog($"Shape transform {so2d.transform.Pos.x} {so2d.transform.Pos.y}");
                            }
                            var c = so2d.InitializeEntity();
                            if(c == null) {
                                Debug.Error("SceneObject2D.InitializeEntity() return null");
                                break;
                            }
                            Func<VisualEntity, bool> canvasMatch = (VisualEntity ce) => {
                                switch(ce) {
                                    case Canvas canvas:
                                        var cs = canvas.state as CanvasState;
                                        if(cs != null && cs.name == so2d.CanvasName) {
                                            System.Diagnostics.Debug.Assert(ce.created);
                                            c.Canvas = canvas;
                                            world.CreateInstantly(c);
                                            sceneObjects[c.state.entityId] = e.obj;
                                            return true;
                                        }
                                        break;
                                }
                                return false;
                            };
                            world.MatchCreation(c, canvasMatch);
                            break;
                    }
                    world.EndEditing();
                }
                    /*switch(e.obj) {
                        case PlayerCircle pc:
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
                        qs.Points = q1.points.Select(x => new Vector3(x, 0.0f)).ToArray();
                        qs.Color = q1.color;
                        qs.Width = q1.width;
                        qs.Transform.Pos = q1.transform.Pos;
                        world.CreateInstantly(qs);
                        ent = qs;
                        break;
                        case PlayerRect pr1:
                        var pr = new Rectangle(pr1.size.x, pr1.size.y);
                        pr.Color = pr1.color;
                        pr.Transform.Pos = pr1.transform.Pos;
                        world.CreateInstantly(pr);
                        ent = pr;
                        break;
                    }
                    world.EndEditing();
                    if(ent != null) {
                        sceneObjects[ent.EntityId] = e.obj;
                    }
                }*/ else if(e.type == SceneEventType.Delete) {
                    var ent = world.FindEntityByCreator(e.obj);
                    if(ent == null) {
                        Debug.Error($"Scene destroying entity that isn't created by us {e.obj.name}");
                    } else {
                        world.Destroy(ent);
                    }
                }
            } else {
                break;
            }
        }
        LastTime = Time.T;
    }
}
