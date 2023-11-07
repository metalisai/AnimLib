using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AnimLib;

/// <summary>
/// Mode for drawing a shape.
/// </summary>
public enum ShapeMode {
    /// <summary>
    /// Don't draw anything.
    /// </summary>
    None,
    /// <summary>
    /// Draw as a line contour.
    /// </summary>
    Contour,
    /// <summary>
    /// Draw a line contour and fill the shape.
    /// </summary>
    FilledContour,
    /// <summary>
    /// Draw only the fill.
    /// </summary>
    Filled,
}

/// <summary>
/// Single element of a shape path.
/// </summary>
public enum PathVerb {
    /// <summary>
    /// No operation.
    /// </summary>
    Noop,
    /// <summary>
    /// Move without a stroke.
    /// </summary>
    Move, // move without stroke
    /// <summary>
    /// Straight line.
    /// </summary>
    Line, // straight line
    /// <summary>
    /// Cubic curve.
    /// </summary>
    Cubic, // cubic curve
    /// <summary>
    /// Cubic curve with conic weight. Can exactly represent conic sections.
    /// </summary>
    Conic, // circular or conic arc
    /// <summary>
    /// Quadratic curve.
    /// </summary>
    Quad, // quadratic curve
    /// <summary>
    /// Close the contour.
    /// </summary>
    Close, // close contour
}

/// <summary>
/// Data for a single path verb.
/// </summary>
[Serializable]
public struct VerbData {
    /// <summary>
    /// Points for the verb.
    /// </summary>
    [JsonInclude]
    public Vector2[] points;
    /// <summary>
    /// Conic weight for the verb (only used for <see cref="PathVerb.Conic"/>).
    /// </summary>
    [JsonInclude]
    public float conicWeight;

    /// <summary>
    /// Create a new <see cref="VerbData"/> instance.
    /// </summary>
    [JsonConstructor]
    public VerbData(Vector2[] points, float conicWeight) {
        this.points = points;
        this.conicWeight = conicWeight;
    }
}

/// <summary>
/// A class that encapsulates a list of path verbs.
/// </summary>
public class ShapePath {
    /// <summary>
    /// The path verbs.
    /// </summary>
    public (PathVerb verb, VerbData data)[] path;

    /// <summary>
    /// A (single) path that consists of only straight lines.
    /// </summary>
    public struct LinearPath {
        /// <summary>
        /// Connected points.
        /// </summary>
        public Vector2[] points;
        /// <summary>
        /// Whether the path is closed (are start and end connected).
        /// </summary>
        public bool closed;

        /// <summary>
        /// Convert the path to a <see cref="ShapePath"/>.
        /// </summary>
        public ShapePath ToPath()
        {
            var builder = new PathBuilder();
            builder.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
            {
                builder.LineTo(points[i]);
            }
            if (closed)
            {
                builder.Close();
            }
            return builder.GetPath();
        }
    }

