using System;

namespace AnimLib;

/// <summary>
/// Math utilities.
/// </summary>
public static class AMath {

    /// <summary>
    /// Floating point modulus.
    /// </summary>
    public static float Mod(float a,float b) {
        return (MathF.Abs(a * b) + a) % b;
    }

    // TODO: write tests for this
    // not sure if this is correct
    /// <summary>
    /// Check if two line segments intersect.
    /// </summary>
    public static bool SegmentsIntersect(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2) {
        var d1 = e1 - s1;
        var d2 = e2 - s2;
        var d = s1 - s2;
        var denom = d2.y * d1.x - d2.x * d1.y;
        if (denom == 0.0f) {
            return false;
        }
        var t1 = (d2.x * d.y - d2.y * d.x) / denom;
        var t2 = (d1.x * d.y - d1.y * d.x) / denom;
        return t1 >= 0.0f && t1 <= 1.0f && t2 >= 0.0f && t2 <= 1.0f;
    }

    /// <summary>
    /// Intersect two 2D rays.
    /// </summary>
    public static Vector2? IntersectRays(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2) {
        var det = d1.x*d2.y - d1.y*d2.x;
        var t = (d2.y*(p2.x-p1.x) - d1.x*(p2.y-p1.y)) / det;
        return det <= float.Epsilon ? null : p1 + t * d1;
    }
}
