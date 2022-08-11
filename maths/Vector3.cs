using System;
using System.Text.Json.Serialization;

namespace AnimLib
{
    [Serializable]
    public struct Vector3 {
        [JsonIgnore]
        public Vector2 xy {
            get {
                return new Vector2(x, y);
            }
        }
        public float x, y, z;

        public float X {
            get { return x; }
            set { x = value; }
        }

        public float Y {
            get { return y; }
            set { y = value; }
        }

        public float Z {
            get { return z; }
            set { z = value; }
        }


        public static readonly Vector3 ZERO = new Vector3();
        public static readonly Vector3 ONE = new Vector3(1.0f, 1.0f, 1.0f);
        public static readonly Vector3 UP = new Vector3(0.0f, 1.0f, 0.0f);
        public static readonly Vector3 FORWARD = new Vector3(0.0f, 0.0f, 1.0f);
        public static readonly Vector3 RIGHT = new Vector3(1.0f, 0.0f, 0.0f);
        public Vector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(Vector2 vector, float z) {
            this.x = vector.x;
            this.y = vector.y;
            this.z = z;
        }
        public Vector3(System.Numerics.Vector3 v3) {
            this.x = v3.X;
            this.y = v3.Y;
            this.z = v3.Z;
        }

        [JsonIgnore]
        public float Length {
            get {
                return MathF.Sqrt(x*x + y*y + z*z);
            }
        }

        [JsonIgnore]
        public Vector3 Normalized {
            get {
                var len = (float)Math.Sqrt(x*x + y*y + z*z);
                return new Vector3(x/len, y/len, z/len);
            }
        }

        public Vector3 ZOfst(float ofset) {
            return new Vector3(x, y, z+ofset);
        }

        public void Normalize() {
            var len = MathF.Sqrt(x*x + y*y + z*z);
            x /= len;
            y /= len;
            z /= len;
        }

        public Vector3 Rotated(Quaternion rot) {
            return rot * this;
        }

        [JsonIgnore]
        public bool ContainsNan {
            get {
                return float.IsNaN(this.x) || float.IsNaN(this.y) || float.IsNaN(this.z);
            }
        }

        public static Vector3 operator+ (Vector3 a, Vector3 b) {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator-(Vector3 a, Vector3 b) {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator*(Vector3 a, Vector3 b) {
            return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z);
        }
        
        public static Vector3 operator*(Vector3 a, float s) 
        {
            return new Vector3(a.x*s, a.y*s, a.z*s);
        }

        public static Vector3 operator*(float l, Vector3 r) {
            return new Vector3(l*r.x, l*r.y, l*r.z);
        }

        public static Vector3 operator-(Vector3 v) {
            return new Vector3(-v.x, -v.y, -v.z);
        }

        public static Vector3 Cross(Vector3 a, Vector3 b) {
            return new Vector3(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
        }

        public static float Dot(Vector3 a, Vector3 b) {
            return a.x*b.x + a.y*b.y + a.z*b.z;
        }

        public static CubicBezier1 smooth1 = new CubicBezier1(0.0f, 0.0f, 1.0f, 1.0f);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t){ 
            return a + t*(b-a);
        }

        public static Vector3 BLerp(Vector3 a, Vector3 b, float t) {
            var nt = smooth1.Evaluate(t);
            return Lerp(a, b, nt);
        }

        public void Floor() {
            x = MathF.Floor(this.x);
            y = MathF.Floor(this.y);
            z = MathF.Floor(this.z);
        }

        [JsonIgnore]
        public Vector3 Floored {
            get {
                return new Vector3(MathF.Floor(this.x), MathF.Floor(this.y), MathF.Floor(this.z));
            }
        }

        [JsonIgnore]
        public Vector3 Fract {
            get {
                return this - this.Floored;
            }
        }

        [JsonIgnore]
        public Vector3 Abs {
            get {
                return new Vector3(MathF.Abs(x), MathF.Abs(y), MathF.Abs(z));
            }
        }

        public Vector3 Clamped(float min, float max) {
            return new Vector3(x < min ? min : x > max ? max : x, y < min ? min : y > max ? max : y, z < min ? min : z > max ? max : z);
        }

        public override string ToString() {
            return $"({x},{y},{z})";
        }

        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0.0f);

        public static implicit operator Vector3(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static implicit operator System.Numerics.Vector3(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }
        public static bool operator !=(Vector3 lhs, Vector3 rhs)
        {
            return !(lhs==rhs);
        }
        public override bool Equals(object obj)
        {
            return obj is Vector2 && ((Vector3)obj).x == x && ((Vector3)obj).y == y && ((Vector3)obj).z == z;
        }
        public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
		}
    }
}
