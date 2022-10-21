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

        public static Color operator*(float l, Color r) {
            return new Color((float)r.r/255.0f * l, (float)r.g/255.0f * l, (float)r.b/255.0f * l, r.a);
        }

        public Color WithA(byte a) {
            return new Color(this.r, this.g, this.b, a);
        }

        public override bool Equals(object obj) {
            if(obj is Color) {
                var cobj = (Color)obj;
                return this == cobj;
            } else return base.Equals(obj);
        }

        public static bool operator== (Color a, Color b) {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public static bool operator!= (Color a, Color b) {
            return a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;
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

        public static Vector4 HSV2RGB(Vector4 c) {
          var K = new Vector4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
          
          var p = ((new Vector3(c.x, c.x, c.x) + new Vector3(K.x, K.y, K.z)).Fract * 6.0f - new Vector3(K.w, K.w, K.w)).Abs;
          return new Vector4(c.z * Vector3.Lerp(new Vector3(K.x, K.x, K.x), ((p - new Vector3(K.x, K.x, K.x)).Clamped(0.0f, 1.0f)), c.y), c.w);
        }

        public static Vector4 RGB2HSV(Vector4 c) {
          var K = new Vector4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
          var p = c.y < c.z ? new Vector4(c.z, c.y, K.w, K.z) : new Vector4(c.y, c.z, K.x, K.y);
          var q = c.x < p.x ? new Vector4(p.x, p.y, p.w, c.x) : new Vector4(c.x, p.y, p.z, p.x);
          float d = q.x - MathF.Min(q.w, q.y);
          float e = 1.0e-10f;
          return new Vector4(MathF.Abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x, c.w);
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
        public static readonly Color TRANSPARENT = new Color(255, 255, 255, 0);
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

        public static Color LerpHSV(Color c1, Color c2, float x)
        {
            var c1v = Color.RGB2HSV(c1.ToVector4());
            var c2v = Color.RGB2HSV(c2.ToVector4());
            var r = Color.HSV2RGB(c1v + x*(c2v-c1v));
            return new Color(r.x, r.y, r.z, r.w);
        }
    };
}
