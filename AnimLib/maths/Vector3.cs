using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace AnimLib
{
    /// <summary>
    /// A 3D vector.
    /// </summary>
    [Serializable]
    public struct Vector3 :
        IAdditionOperators<Vector3, Vector3, Vector3>,
        ISubtractionOperators<Vector3, Vector3, Vector3>,
        IMultiplyOperators<Vector3, double, Vector3>,
        IMultiplyOperators<Vector3, float, Vector3>
    {
        /// <summary>
        /// A component of the vector.
        /// </summary>
        public float x, y, z;

        /// <summary>
        /// Swizzle for x and y.
        /// </summary>
        [JsonIgnore]
        public Vector2 xy {
            get {
                return new Vector2(x, y);
            }
        }

        /// <summary>
        /// A component property for serialization.
        /// </summary>
        public float X {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// A component property for serialization.
        /// </summary>
        public float Y {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// A component property for serialization.
        /// </summary>
        public float Z {
            get { return z; }
            set { z = value; }
        }

        /// <summary>
        /// Vector3(0,0,0)
        /// </summary>
        public static readonly Vector3 ZERO = new Vector3();
        /// <summary>
        /// Vector3(1,1,1)
        /// </summary>
        public static readonly Vector3 ONE = new Vector3(1.0f, 1.0f, 1.0f);
        /// <summary>
        /// Vector3(0,1,0)
        /// </summary>
        public static readonly Vector3 UP = new Vector3(0.0f, 1.0f, 0.0f);
        /// <summary>
        /// Vector3(0,0,1)
        /// </summary>
        public static readonly Vector3 FORWARD = new Vector3(0.0f, 0.0f, 1.0f);
        /// <summary>
        /// Vector3(1,0,0)
        /// </summary>
        public static readonly Vector3 RIGHT = new Vector3(1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Construct vector from components.
        /// </summary>
        public Vector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// Construct vector from xy and z components.
        /// </summary>
        public Vector3(Vector2 vector, float z) {
            this.x = vector.x;
            this.y = vector.y;
            this.z = z;
        }

        /// <summary>
        /// Construct vector from System.Numerics.Vector3.
        /// </summary>
        public Vector3(System.Numerics.Vector3 v3) {
            this.x = v3.X;
            this.y = v3.Y;
            this.z = v3.Z;
        }

        /// <summary>
        /// The length of the vector.
        /// </summary>
        [JsonIgnore]
        public float Length {
            get {
                return MathF.Sqrt(x*x + y*y + z*z);
            }
        }

        /// <summary>
        /// Normalized vector.
        /// </summary>
        [JsonIgnore]
        public Vector3 Normalized {
            get {
                var len = (float)Math.Sqrt(x*x + y*y + z*z);
                return new Vector3(x/len, y/len, z/len);
            }
        }

        /// <summary>
        /// Vector with a Z offset.
        /// </summary>
        public Vector3 ZOfst(float ofset) {
            return new Vector3(x, y, z+ofset);
        }

        /// <summary>
        /// Normalize this vector.
        /// </summary>
        public void Normalize() {
            var len = MathF.Sqrt(x*x + y*y + z*z);
            x /= len;
            y /= len;
            z /= len;
        }

        /// <summary>
        /// Rotate this vector by a quaternion.
        /// </summary>
        public Vector3 Rotated(Quaternion rot) {
            return rot * this;
        }

        /// <summary>
        /// Whether this vector contains NaN.
        /// </summary>
        [JsonIgnore]
        public bool ContainsNan {
            get {
                return float.IsNaN(this.x) || float.IsNaN(this.y) || float.IsNaN(this.z);
            }
        }

        /// <summary>
        /// Vector sum.
        /// </summary>
        public static Vector3 operator+ (Vector3 a, Vector3 b) {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        /// <summary>
        /// Vector difference.
        /// </summary>
        public static Vector3 operator-(Vector3 a, Vector3 b) {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        /// <summary>
        /// Component-wise vector product.
        /// </summary>
        public static Vector3 operator*(Vector3 a, Vector3 b) {
            return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z);
        }
        
        /// <summary>
        /// Scale vector by a scalar.
        /// </summary>
        public static Vector3 operator*(Vector3 a, float s) 
        {
            return new Vector3(a.x*s, a.y*s, a.z*s);
        }

        /// <summary>
        /// Scale vector by a scalar.
        /// </summary>
        public static Vector3 operator*(float l, Vector3 r) {
            return new Vector3(l*r.x, l*r.y, l*r.z);
        }

        /// <summary>
        /// Scale vector by a scalar.
        /// </summary>
        public static Vector3 operator*(Vector3 a, double s) 
        {
            float fs = (float)s;
            return new Vector3(a.x*fs, a.y*fs, a.z*fs);
        }

        /// <summary>
        /// Scale vector by a scalar.
        /// </summary>
        public static Vector3 operator*(double l, Vector3 r) {
            float fl = (float)l;
            return new Vector3(fl*r.x, fl*r.y, fl*r.z);
        }

        /// <summary>
        /// Negate operator.
        /// </summary>
        public static Vector3 operator-(Vector3 v) {
            return new Vector3(-v.x, -v.y, -v.z);
        }

        /// <summary>
        /// Cross product.
        /// </summary>
        public static Vector3 Cross(Vector3 a, Vector3 b) {
            return new Vector3(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
        }

        /// <summary>
        /// Dot product.
        /// </summary>
        public static float Dot(Vector3 a, Vector3 b) {
            return a.x*b.x + a.y*b.y + a.z*b.z;
        }

        internal static CubicBezier<float, float> smooth1 = new (0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// Linear interpolation.
        /// </summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t){ 
            return a + t*(b-a);
        }

        /// <summary>
        /// Smooth (c1) interpolation.
        /// </summary>
        public static Vector3 BLerp(Vector3 a, Vector3 b, float t) {
            var nt = smooth1.Evaluate(t);
            return Lerp(a, b, nt);
        }

        /// <summary>
        /// Floor all components of this vector.
        /// </summary>
        public void Floor() {
            x = MathF.Floor(this.x);
            y = MathF.Floor(this.y);
            z = MathF.Floor(this.z);
        }

        /// <summary>
        /// A vector with floored components.
        /// </summary>
        [JsonIgnore]
        public Vector3 Floored {
            get {
                return new Vector3(MathF.Floor(this.x), MathF.Floor(this.y), MathF.Floor(this.z));
            }
        }

        /// <summary>
        /// A vector with fractional components.
        /// </summary>
        [JsonIgnore]
        public Vector3 Fract {
            get {
                return this - this.Floored;
            }
        }

        /// <summary>
        /// A vector with absolute components.
        /// </summary>
        [JsonIgnore]
        public Vector3 Abs {
            get {
                return new Vector3(MathF.Abs(x), MathF.Abs(y), MathF.Abs(z));
            }
        }

        /// <summary>
        /// A vector with clamped components.
        /// </summary>
        public Vector3 Clamped(float min, float max) {
            return new Vector3(x < min ? min : x > max ? max : x, y < min ? min : y > max ? max : y, z < min ? min : z > max ? max : z);
        }

        /// <summary>
        /// Convert to string.
        /// </summary>
        public override string ToString() {
            return $"({x},{y},{z})";
        }

        /// <summary>
        /// Implicit conversion to a Vector2 takes the x and y components.
        /// </summary>
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0.0f);

        /// <summary>
        /// Implicitly convert from System.Numerics.Vector3.
        /// </summary>
        public static implicit operator Vector3(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

        /// <summary>
        /// Implicitly convert to System.Numerics.Vector3.
        /// </summary>
        public static implicit operator System.Numerics.Vector3(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);


        /// <summary>
        /// Component-wise equality.
        /// </summary>
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }

        /// <summary>
        /// Component-wise inequality.
        /// </summary>
        public static bool operator !=(Vector3 lhs, Vector3 rhs)
        {
            return !(lhs==rhs);
        }

        /// <summary>
        /// Vector3 is equal to another object if it is a Vector3 and all components are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Vector3 && ((Vector3)obj).x == x && ((Vector3)obj).y == y && ((Vector3)obj).z == z;
        }

        /// <summary>
        /// Hash code for this vector.
        /// </summary>
        public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
		}
    }
}
