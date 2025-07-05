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
    /// An imaginary component of the quaternion.
    /// </summary>
    public float x, y, z;

    /// <summary>
    /// Property for an imaginary X component of the quaternion. For serialization purposes.
    /// </summary>
    public float X {
        get { return x; }
        set { x = value; }
    }

    /// <summary>
    /// Property for an imaginary Y component of the quaternion. For serialization purposes.
    /// </summary>
    public float Y {
        get { return y; }
        set { y = value; }
    }

    /// <summary>
    /// Property for an imaginary Z component of the quaternion. For serialization purposes.
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
        float trace = m.m11 + m.m22 + m.m33;
        if (trace > 0) {
            float s = (float)Math.Sqrt(trace + 1.0f) * 2f; // s=4*w
            this.w = 0.25f * s;
            this.x = (m.m32 - m.m23) / s;
            this.y = (m.m13 - m.m31) / s;
            this.z = (m.m21 - m.m12) / s;
        } else if (m.m11 > m.m22 && m.m11 > m.m33) {
            float s = (float)Math.Sqrt(1.0f + m.m11 - m.m22 - m.m33) * 2f; // s=4*x
            this.w = (m.m32 - m.m23) / s;
            this.x = 0.25f * s;
            this.y = (m.m12 + m.m21) / s;
            this.z = (m.m13 + m.m31) / s;
        } else if (m.m22 > m.m33) {
            float s = (float)Math.Sqrt(1.0f + m.m22 - m.m11 - m.m33) * 2f; // s=4*y
            this.w = (m.m13 - m.m31) / s;
            this.x = (m.m12 + m.m21) / s;
            this.y = 0.25f * s;
            this.z = (m.m23 + m.m32) / s;
        } else {
            float s = (float)Math.Sqrt(1.0f + m.m33 - m.m11 - m.m22) * 2f; // s=4*z
            this.w = (m.m21 - m.m12) / s;
            this.x = (m.m13 + m.m31) / s;
            this.y = (m.m23 + m.m32) / s;
            this.z = 0.25f * s;
        }
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

    /// <summary>
    /// Get a quaternion representing a rotation that rotates from one vector to another, given an up vector.
    /// </summary>
    public static Quaternion LookRotation(Vector3 forward, Vector3 up) {
        forward = forward.Normalized;
        up = up.Normalized;

        Vector3 right = Vector3.Cross(up, forward).Normalized;
        Vector3 newUp = Vector3.Cross(forward, right).Normalized;
        var mat = new M3x3();
        mat.m11 = right.x;
        mat.m21 = right.y;
        mat.m31 = right.z;
        mat.m12 = newUp.x;
        mat.m22 = newUp.y;
        mat.m32 = newUp.z;
        mat.m13 = forward.x;
        mat.m23 = forward.y;
        mat.m33 = forward.z;
        return new Quaternion(ref mat);
    }

    /// <summary>
    /// Interpolate between two quaternions using normalized linear interpolation.
    /// This method has constant angular velocity.
    /// </summary>
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t) {
        float cosHalfTheta = a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;
        if (MathF.Abs(cosHalfTheta) >= 1.0f) {
            return a;
        }
        float halfTheta = MathF.Acos(cosHalfTheta);
        float sinHalfTheta = MathF.Sqrt(1.0f - cosHalfTheta * cosHalfTheta);
        if (MathF.Abs(sinHalfTheta) < 0.001f) {
            return new Quaternion(
                a.w * 0.5f + b.w * 0.5f,
                a.x * 0.5f + b.x * 0.5f,
                a.y * 0.5f + b.y * 0.5f,
                a.z * 0.5f + b.z * 0.5f
            );
        }
        float ratioA = MathF.Sin((1.0f - t) * halfTheta) / sinHalfTheta;
        float ratioB = MathF.Sin(t * halfTheta) / sinHalfTheta;
        return new Quaternion(
            a.w * ratioA + b.w * ratioB,
            a.x * ratioA + b.x * ratioB,
            a.y * ratioA + b.y * ratioB,
            a.z * ratioA + b.z * ratioB
        );
    }

    /// <summary>
    /// Interpolate between two quaternions using normalized linear interpolation.
    /// This method does not have constant angular velocity.
    /// </summary>
    public static Quaternion Nlerp(Quaternion a, Quaternion b, float t) {
        Vector4 v = new Vector4(a.x, a.y, a.z, a.w);
        Vector4 u = new Vector4(b.x, b.y, b.z, b.w);
        Vector4 r = Vector4.Lerp(v, u, t).Normalized;
        return new Quaternion(r.w, r.x, r.y, r.z);
    }
}
