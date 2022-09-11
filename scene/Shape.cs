using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnimLib;

// tuples don't seem to be serializable, so we need this..
[Serializable]
public struct PlayerPathVerb {
    [JsonInclude]
    public PathVerb verb;
    [JsonInclude]
    public VerbData data;
}

[Serializable]
public class PlayerShape : SceneObject2D
{
    [ShowInEditor]
    public Color color { get; set; } = Color.VIOLET;
    [ShowInEditor]
    public Color outline { get; set; } = Color.BLACK;
    [ShowInEditor]
    public float outlineWidth { get; set; } = 0.0f;
    [ShowInEditor]
    public ShapeMode mode { get; set; } = ShapeMode.FilledContour;

    public PlayerPathVerb[] path { get; set; }

    public PlayerShape(string canvasName) : base() {
        this.CanvasName = canvasName; 
        var pb = new PathBuilder();
        pb.MoveTo(Vector2.ZERO);
        pb.CubicTo(new Vector2(2.0f, 0.0f), new Vector2(2.0f, 2.0f), new Vector2(3.0f, 3.0f));
        pb.CubicTo(new Vector2(4.0f, 4.0f), new Vector2(5.0f, 4.0f), new Vector2(6.0f, 4.0f));
        this.path = pb.GetPath().path.Select(x => new PlayerPathVerb() { verb = x.Item1, data = x.Item2}).ToArray();
    }

    [JsonConstructor]
    public PlayerShape() : base() {
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }

    public override Vector2[] GetHandles2D()
    {
        var points = new List<Vector2>();
        foreach(var verb in this.path) {
            var vtype = verb.verb;
            switch(vtype) {
                case PathVerb.Move:
                    points.Add(transform.Pos + verb.data.points[0]);
                    break;
                case PathVerb.Conic:
                    points.Add(transform.Pos + verb.data.points[1]);
                    points.Add(transform.Pos + verb.data.points[2]);
                    break;
                case PathVerb.Quad:
                    points.Add(transform.Pos + verb.data.points[1]);
                    points.Add(transform.Pos + verb.data.points[2]);
                    break;
                case PathVerb.Cubic:
                    points.Add(transform.Pos + verb.data.points[1]);
                    points.Add(transform.Pos + verb.data.points[2]);
                    points.Add(transform.Pos + verb.data.points[3]);
                    break;
                case PathVerb.Line:
                    points.Add(transform.Pos + verb.data.points[1]);
                    break;
            }
        }
        return points.ToArray();
    }

    public override VisualEntity2D InitializeEntity()
    {
        var path = this.path.Select(x => (x.verb, x.data)).ToArray();
        var shape = new Shape(new ShapePath() { path = path});
        shape.Color = this.color;
        shape.ContourColor = this.outline;
        shape.ContourSize = this.outlineWidth;
        shape.Mode = this.mode;
        shape.Transform.Pos = transform.Pos;
        shape.Transform.Rot = transform.Rot;
        return shape;
    }

    public override bool Intersects(Vector2 point)
    {
#warning scene/Shape Intersects not implemented
        if((point - transform.Pos).Length < 1.0f) {
            return true;
        }
        return false;
    }

    public override void SetHandle(int id, Vector2 wpos)
    {
#warning scene/Shape SetHandle not implemented
        int i = 0;
        foreach(var verb in this.path) {
            var vtype = verb.verb;
            switch(vtype) {
                case PathVerb.Move:
                    if(i == id) {
                        verb.data.points[0] = wpos - transform.Pos;
                    }
                    i++;
                    break;
                case PathVerb.Line:
                    if(i == id) {
                        verb.data.points[1] = wpos - transform.Pos;
                    }
                    i++;
                    break;
                case PathVerb.Conic:
                case PathVerb.Quad:
                    if(i == id) {
                        verb.data.points[1] = wpos - transform.Pos;
                    } else if(i + 1 == id) {
                        verb.data.points[2] = wpos - transform.Pos;
                    }
                    i+=2;
                    break;
                case PathVerb.Cubic:
                    if(i == id) {
                        verb.data.points[1] = wpos - transform.Pos;
                    } else if(i + 1 == id) {
                        verb.data.points[2] = wpos - transform.Pos;
                    } else if(i + 2 == id) {
                        verb.data.points[3] = wpos - transform.Pos;
                    }
                    i+=3;
                    break;
            }
        }

    }
}
