using System;

namespace AnimLib
{
    [Serializable]
    public struct Color {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color(byte R, byte G, byte B, byte A) {
            this.r = R;
            this.g = G;
            this.b = B;
            this.a = A;
        }

        public Color(float R, float G, float B, float A) {
            this.r = (byte)Math.Clamp(R * 255, 0.0, 255.0);
            this.g = (byte)Math.Clamp(G * 255, 0.0, 255.0);
            this.b = (byte)Math.Clamp(B * 255, 0.0, 255.0);
            this.a = (byte)Math.Clamp(A * 255, 0.0, 255.0);
        }

        public Color(Vector4 v) {
            this.r = (byte)Math.Clamp(v.x * 255, 0.0, 255.0);
            this.g = (byte)Math.Clamp(v.y * 255, 0.0, 255.0);
            this.b = (byte)Math.Clamp(v.z * 255, 0.0, 255.0);
            this.a = (byte)Math.Clamp(v.w * 255, 0.0, 255.0);
        }

        public uint ToU32() {
            return (uint)r<<24 | (uint)g<<16 | (uint)b<<8 | (uint)a;
        }

        public Vector3 ToVector3() {
            return new Vector3(((float)r)/255.0f, ((float)g)/255.0f, ((float)b)/255.0f);
        }

        public Vector4 ToVector4() {
            return new Vector4(((float)r)/255.0f, ((float)g)/255.0f, ((float)b)/255.0f, ((float)a)/255.0f);
        }

        public override string ToString() {
            return $"({r},{g},{b},{a})";
        }

        public byte R {
            get {
                return r;
            } set {
                r = value;
            }
        }

        public byte G {
            get {
                return g;
            } set {
                g = value;
            }
        }

        public byte B {
            get {
                return b;
            } set {
                b = value;
            }
        }

        public byte A {
            get {
                return a;
            } set {
                a = value;
            }
        }

        public static readonly Color BLACK = new Color(0, 0, 0, 255);
        public static readonly Color WHITE = new Color(255, 255, 255, 255);
        public static readonly Color RED = new Color(255, 0, 0, 255);
        public static readonly Color GREEN = new Color(0, 255, 0, 255);
        public static readonly Color BLUE = new Color(0, 0, 255, 255);
        public static readonly Color YELLOW = new Color(255, 255, 0, 255);
        public static readonly Color CYAN = new Color(0, 255, 255, 255);
        public static readonly Color MAGENTA = new Color(255, 0, 255, 255);
        public static readonly Color TRANSPARENT = new Color(0, 0, 0, 255);
        public static readonly Color ORANGE = new Color(240, 94, 35, 255);
        public static readonly Color BROWN = new Color(101, 67, 33, 255);
        public static readonly Color VIOLET = new Color(121, 61, 244, 255);

        public static Color Lerp(Color c1, Color c2, float x)
        {
            var c1v = c1.ToVector4();
            var c2v = c2.ToVector4();
            var r = (c1v + x*(c2v-c1v));
            return new Color(r.x, r.y, r.z, r.w);
        }
    };
}
