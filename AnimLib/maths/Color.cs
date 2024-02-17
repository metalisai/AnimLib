using System;

namespace AnimLib;

/// <summary>
/// A color with 4 channels.
/// </summary>
[Serializable]
public struct Color {
    /// <summary>
    /// A channel of the color.
    /// </summary>
    public float r, g, b, a;

    /// <summary>
    /// Create color from a 32-but color. In range 0-255.
    /// </summary>
    public Color(byte R, byte G, byte B, byte A) {
        this.r = (float)R/255.0f;
        this.g = (float)G/255.0f;
        this.b = (float)B/255.0f;
        this.a = (float)A/255.0f;
    }

    /// <summary>
    /// Create color from a single precision values. In range 0.0-1.0, or HDR.
    /// </summary>
    public Color(float R, float G, float B, float A) {
        this.r = R;
        this.g = G;
        this.b = B;
        this.a = A;
    }

    /// <summary>
    /// Create color from a single precision values. In range 0.0-1.0, or HDR.
    /// </summary>
    public Color(Vector4 v) {
        this.r = v.x;
        this.g = v.y;
        this.b = v.z;
        this.a = v.w;
    }

    /// <summary>
    /// Overload to multiply a color by a scalar. Component-wise.
    /// </summary>
    public static Color operator*(float l, Color r) {
        return new Color(l*r.r, l*r.g, l*r.b, l*r.a);
    }

    /// <summary>
    /// Overload to multiply a color by a scalar. Component-wise.
    /// </summary>
    public static Color operator*(Color l, float r) {
        return new Color(l.r*r, l.g*r, l.b*r, l.a*r);
    }

    /// <summary>
    /// Get Color with the alpha channel overridden.
    /// </summary>
    public Color WithA(byte a) {
        float af = (float)a/255.0f;
        var ret = new Color(r, g, b, af);
        return ret;
    }

    /// <summary>
    /// Get Color with the alpha channel overridden.
    /// </summary>
    public Color WithA(float a) {
        var ret = new Color(r, g, b, a);
        return ret;
    }

    /// <summary>
    /// Compare an object to this color.
    /// </summary>
    public override bool Equals(object? obj) {
        if(obj is Color cobj) {
            return this == cobj;
        } else return base.Equals(obj);
    }

    /// <summary>
    /// Get a hash code for this color.
    /// </summary>
    public override int GetHashCode() {
        return ToU32().GetHashCode();
    }

    /// <summary>
    /// Compare two colors.
    /// </summary>
    public static bool operator== (Color a, Color b) {
        return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
    }

    /// <summary>
    /// Compare two colors for inequality.
    /// </summary>
    public static bool operator!= (Color a, Color b) {
        return a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;
    }

    /// <summary>
    /// Get the hash code of this color.
    /// </summary>
    public uint ToU32() {
        byte R = byte.CreateSaturating(r*255.0f);
        byte G = byte.CreateSaturating(g*255.0f);
        byte B = byte.CreateSaturating(b*255.0f);
        byte A = byte.CreateSaturating(a*255.0f);
        return (uint)((R << 24) | (G << 16) | (B << 8) | A);
    }

    /// <summary>
    /// Convert to a vector3. Includes RGB channels.
    /// </summary>
    public Vector3 ToVector3() {
        return new Vector3(r, g, b);
    }

    /// <summary>
    /// Convert to a vector4. Includes RGBA channels.
    /// </summary>
    public Vector4 ToVector4() {
        return new Vector4(r, g, b, a);
    }

    /// <summary>
    /// Convert HSV color to RGB color.
    /// </summary>
    public static Vector4 HSV2RGB(Vector4 c) {
      var K = new Vector4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
      
      var p = ((new Vector3(c.x, c.x, c.x) + new Vector3(K.x, K.y, K.z)).Fract * 6.0f - new Vector3(K.w, K.w, K.w)).Abs;
      return new Vector4(c.z * Vector3.Lerp(new Vector3(K.x, K.x, K.x), ((p - new Vector3(K.x, K.x, K.x)).Clamped(0.0f, 1.0f)), c.y), c.w);
    }