    /// <summary>
    /// Evaluate a verb path at a given t (in the range [0,1]).
    /// </summary>
    public static Vector2 EvaluateVerb(PathVerb verb, VerbData data, float t)
    {
        switch (verb)
        {
            case PathVerb.Move:
                return data.points[0];
            case PathVerb.Line:
                return data.points[0] + (data.points[1] - data.points[0]) * t;
            case PathVerb.Quad:
                return BezierCurve.Quadratic(data.points[0], data.points[1], data.points[2], t);
            case PathVerb.Cubic:
                return BezierCurve.Cubic(data.points[0], data.points[1], data.points[2], data.points[3], t);
            case PathVerb.Conic:
                return BezierCurve.Conic(data.points[0], data.points[1], data.points[2], data.conicWeight, t);
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts the path into a list of linear splines.
    /// </summary>
    public LinearPath[] Linearize(int segments)
    {
        List<List<(PathVerb verb, VerbData data)>> shapes = new List<List<(PathVerb verb, VerbData data)>>();

        Vector2 lastPos = Vector2.ZERO;
        List<(PathVerb verb, VerbData data)> currentShape = new();
        foreach(var (verb, data) in path) {
            switch (verb)
            {
                case PathVerb.Move:
                    lastPos = data.points[0];
                    if (currentShape.Count > 0)
                    {
                        shapes.Add(currentShape);
                    }
                    currentShape = new();
                    break;
                case PathVerb.Close:
                    currentShape.Add((verb, data));
                    if (currentShape.Count > 0)
                    {
                        shapes.Add(currentShape);
                    }
                    currentShape = new();
                    break;
                case PathVerb.Line:
                case PathVerb.Conic:
                case PathVerb.Cubic:
                case PathVerb.Quad:
                    currentShape.Add((verb, data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        LinearPath[] ret = new LinearPath[shapes.Count];
        int si = 0;
        foreach (var shape in shapes)
        {
            bool hasClose = shape[^1].verb == PathVerb.Close;
            int curveCount = hasClose ? shape.Count - 1 : shape.Count;
            float maxT = (float)curveCount;
            float segT = maxT / segments;
            float t = segT;
            List<Vector2> points = new List<Vector2>();
            points.Add(shape[0].data.points[0]);
            float lastT = 0.0f;
            for (int i = 1; i < segments+1; i++)
            {
                var idx = (int)Math.Floor(t);
                float curT = t - idx;
                // reposition to the start of the curve if we crossed a curve boundary
                if (Math.Floor(lastT) != Math.Floor(t))
                {
                    curT = 0.0f;
                }
                if (idx >= curveCount)
                {
                    idx = curveCount - 1;
                    curT = 1.0f;
                }
                var verb = shape[idx].verb;
                var data = shape[idx].data;
                var point = EvaluateVerb(verb, data, curT);
                points.Add(point);
                lastT = t;
                t += segT;
            }
            var lpath = new LinearPath() {
                points = points.ToArray(),
                closed = hasClose
            };
            ret[si] = lpath;
            si++;
        }
        return ret;
    }

    /// <summary>
    /// Clone the path.
    /// </summary>
    public ShapePath Clone() {
        var cp = new (PathVerb verb, VerbData data)[path.Length];
        path.CopyTo(cp.AsSpan());
        return new ShapePath() {
            path = cp,
        };
    }
}

/// <summary>
/// A class that can be used to build a <see cref="ShapePath"/>.
/// </summary>
public class PathBuilder {
    private Vector2 lastPos = Vector2.ZERO;

    public List<(PathVerb verb, VerbData data)> path = new ();

    /// <summary>
    /// Move without a stroke.
    /// </summary>
    public void MoveTo(Vector2 pos) {
        var vd = new VerbData() {
            points = new Vector2[1] { pos },
        };
        path.Add((PathVerb.Move, vd));
        lastPos = pos;
    }

    /// <summary>
    /// Straight line from the last position to the given position.
    /// </summary>
    public void LineTo(Vector2 pos) {
        var vd = new VerbData() {
            points = new Vector2[2] { lastPos, pos},
        };
        path.Add((PathVerb.Line, vd));
        lastPos = pos;
    }

    /// <summary>
    /// Quadratic curve from the last position to given control point and end point.
    /// </summary>
    public void QuadTo(Vector2 p1, Vector2 p2) {
        var vd = new VerbData() {
            points = new Vector2[3] { lastPos, p1, p2 },
        };
        path.Add((PathVerb.Quad, vd));
        lastPos = p2;
    }

    /// <summary>
    /// Cubic curve from the last position to given control points and end point.
    /// </summary>
    public void CubicTo(Vector2 p1, Vector2 p2, Vector3 p3) {
        var vd = new VerbData() {
            points = new Vector2[4] { lastPos, p1, p2, p3 },
        };
        path.Add((PathVerb.Cubic, vd));
        lastPos = p3;
    }

    /// <summary>
    /// Conic curve from the last position to given control point and end point.
    /// </summary>
    public void ConicTo(Vector2 p1, Vector2 p2, float weight) {
        var vd = new VerbData() {
            points = new Vector2[3] { lastPos, p1, p2},
            conicWeight = weight
        };
        path.Add((PathVerb.Conic, vd));
        lastPos = p2;
    }

    /// <summary>
    /// Stroke an axis-aligned rectangle shape given two corners.
    /// </summary>
    public void Rectangle(Vector2 min, Vector2 max) {
        MoveTo(min);
        LineTo(new Vector2(max.x, min.y));
        LineTo(max);
        LineTo(new Vector2(min.x, max.y));
        Close();
    }

    /// <summary>
    /// Stroke a circle shape using conic curves, given a center and radius.
    /// </summary>
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

    /// <summary>
    /// Close the contour path.
    /// </summary>
    public void Close() {
        var vd = new VerbData() {
            points = new Vector2[] { lastPos },
        };
        path.Add((PathVerb.Close, vd));
    }

    /// <summary>
    /// Clear all path data.
    /// </summary>
    public void Clear() {
        lastPos = Vector3.ZERO;
        path.Clear();
    }

    /// <summary>
    /// Convert the path builder to a <see cref="ShapePath"/>.
    /// </summary>
    public ShapePath GetPath() {
        return this;
    }
    
    /// <summary>
    /// Debug string representation of the path builder.
    /// </summary>
    public override string ToString() {
        var sb = new System.Text.StringBuilder();
        sb.Append("PathBuilder:\n");
        foreach(var (verb, data) in path) {
            sb.Append("  ");
            sb.Append(verb.ToString());
            sb.Append("    ");
            foreach(var p in data.points) {
                sb.Append(p.ToString());
                sb.Append(" ");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Implicit conversion to <see cref="ShapePath"/>.
    /// </summary>
    public static implicit operator ShapePath(PathBuilder pb) => new ShapePath() { path = pb.path.ToArray()};
}

/// <summary>
/// Internal state of a <see cref="Shape"/>.
/// </summary>
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

/// <summary>
/// A 2D shape made up of a contour and a fill.
/// </summary>
public class Shape : VisualEntity2D, IColored {
    internal Shape(ShapeState state) : base(state) {
    }

    /// <summary>
    /// Create a new shape from a path.
    /// </summary>
    public Shape(ShapePath path) : base(new ShapeState(path)) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Shape(Shape s) : base(s) {
    }

    /// <summary>
    /// The path of the shape.
    /// </summary>
    public ShapePath Path {
        get {
            return ((ShapeState)state).path;
        }
        set {
            World.current.SetProperty(this, "Path", value, ((ShapeState)state).path);
            ((ShapeState)state).path = value;
        }
    }

    /// <summary>
    /// The color of the shape. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Filled"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public Color Color {
        get {
            return ((ShapeState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((ShapeState)state).color);
            ((ShapeState)state).color = value;
        }
    }

    /// <summary>
    /// The color of the contour. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Contour"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public Color ContourColor {
        get {
            return ((ShapeState)state).contourColor;
        }
        set {
            World.current.SetProperty(this, "ContourColor", value, ((ShapeState)state).contourColor);
            ((ShapeState)state).contourColor = value;
        }
    }

    /// <summary>
    /// The size of the contour. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Contour"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public float ContourSize {
        get {
            return ((ShapeState)state).contourSize;
        }
        set {
            World.current.SetProperty(this, "ContourSize", value, ((ShapeState)state).contourSize);
            ((ShapeState)state).contourSize = value;
        }
    }

    /// <summary>
    /// The drawing mode of the shape.
    /// </summary>
    public ShapeMode Mode {
        get {
            return ((ShapeState)state).mode;
        }
        set {
            World.current.SetProperty(this, "Mode", value, ((ShapeState)state).mode);
            ((ShapeState)state).mode = value;
        }
    }

    /// <summary>
    /// Clone the shape.
    /// </summary>
    public override object Clone() {
        return new Shape(this);
    }
}
