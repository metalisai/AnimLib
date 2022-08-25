using System;

namespace AnimLib
{
    public struct Vector4 {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4(Vector3 v, float w) {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = w;
        }

        public Vector4 Normalized {
            get {
                float len = MathF.Sqrt(x*x + y*y + z*z + w*w);
                return new Vector4(x/len, y/len, z/len, w/len);
            }
        }

        public Vector3 xyz {
            get {
                return new Vector3(x, y, z);
            }
        }

        public float Length {
            get {
                return MathF.Sqrt(x*x + y*y + z*z + w*w);
            }
        }

        public void Normalize() {
            float len = Length;
            x /= len;
            y /= len;
            z /= len;
            w /= len;
        }

        public override string ToString() {
            return $"({x},{y},{z},{w})";
        }

        public static Vector4 FromInt32(uint val) {
            return new Vector4((float)((val>>24)&0xFF)/255.0f, (float)((val>>16)&0xFF)/255.0f, (float)((val>>8)&0xFF)/255.0f, (float)((val)&0xFF)/255.0f);
        }

        public static Vector4 operator+ (Vector4 a, Vector4 b) {
            return new Vector4(a.x+b.x, a.y+b.y, a.z+b.z, a.w+b.w);
        }

        public static Vector4 operator- (Vector4 a, Vector4 b) {
            return new Vector4(a.x-b.x, a.y-b.y, a.z-b.z, a.w-b.w);
        }

        public static Vector4 operator* (float s, Vector4 a) {
            return new Vector4(s*a.x, s*a.y, s*a.z, s*a.w);
        }

        public static Vector4 operator* (Vector4 a, float s) {
            return new Vector4(s*a.x, s*a.y, s*a.z, s*a.w);
        }

        public static float Dot(Vector4 a, Vector4 b) {
            return a.x*b.x + a.y*b.y + a.z*b.z + a.w*b.w;
        }

        public static implicit operator Vector4(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
        public static implicit operator System.Numerics.Vector4(Vector4 v) => new System.Numerics.Vector4(v.x, v.y, v.z, v.w);

    }

}
