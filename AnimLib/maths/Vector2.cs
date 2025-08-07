using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json.Serialization;

namespace AnimLib;

/// <summary>
/// A 2D vector.
/// </summary>
[Serializable]
public struct Vector2 : IEquatable<Vector2>,
    IMultiplyOperators<Vector2, float, Vector2>,
    IMultiplyOperators<Vector2, double, Vector2>,
    IAdditionOperators<Vector2, Vector2, Vector2>,
    ISubtractionOperators<Vector2, Vector2, Vector2>
{
    /// <summary>
    /// A component of the vector.
    /// </summary>
    public float x, y;

    /// <summary>
    /// (0,0) vector.
    /// </summary>
    public static readonly Vector2 ZERO = new Vector2();

    /// <summary>
    /// (1,1) vector.
    /// </summary>
    public static readonly Vector2 ONE = new Vector2(1.0f, 1.0f);

    /// <summary>
    /// (0,1) vector.
    /// </summary>
    public static readonly Vector2 UP = new Vector2(0.0f, 1.0f);

    /// <summary>
    /// (1,0) vector.
    /// </summary>
    public static readonly Vector2 RIGHT = new Vector2(1.0f, 0.0f);

    /// <summary>
    /// Broadcasts a scalar to a vector.
    /// </summary>
    public Vector2(float v) {
        this.x = v;
        this.y = v;
    }

    /// <summary>
    /// Creates a new vector with the given components.
    /// </summary>
    public Vector2(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Creates a new vector from the x and y components of a 3D vector.
    /// </summary>
    public Vector2(Vector3 v) {
        this.x = v.x;
        this.y = v.y;
    }

    /// <summary>
    /// The x component of the vector. For serialization purposes.
    /// </summary>
    public float X {
        get {
            return x;
        } set {
            x = value;
        }
    }

    /// <summary>
    /// The y component of the vector. For serialization purposes.
    /// </summary>
    public float Y {
        get {
            return y;
        } set {
            y = value;
        }
    }

    /// <summary>
    /// Returns a vector rotated by the given angle in radians.
    /// </summary>
    public Vector2 Rotated(float angleRad) {
        return new Vector2(MathF.Cos(angleRad)*x - MathF.Sin(angleRad)*y, MathF.Sin(angleRad)*x + MathF.Cos(angleRad)*y);
    }

    /// <summary>
    /// Returns the normalized form of the vector.
    /// </summary>
    [JsonIgnore]
    public Vector2 Normalized {
        get {
            var len = (float)Math.Sqrt(x*x + y*y);
            return new Vector2(x/len, y/len);
        }
    }

    /// <summary>
    /// Returns the perpendicular vector to this one, rotated clockwise.
    /// </summary>
    [JsonIgnore]
    public Vector2 PerpCw {
        get {
            return new Vector2(this.y, -this.x);
        }
    }

    /// <summary>
    /// Returns the perpendicular vector to this one, rotated counter-clockwise.
    /// </summary>
    [JsonIgnore]
    public Vector2 PerpCcw {
        get {
            return new Vector2(-this.y, this.x);
        }
    }

    /// <summary>
    /// Normalizes this vector.
    /// </summary>
    public void Normalize() {
        float len = MathF.Sqrt(x*x + y*y);
        x /= len;
        y /= len;
    }

    /// <summary>
    /// Returns the length of the vector.
    /// </summary>
    [JsonIgnore]
    public float Length {
        get {
            return MathF.Sqrt(x*x+y*y);
        }
    }

    /// <summary>
    /// Operator overload for the + operator between two 2D vectors.
    /// </summary>
    public static Vector2 operator+ (Vector2 a, Vector2 b) {
        return new Vector2(a.x + b.x, a.y + b.y);
    }

    /// <summary>
    /// Operator overload for the - operator between two 2D vectors.
    /// </summary>
    public static Vector2 operator- (Vector2 a, Vector2 b) {
        return new Vector2(a.x - b.x, a.y - b.y);
    }

    /// <summary>
    /// Operator overload for the unary - operator.
    /// </summary>
    public static Vector2 operator- (Vector2 a) {
        return new Vector2(-a.x, -a.y);
    }

    /// <summary>
    /// Operator overload for the * operator between a 2D vector and a scalar.
    /// </summary>
    public static Vector2 operator* (float a, Vector2 b) {
        return new Vector2(a*b.x, a*b.y);
    }

    /// <summary>
    /// Operator overload for the * operator between a 2D vector and a double precision scalar.
    /// </summary>
    public static Vector2 operator* (double a, Vector2 b) {
        float fa = (float)a;
        return new Vector2(fa*b.x, fa*b.y);
    }

    /// <summary>
    /// Operator overload for the == operator between two 2D vectors. Compares component-wise.
    /// </summary>
    public static bool operator== (Vector2 a, Vector2 b) {
        return a.x == b.x && a.y == b.y;
    }

    /// <summary>
    /// Operator overload for the != operator between two 2D vectors. Compares component-wise.
    /// </summary>
    public static bool operator!= (Vector2 a, Vector2 b) {
        return a.x != b.x || a.y != b.y;
    }

    /// <summary>
    /// Operator overload for the * operator between a 2D vector and a scalar.
    /// </summary>
    public static Vector2 operator* (Vector2 a, float b) {
        return new Vector2(b*a.x, b*a.y);
    }

    /// <summary>
    /// Operator overload for the * operator between a 2D vector and a double precision scalar.
    /// </summary>
    public static Vector2 operator* (Vector2 a, double b) {
        float fb = (float)b;
        return new Vector2(fb*a.x, fb*a.y);
    }

    /// <summary>
    /// Operator overload for the * operator between two 2D vectors. Component-wise product.
    /// </summary>
    public static Vector2 operator* (Vector2 a, Vector2 b) {
        return new Vector2(a.x*b.x, a.y*b.y);
    }

    /// <summary>
    /// Operator overload for the / operator between a 2D vector and a scalar. Equivalent to multiplying by the inverse of the scalar.
    /// </summary>
    public static Vector2 operator/ (Vector2 a, float b) {
        return new Vector2(a.x/b, a.y/b);
    }

    /// <summary>
    /// Operator overload for the / operator between a 2D vector and a double precision scalar. Equivalent to multiplying by the inverse of the scalar.
    /// </summary>
    public static Vector2 operator/ (Vector2 a, double b) {
        float fb = (float)b;
        return new Vector2(a.x/fb, a.y/fb);
    }

    /// <summary>
    /// Operator overload for the / operator between two 2D vectors. Component-wise division.
    /// </summary>
    public static Vector2 operator/ (Vector2 a, Vector2 b) {
        return new Vector2(a.x/b.x, a.y/b.y);
    }

    /// <summary>
    /// Returns the dot product of two 2D vectors.
    /// </summary>
    public static float Dot(Vector2 a, Vector2 b) {
        return a.x*b.x + a.y*b.y;
    }

    /// <summary>
    /// Linearly interpolates between two 2D vectors.
    /// </summary>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t){ 
        return a + t*(b-a);
    }

    /// <summary>
    /// Smoothly interpolates between two 2D vectors. C1 continuous.
    /// </summary>
    public static Vector2 Berp(Vector2 a, Vector2 b, float t) {
        var nt = Vector3.smooth1.Evaluate(t);
        return Lerp(a, b, nt);
    }
    
    /// <summary>
    /// Whether this vector contains NaN.
    /// </summary>
    [JsonIgnore]
    public bool ContainsNan {
        get {
            return float.IsNaN(this.x) || float.IsNaN(this.y);
        }
    }

    /// <summary>
    /// Converts the vector to a string.
    /// </summary>
    public override string ToString()
    {
        return $"({x},{y})";
    }

    /// <summary>
    /// Compares two vectors for equality.
    /// </summary>
    public bool Equals([AllowNull] Vector2 other)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compares an object to this vector for equality. True only if the object is a vector and has equal components.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Vector2 ov2 && ov2.x == x && ov2.y == y;
    }

    /// <summary>
    /// Returns a hash code for the vector. For use in hash tables.
    /// </summary>
    public override int GetHashCode()
    {
        return this.x.GetHashCode() ^ this.y.GetHashCode() << 2;
    }

    /// <summary>
    /// Implicit conversion from a tuple of two double precision floats to a 2D vector.
    /// </summary>
    public static implicit operator Vector2((double, double) v) => new Vector2((float)v.Item1, (float)v.Item2);
    /// <summary>
    /// Implicit conversion from a tuple of two floats to a 2D vector.
    /// </summary>
    public static implicit operator Vector2((float, float) v) => new Vector2(v.Item1, v.Item2);
    /// <summary>
    /// Implicit conversion from 3D vector to 2D vector, discarding the z component.
    /// </summary>
    public static implicit operator Vector2(Vector3 v) => v.xy;
    /// <summary>
    /// Implicit conversion from System.Numerics.Vector2 to 2D vector.
    /// </summary>
    public static implicit operator Vector2(System.Numerics.Vector2 d) => new Vector2(d.X, d.Y);
    /// <summary>
    /// Implicit conversion from 2D vector to System.Numerics.Vector2.
    /// </summary>
    public static implicit operator System.Numerics.Vector2(Vector2 d) => new System.Numerics.Vector2(d.X, d.Y);
}
