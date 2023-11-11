using System;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A class that can be used to build a <see cref="ShapePath"/>.
/// </summary>
public class PathBuilder {
    private Vector2 lastPos = Vector2.ZERO;

    /// <summary>
    /// The path verbs built so far.
    /// </summary>
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
        // counter-clockwise
        var topLeft = new Vector2(min.x, max.y);
        var bottomLeft = min;
        var bottomRight = new Vector2(max.x, min.y);
        var topRight = max;
        MoveTo(topRight);
        LineTo(topLeft);
        LineTo(bottomLeft);
        LineTo(bottomRight);
        Close();
    }

    /// <summary>
    /// Stroke a circle shape using conic curves, given a center and radius.
    /// </summary>
    public void Circle(Vector2 center, float radius, bool cw = false) {
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
        if (cw)
        {
            ConicTo(cp4, end3, w);
            ConicTo(cp3, end2, w);
            ConicTo(cp2, end1, w);
            ConicTo(cp1, start, w);
        }
        else
        {
            ConicTo(cp1, end1, w);
            ConicTo(cp2, end2, w);
            ConicTo(cp3, end3, w);
            ConicTo(cp4, start, w);
        }
        Close();
    }

    /// <summary>
    /// Stroke a torus shape given a center, outer radius and inner radius.
    /// </summary>
    public void Torus(Vector2 center, float outerRadius, float innerRadius)
    {
        Circle(center, outerRadius);
        Circle(center, innerRadius, cw: true);
    }

    /// <summary>
    /// Stroke a N-pointed star shape given an outer radius, inner radius and number of points.
    /// </summary>
    public void Star(float outerR, float innerR, int points)
    {
        var angle = 0.0f;
        var angleStep = (float)(Math.PI * 2.0f / points);
        var outer = new Vector2(outerR, 0.0f);
        var inner = new Vector2(innerR, 0.0f);
        var center = Vector2.ZERO;

        MoveTo(center + inner);

        for (int i = 0; i < points; i++)
        {
            LineTo(center + inner.Rotated(angle));
            LineTo(center + outer.Rotated(angle + angleStep / 2.0f));
            angle += angleStep;
        }
        if (points % 2 == 0)
        {
            LineTo(center + inner);
        }
        Close();
    }

    /// <summary>
    /// Stroke a heart shape given a center and a radius.
    /// </summary>
    public void Heart(float radius, Vector2 center = default(Vector2))
    {
        var vtipY = 0.85f * radius * 2.0f;
        var lowerCpX = 0.96f * radius * 2.0f;
        var lowerCpY = 0.68f * radius * 2.0f;
        var upperCpX = 0.28f * radius * 2.0f;
        var upperCpY = 1.3f * radius * 2.0f;

        center += new Vector2(0.0f, -0.5f*radius);

        CubicBezier<Vector2, float> curve1 = new (
            center + new Vector2(0.0f, vtipY),
            center + new Vector2(-upperCpX, upperCpY),
            center + new Vector2(-lowerCpX, lowerCpY),
            center + new Vector2(0.0f, 0.0f)
        );
        CubicBezier<Vector2, float> curve2 = new (
            curve1.p3,
            center + new Vector2(lowerCpX, lowerCpY),
            center + new Vector2(upperCpX, upperCpY),
            curve1.p0
        );

        // split to 4 curves so that we can start at middle right (the mathematical 0 angle)
        var (q1, q2) = CubicSpline.CollapsePair((curve1, curve2), curve1, 1.0f);
        var (q3, q4) = CubicSpline.CollapsePair((curve1, curve2), curve2, 1.0f);

        MoveTo(q4.p0);
        CubicTo(q4.p1, q4.p2, q4.p3);
        CubicTo(q1.p1, q1.p2, q1.p3);
        CubicTo(q2.p1, q2.p2, q2.p3);
        CubicTo(q3.p1, q3.p2, q3.p3);
        Close();
    }

    /// <summary>
    /// Stroke a grid given a unit size, min and max coordinates.
    /// </summary>
    public void Grid(float unit, Vector2 min, Vector2 max)
    {
        double width = max.x - min.x;
        double height = max.y - min.y;
        var xSteps = (int)Math.Ceiling((double)width / (double)unit);
        var ySteps = (int)Math.Ceiling((double)height / (double)unit);
        for (int i = 0; i <= xSteps; i++)
        {
            var x = min.x + i * unit;
            MoveTo(new Vector2((float)x, min.y));
            LineTo(new Vector2((float)x, max.y));
        }
        for (int i = 0; i <= ySteps; i++)
        {
            var y = min.y + i * unit;
            MoveTo(new Vector2(min.x, (float)y));
            LineTo(new Vector2(max.x, (float)y));
        }
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
