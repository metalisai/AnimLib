using System;

namespace AnimLib;

/// <summary>
/// A quaternion representing a rotation.
/// </summary>
[Serializable]
public struct Quaternion {
    /// <summary>
    /// The real component of the quaternion.
    /// </summary>
    public float w;
    /// <summary>
    /// The imaginary component of the quaternion.
    /// </summary>
    public float x, y, z;

    /// <summary>
    /// Property for the real component of the quaternion. For serialization purposes.
    /// </summary>
    public float X {
        get { return x; }
        set { x = value; }
    }

    /// <summary>
    /// Property for the imaginary component of the quaternion. For serialization purposes.
    /// </summary>
    public float Y {
        get { return y; }
        set { y = value; }
    }

    /// <summary>
    /// Property for the imaginary component of the quaternion. For serialization purposes.
    /// </summary>
    public float Z {
        get { return z; }
        set { z = value; }
    }

    /// <summary>
    /// Property for the real component of the quaternion. For serialization purposes.
    /// </summary>
    public float W {
        get { return w; }
        set { w = value; }
    }

    /// <summary>
    /// The identity quaternion. Represents no rotation.
    /// </summary>
    public static readonly Quaternion IDENTITY = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    /// Create a new quaternion from individual components.
    /// </summary>
    public Quaternion(float w, float x, float y, float z) {
        this.w = w;
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// <summary>
    /// Create a new quaternion from a rotation matrix. Assumes the matrix is orthonormal.
    /// </summary>
    public Quaternion(ref M3x3 m) {
        this.w = (float)Math.Sqrt(1.0 + m.m11 + m.m22 + m.m33) / 2.0f;
        this.x = (m.m32 - m.m23)/(4.0f * this.w);
        this.y = (m.m13 - m.m31)/(4.0f * this.w);
        this.z = (m.m21 - m.m12)/(4.0f * this.w);
    }

    /// <summary>
    /// Create a new quaternion from an angle and axis. Represents a rotation around the axis by given angle in radians.
    /// </summary>
    public static Quaternion AngleAxis(float angleRad, Vector3 axis) {
        var ret = new Quaternion();
        float halfAngle = angleRad * 0.5f;
        float sinH = (float)Math.Sin(halfAngle);
        ret.w = (float)Math.Cos(halfAngle); 
        ret.x = axis.x * sinH;
        ret.y = axis.y * sinH;
        ret.z = axis.z * sinH;
        return ret;
    }

    /// <summary>
    /// Rotate a vector by this quaternion. Equivalent to multiplying the vector by the rotation matrix represented by this quaternion.
    /// </summary>
    public static Vector3 operator*(Quaternion r, Vector3 l) {
        var rot = M4x4.Rotate(r);
        var vh = new Vector4(l.x, l.y, l.z, 1.0f);
        var rh = rot*vh;
        return new Vector3(rh.x, rh.y, rh.z);
    }

    /// <summary>
    /// Multiply two quaternions together. Represents the rotation of the first quaternion followed by the rotation of the second quaternion.
    /// </summary>
    public static Quaternion operator*(Quaternion r, Quaternion l) {
        Quaternion ret = new Quaternion();
        ret.w = l.w * r.w - l.x * r.x - l.y * r.y - l.z * r.z;
        ret.x = l.w * r.x + l.x * r.w + l.y * r.z - l.z * r.y;
        ret.y = l.w * r.y - l.x * r.z + l.y * r.w + l.z * r.x;
        ret.z = l.w * r.z + l.x * r.y - l.y * r.x + l.z * r.w;
        return ret;
    }

}
