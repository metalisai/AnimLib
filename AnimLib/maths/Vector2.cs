using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json.Serialization;

namespace AnimLib
{
    [Serializable]
    public struct Vector2 : IEquatable<Vector2>,
        IMultiplyOperators<Vector2, float, Vector2>,
        IMultiplyOperators<Vector2, double, Vector2>,
        IAdditionOperators<Vector2, Vector2, Vector2>,
        ISubtractionOperators<Vector2, Vector2, Vector2>
    {
        public float x, y;
        public static readonly Vector2 ZERO = new Vector2();
        public static readonly Vector2 ONE = new Vector2(1.0f, 1.0f);
        public static readonly Vector2 UP = new Vector2(0.0f, 1.0f);
        public static readonly Vector2 RIGHT = new Vector2(1.0f, 0.0f);

        public Vector2(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public Vector2(Vector3 v) {
            this.x = v.x;
            this.y = v.y;
        }

        public float X {
            get {
                return x;
            } set {
                x = value;
            }
        }

        public float Y {
            get {
                return y;
            } set {
                y = value;
            }
        }

        public Vector2 Rotated(float angleRad) {
            return new Vector2(MathF.Cos(angleRad)*x - MathF.Sin(angleRad)*y, MathF.Sin(angleRad)*x + MathF.Cos(angleRad)*y);
        }

        [JsonIgnore]
        public Vector2 Normalized {
            get {
                var len = (float)Math.Sqrt(x*x + y*y);
                return new Vector2(x/len, y/len);
            }
        }

        [JsonIgnore]
        public Vector2 PerpCw {
            get {
                return new Vector2(this.y, -this.x);
            }
        }

        [JsonIgnore]
        public Vector2 PerpCcw {
            get {
                return new Vector2(-this.y, this.x);
            }
        }

        [JsonIgnore]
        public Vector2 PerpendicularCW {
            get {
                return new Vector2(y, -x);
            }
        }

        public void Normalize() {
            float len = MathF.Sqrt(x*x + y*y);
            x /= len;
            y /= len;
        }

        [JsonIgnore]
        public float Length {
            get {
                return MathF.Sqrt(x*x+y*y);
            }
        }

        public static Vector2 operator+ (Vector2 a, Vector2 b) {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator- (Vector2 a, Vector2 b) {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        public static Vector2 operator- (Vector2 a) {
            return new Vector2(-a.x, -a.y);
        }

        public static Vector2 operator* (float a, Vector2 b) {
            return new Vector2(a*b.x, a*b.y);
        }

        public static Vector2 operator* (double a, Vector2 b) {
            float fa = (float)a;
            return new Vector2(fa*b.x, fa*b.y);
        }

        public static bool operator== (Vector2 a, Vector2 b) {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator!= (Vector2 a, Vector2 b) {
            return a.x != b.x || a.y != b.y;
        }

        public static Vector2 operator* (Vector2 a, float b) {
            return new Vector2(b*a.x, b*a.y);
        }

        public static Vector2 operator* (Vector2 a, double b) {
            float fb = (float)b;
            return new Vector2(fb*a.x, fb*a.y);
        }

        public static Vector2 operator* (Vector2 a, Vector2 b) {
            return new Vector2(a.x*b.x, a.y*b.y);
        }

        public static Vector2 operator/ (Vector2 a, float b) {
            return new Vector2(a.x/b, a.y/b);
        }

        public static Vector2 operator/ (Vector2 a, double b) {
            float fb = (float)b;
            return new Vector2(a.x/fb, a.y/fb);
        }

        public static Vector2 operator/ (Vector2 a, Vector2 b) {
            return new Vector2(a.x/b.x, a.y/b.y);
        }

        public static float Dot(Vector2 a, Vector2 b) {
            return a.x*b.x + a.y*b.y;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t){ 
            return a + t*(b-a);
        }

        public static Vector2 Berp(Vector2 a, Vector2 b, float t) {
            var nt = Vector3.smooth1.Evaluate(t);
            return Lerp(a, b, nt);
        }

        public override string ToString() {
            return $"({x},{y})";
        }

        public bool Equals([AllowNull] Vector2 other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2 && ((Vector2)obj).x == x && ((Vector2)obj).y == y;
        }

        public override int GetHashCode()
		{
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2;
		}

        public static implicit operator Vector2((double, double) v) => new Vector2((float)v.Item1, (float)v.Item2);
        public static implicit operator Vector2((float, float) v) => new Vector2(v.Item1, v.Item2);
        public static implicit operator Vector2(Vector3 v) => v.xy;
        public static implicit operator Vector2(System.Numerics.Vector2 d) => new Vector2(d.X, d.Y);
        public static implicit operator System.Numerics.Vector2(Vector2 d) => new System.Numerics.Vector2(d.X, d.Y);
    }
}
