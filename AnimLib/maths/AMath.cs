using System;

namespace AnimLib;

/// <summary>
/// Math utilities.
/// </summary>
public static class AMath
{

    /// <summary>
    /// Floating point modulus.
    /// </summary>
    public static float Mod(float a, float b)
    {
        return (MathF.Abs(a * b) + a) % b;
    }

    // TODO: write tests for this
    // not sure if this is correct
    /// <summary>
    /// Check if two line segments intersect.
    /// </summary>
    public static bool SegmentsIntersect(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2)
    {
        var d1 = e1 - s1;
        var d2 = e2 - s2;
        var d = s1 - s2;
        var denom = d2.y * d1.x - d2.x * d1.y;
        if (denom == 0.0f)
        {
            return false;
        }
        var t1 = (d2.x * d.y - d2.y * d.x) / denom;
        var t2 = (d1.x * d.y - d1.y * d.x) / denom;
        return t1 >= 0.0f && t1 <= 1.0f && t2 >= 0.0f && t2 <= 1.0f;
    }

    /// <summary>
    /// Intersect two 2D rays.
    /// </summary>
    public static Vector2? IntersectRays(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2)
    {
        var det = d1.x * d2.y - d1.y * d2.x;
        var t = (d2.y * (p2.x - p1.x) - d1.x * (p2.y - p1.y)) / det;
        return det <= float.Epsilon ? null : p1 + t * d1;
    }

    // https://github.com/chengkehan/Line-Triangle-Intersection/blob/master/Assets/LineTriangleIntersection.cs
    // TODO: write tests for this
    /// <summary>
    /// Intersect a 3D ray and a line segment.
    /// </summary>
    public static Vector3? IntersectSegmentTriangle(Vector3 start, Vector3 end, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var n = Vector3.Cross(p2 - p1, p3 - p1).Normalized;
        float d = Vector3.Dot(p1, n);
        Plane plane = new(n, p1);
        float l0 = Vector3.Dot(start, n) - d;
        float l1 = Vector3.Dot(end, n) - d;
        if (l0 * l1 >= 0.0f)
        {
            return null;
        }
        float t = l0 / (l0 - l1);
        Vector3 ld = end - start;
        Vector3 p = start + t * ld;

        Vector3[] points = [p1, p2, p3];
        for (int i = 0; i < 3; i++)
        {
            var edge = points[(i + 1) % 3] - points[i];
            var sd = Vector3.Cross(n, edge);
            var vd = p - points[i];
            float d2 = Vector3.Dot(vd, sd);
            if (d2 < 0.0f)
            {
                return null;
            }
        }
        return p;
    }

    public static Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 start, Vector2 end)
    {
        var dif = end - start;
        var difp = p - start;
        float len2 = dif.x * dif.x + dif.y * dif.y;
        float t = (difp.x * dif.x + difp.y * dif.y) / len2;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var ret = start + dif * t;
        return ret;
    }
    
    public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 start, Vector3 end) {
        var dif = end - start;
        var difp = p - start;
        float len2 = Vector3.Dot(dif, dif);
        if (len2 < 1e-6) return start;
        float t = Vector3.Dot(difp, dif) / len2;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var ret = start + dif * t;
        return ret;
    }
}
