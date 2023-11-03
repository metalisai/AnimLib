using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AnimLib;

public enum ShapeMode {
    None,
    Contour,
    FilledContour,
    Filled,
}

public enum PathVerb {
    Noop, // do nothing
    Move, // move without stroke
    Line, // straight line
    Cubic, // cubic curve
    Conic, // circular or conic arc
    Quad, // quadratic curve
    Close, // close contour
}

[Serializable]
public struct VerbData {
    [JsonInclude]
    public Vector2[] points;
    [JsonInclude]
    public float conicWeight;

    [JsonConstructor]
    public VerbData(Vector2[] points, float conicWeight) {
        this.points = points;
        this.conicWeight = conicWeight;
    }
}

public class ShapePath {
    public (PathVerb,VerbData)[] path;

    public ShapePath Clone() {
        var cp = new (PathVerb,VerbData)[path.Length];
        path.CopyTo(cp.AsSpan());
        return new ShapePath() {
            path = cp,
        };
    }
}

public class PathBuilder {
    private Vector2 lastPos = Vector2.ZERO;

    public List<(PathVerb,VerbData)> path = new List<(PathVerb,VerbData)>();

    public void MoveTo(Vector2 pos) {
        var vd = new VerbData() {
            points = new Vector2[1] { pos },
        };
        path.Add((PathVerb.Move, vd));
        lastPos = pos;
    }

    public void LineTo(Vector2 pos) {
        var vd = new VerbData() {
            points = new Vector2[2] { lastPos, pos},
        };
        path.Add((PathVerb.Line, vd));
        lastPos = pos;
    }

    public void QuadTo(Vector2 p1, Vector2 p2) {
        var vd = new VerbData() {
            points = new Vector2[3] { lastPos, p1, p2 },
        };
        path.Add((PathVerb.Quad, vd));
        lastPos = p2;
    }

    public void CubicTo(Vector2 p1, Vector2 p2, Vector3 p3) {
        var vd = new VerbData() {
            points = new Vector2[4] { lastPos, p1, p2, p3 },
        };
        path.Add((PathVerb.Cubic, vd));
        lastPos = p3;
    }

    public void ConicTo(Vector2 p1, Vector2 p2, float weight) {
        var vd = new VerbData() {
            points = new Vector2[3] { lastPos, p1, p2},
            conicWeight = weight
        };
        path.Add((PathVerb.Conic, vd));
        lastPos = p2;
    }

    public void Rectangle(Vector2 min, Vector2 max) {
        MoveTo(min);
        LineTo(new Vector2(max.x, min.y));
        LineTo(max);
        LineTo(new Vector2(min.x, max.y));
        Close();
    }

    public void Circle(Vector2 center, float radius) {
        var start = center + new Vector2(radius, 0.0f);
        var end1 = center + new Vector2(0.0f, radius);
        var end2 = center + new Vector2(-radius, 0.0f);
        var end3 = center + new Vector2(0.0f, -radius);
        var cp1 = center + new Vector2(radius, radius);
        var cp2 = center + new Vector2(-radius, radius);
        var cp3 = center + new Vector2(-radius, -radius);
        var cp4 = center + new Vector2(radius, -radius);
        MoveTo(start);
        float w = (float)(Math.Sqrt(2) / 2.0);
        ConicTo(cp1, end1, w);
        ConicTo(cp2, end2, w);
        ConicTo(cp3, end3, w);
        ConicTo(cp4, start, w);
        Close();
    }

    public void Close() {
        var vd = new VerbData() {
            points = new Vector2[] { lastPos },
        };
        path.Add((PathVerb.Close, vd));
    }

    public void Clear() {
        lastPos = Vector3.ZERO;
        path.Clear();
    }

    public ShapePath GetPath() {
        return this;
    }

    public static implicit operator ShapePath(PathBuilder pb) => new ShapePath() { path = pb.path.ToArray()};
}

internal class ShapeState : EntityState2D {
    public ShapePath path;
    public Color color = Color.RED;
    public Color contourColor = Color.BLACK;
    public float contourSize = 0.0f;
    public ShapeMode mode = ShapeMode.FilledContour;

    public ShapeState(ShapePath path) {
        this.path = path;
    }

    public ShapeState(ShapeState ss) : base(ss) {
        this.path = ss.path.Clone();
        this.color = ss.color;
        this.contourColor = ss.contourColor;
        this.contourSize = ss.contourSize;
        this.mode = ss.mode;
    }

    public override Vector2 AABB {
        get {
            throw new NotImplementedException();
        }
    }

    public override object Clone() {
        return new ShapeState(this);
    }
}

public class Shape : VisualEntity2D, IColored {
    internal Shape(ShapeState state) : base(state) {
    }

    public Shape(ShapePath path) : base(new ShapeState(path)) {
    }

    public Shape(Shape s) : base(s) {
    }

    public ShapePath Path {
        get {
            return ((ShapeState)state).path;
        }
        set {
            World.current.SetProperty(this, "Path", value, ((ShapeState)state).path);
            ((ShapeState)state).path = value;
        }
    }
    public Color Color {
        get {
            return ((ShapeState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((ShapeState)state).color);
            ((ShapeState)state).color = value;
        }
    }
    public Color ContourColor {
        get {
            return ((ShapeState)state).contourColor;
        }
        set {
            World.current.SetProperty(this, "ContourColor", value, ((ShapeState)state).contourColor);
            ((ShapeState)state).contourColor = value;
        }
    }
    public float ContourSize {
        get {
            return ((ShapeState)state).contourSize;
        }
        set {
            World.current.SetProperty(this, "ContourSize", value, ((ShapeState)state).contourSize);
            ((ShapeState)state).contourSize = value;
        }
    }
    public ShapeMode Mode {
        get {
            return ((ShapeState)state).mode;
        }
        set {
            World.current.SetProperty(this, "Mode", value, ((ShapeState)state).mode);
            ((ShapeState)state).mode = value;
        }
    }

    public override object Clone() {
        return new Shape(this);
    }
}
