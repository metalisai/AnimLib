using System;

namespace AnimLib;

/// <summary>
/// A 4D vector.
/// </summary>
public struct Vector4 {
    /// <summary>
    /// The component of the vector.
    /// </summary>
    public float x, y, z, w;

    /// <summary>
    /// Create a new vector from individual components.
    /// </summary>
    public Vector4(float x, float y, float z, float w) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    /// <summary>
    /// Create a vector from a 2D vector and two components.
    /// </summary>
    public Vector4(Vector3 xy, float z, float w) {
        this.x = xy.x;
        this.y = xy.y;
        this.z = z;
        this.w = w;
    }

    /// <summary>
    /// Create a vector from a 3D vector and a component.
    /// </summary>
    public Vector4(Vector3 v, float w) {
        this.x = v.x;
        this.y = v.y;
        this.z = v.z;
        this.w = w;
    }

    /// <summary>
    /// Normalized version of the vector.
    /// </summary>
    public Vector4 Normalized {
        get {
            float len = MathF.Sqrt(x*x + y*y + z*z + w*w);
            return new Vector4(x/len, y/len, z/len, w/len);
        }
    }

    /// <summary>
    /// Swizzle the xyz components of the vector.
    /// </summary>
    public Vector3 xyz {
        get {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Swizzle the xy components of the vector.
    /// </summary>
    public Vector2 xy {
        get {
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// The length of the vector.
    /// </summary>
    public float Length {
        get {
            return MathF.Sqrt(x*x + y*y + z*z + w*w);
        }
    }

    /// <summary>
    /// Normalize this vector.
    /// </summary>
    public void Normalize() {
        float len = Length;
        x /= len;
        y /= len;
        z /= len;
        w /= len;
    }

    /// <summary>
    /// Convert this vector to a string representation.
    /// </summary>
    public override string ToString() {
        return $"({x},{y},{z},{w})";
    }

    /// <summary>
    /// Construct a vector with components in the range [0,1] from a 32-bit integer. For example RGBA colors.
    /// </summary>
    /// <param name="val">The 32-bit integer to convert.</param>
    public static Vector4 FromInt32(uint val) {
        return new Vector4((float)((val>>24)&0xFF)/255.0f, (float)((val>>16)&0xFF)/255.0f, (float)((val>>8)&0xFF)/255.0f, (float)((val)&0xFF)/255.0f);
    }

    /// <summary>
    /// Component-wise comparison of two vectors.
    /// </summary>
    public static bool operator ==(Vector4 lhs, Vector4 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w  == rhs.w;
    }

    /// <summary>
    /// Component-wise inverted comparison of two vectors.
    /// </summary>
    public static bool operator !=(Vector4 lhs, Vector4 rhs)
    {
        return !(lhs==rhs);
    }

    /// <summary>
    /// Checks if object is a vector and performs component-wise comparison.
    /// </summary>
    public override bool Equals(object obj) {
        return obj is Vector4 && ((Vector4)obj).x == x && ((Vector4)obj).y == y && ((Vector4)obj).z == z;
    }

    /// <summary>
    /// Get a hash code for this vector.
    /// </summary>
    public override int GetHashCode() {
        // TODO: is this a good hash function? (it's not)
        return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
    }

    /// <summary>
    /// Component-wise addition of two vectors.
    /// </summary>
    public static Vector4 operator+ (Vector4 a, Vector4 b) {
        return new Vector4(a.x+b.x, a.y+b.y, a.z+b.z, a.w+b.w);
    }

    /// <summary>
    /// Component-wise subtraction of two vectors.
    /// </summary>
    public static Vector4 operator- (Vector4 a, Vector4 b) {
        return new Vector4(a.x-b.x, a.y-b.y, a.z-b.z, a.w-b.w);
    }

    /// <summary>
    /// Multiplies vector components by a scalar.
    /// </summary>
    public static Vector4 operator* (float s, Vector4 a) {
        return new Vector4(s*a.x, s*a.y, s*a.z, s*a.w);
    }

    /// <summary>
    /// Multiplies vector components by a scalar.
    /// </summary>
    public static Vector4 operator* (Vector4 a, float s) {
        return new Vector4(s*a.x, s*a.y, s*a.z, s*a.w);
    }

    /// <summary>
    /// Divides vector components by a scalar. Mathematically equivalent to multiplying by 1/s scalar.
    /// </summary>
    public static Vector4 operator/ (Vector4 a, float s) {
        return new Vector4(a.x/s, a.y/s, a.z/s, a.w/s);
    }

    /// <summary>
    /// Dot product of two vectors.
    /// </summary>
    public static float Dot(Vector4 a, Vector4 b) {
        return a.x*b.x + a.y*b.y + a.z*b.z + a.w*b.w;
    }

    /// <summary>
    /// Implicit conversion from System.Numerics.Vector4 to Vector4.
    /// </summary>
    public static implicit operator Vector4(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
    /// <summary>
    /// Implicit conversion from Vector4 to System.Numerics.Vector4.
    /// </summary>
    public static implicit operator System.Numerics.Vector4(Vector4 v) => new System.Numerics.Vector4(v.x, v.y, v.z, v.w);

}