    /// <summary>
    /// Convert RGB color to HSV color.
    /// </summary>
    public static Vector4 RGB2HSV(Vector4 c) {
      var K = new Vector4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
      var p = c.y < c.z ? new Vector4(c.z, c.y, K.w, K.z) : new Vector4(c.y, c.z, K.x, K.y);
      var q = c.x < p.x ? new Vector4(p.x, p.y, p.w, c.x) : new Vector4(c.x, p.y, p.z, p.x);
      float d = q.x - MathF.Min(q.w, q.y);
      float e = 1.0e-10f;
      return new Vector4(MathF.Abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x, c.w);
    }

    /// <summary>
    /// Convert to a string.
    /// </summary>
    public override string ToString() {
        return $"({r:F2},{g:F2},{b:F2},{a:F2})";
    }

    /// <summary>
    /// The red channel property. For serialization.
    /// </summary>
    public float R {
        get {
            return r;
        } set {
            r = value;
        }
    }

    /// <summary>
    /// The green channel property. For serialization.
    /// </summary>
    public float G {
        get {
            return g;
        } set {
            g = value;
        }
    }

    /// <summary>
    /// The blue channel property. For serialization.
    /// </summary>
    public float B {
        get {
            return b;
        } set {
            b = value;
        }
    }

    /// <summary>
    /// The alpha channel property. For serialization.
    /// </summary>
    public float A {
        get {
            return a;
        } set {
            a = value;
        }
    }

    /// <summary> The color black. </summary>
    public static readonly Color BLACK = new Color(0, 0, 0, 255);
    /// <summary> The color white. </summary>
    public static readonly Color WHITE = new Color(255, 255, 255, 255);
    /// <summary> The color red. </summary>
    public static readonly Color RED = new Color(255, 0, 0, 255);
    /// <summary> The color green. </summary>
    public static readonly Color GREEN = new Color(0, 255, 0, 255);
    /// <summary> The color blue. </summary>
    public static readonly Color BLUE = new Color(0, 0, 255, 255);
    /// <summary> The color yellow. </summary>
    public static readonly Color YELLOW = new Color(255, 255, 0, 255);
    /// <summary> The color cyan. </summary>
    public static readonly Color CYAN = new Color(0, 255, 255, 255);
    /// <summary> The color magenta. </summary>
    public static readonly Color MAGENTA = new Color(255, 0, 255, 255);
    /// <summary> Clear.</summary>
    public static readonly Color TRANSPARENT = new Color(255, 255, 255, 0);
    /// <summary> The color orange. </summary>
    public static readonly Color ORANGE = new Color(240, 94, 35, 255);
    /// <summary> The color pink. </summary>
    public static readonly Color BROWN = new Color(101, 67, 33, 255);
    /// <summary> The color pink. </summary>
    public static readonly Color VIOLET = new Color(121, 61, 244, 255);

    /// <summary>
    /// Interpolate between two colors in RGB space.
    /// </summary>
    public static Color Lerp(Color c1, Color c2, float x)
    {
        var c1v = c1.ToVector4();
        var c2v = c2.ToVector4();
        var r = (c1v + x*(c2v-c1v));
        return new Color(r.x, r.y, r.z, r.w);
    }

    /// <summary>
    /// Interpolate between two colors in HSV space.
    /// </summary>
    public static Color LerpHSV(Color c1, Color c2, float x)
    {
        var c1v = Color.RGB2HSV(c1.ToVector4());
        var c2v = Color.RGB2HSV(c2.ToVector4());
        var r = Color.HSV2RGB(c1v + x*(c2v-c1v));
        return new Color(r.x, r.y, r.z, r.w);
    }

    [ThreadStatic]
    static Random? random;

    /// <summary>
    /// Get a random color.
    /// </summary>
    public static Color Random() {
        if(random == null) {
            random = new Random();
        }
        float h = random.NextSingle();
        float s = 0.4f + 0.6f*random.NextSingle();
        float v = 0.1f + 0.9f*random.NextSingle();
        var c4 = Color.HSV2RGB(new Vector4(h, s, v, 1.0f));
        return new Color(c4.x, c4.y, c4.z, c4.w);
    }
}
