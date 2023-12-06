using System;

namespace AnimLib;

/// <summary>
/// A mathematical ray in 3D space. Ray has an origin and a direction.
/// </summary>
public struct Ray {
    /// <summary> Origin of the ray </summary>
    public Vector3 o;
    /// <summary> Direction of the ray </summary>
    public Vector3 d;

    /// <summary> Intersect this ray with a plane </summary>
    /// <param name="plane"> The plane to intersect with </param>
    /// <returns> The point of intersection, or null if there is no intersection </returns>
    public Vector3? Intersect(Plane plane) {
        float denom = Vector3.Dot(this.d, plane.n);
        if(MathF.Abs(denom) <= float.Epsilon) {
            return null;
        }
        float t = -(Vector3.Dot(this.o, plane.n) + plane.o) / denom;
        if(t < 0) {
            return null;
        }
        Vector3 ret = t*this.d;
        return this.o + ret;
    }
}

/// <summary> A mathematical plane in 3D space. Plane has a normal and an offset. </summary>
public struct Plane {
    /// <summary> Normal vector of the plane </summary>
    public Vector3 n; // normal
    /// <summary> Offset of the plane </summary>
    public float o; // offset

    /// <summary> Create a new plane </summary>
    /// <param name="normal"> The normal vector of the plane </param>
    /// <param name="pointOnPlane"> A point on the plane </param>
    public Plane(Vector3 normal, Vector3 pointOnPlane) {
        n = normal.Normalized;
        o = -Vector3.Dot(n, pointOnPlane);
    }

    /// <summary> String representation of the plane's parameters. </summary>
    public override string ToString() {
        return $"{n.x} {n.y} {n.z} {o}";
    }
}
