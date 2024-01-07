using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

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
    public (PathVerb verb, VerbData data)[] path = Array.Empty<(PathVerb verb, VerbData data)>();

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
            return builder.Build();
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

