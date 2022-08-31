using System;

namespace AnimLib
{
    public struct M3x3 {
        public float m11, m21, m31;
        public float m12, m22, m32;
        public float m13, m23, m33;

        public M3x3 FromColumns(Vector3 c1, Vector3 c2, Vector3 c3) {
            M3x3 ret;
            ret.m11 = c1.x; ret.m21 = c1.y; ret.m31 = c1.z;
            ret.m12 = c2.x; ret.m22 = c2.y; ret.m32 = c2.z;
            ret.m13 = c3.x; ret.m23 = c3.y; ret.m33 = c3.z;
            return ret;
        }

        public override string ToString() {
            return $"[\n{m11:N3} {m12:N3} {m13:N3}\n{m21:N3} {m22:N3} {m23:N3}\n{m31:N3} {m32:N3} {m33:N3}\n]";
        }

        public static M3x3 Translate_2D(Vector2 t) {
            M3x3 tr;
            tr.m11 = 1.0f; tr.m12 = 0.0f; tr.m13 = t.x;
            tr.m21 = 0.0f; tr.m22 = 1.0f; tr.m23 = t.y;
            tr.m31 = 0.0f; tr.m32 = 0.0f; tr.m33 = 1.0f;
            return tr;
        }

        public static M3x3 Rotate_2D(float r) {
            M3x3 rot;
            rot.m11 = MathF.Cos(r); rot.m12 = -MathF.Sin(r); rot.m13 = 0.0f;
            rot.m21 = MathF.Sin(r); rot.m22 = MathF.Cos(r); rot.m23 = 0.0f;
            rot.m31 = 0.0f; rot.m32 = 0.0f; rot.m33 = 1.0f;
            return rot;
        }

        public static M3x3 TRS_2D(Vector2 t, float r, Vector2 s) {
            M3x3 tr, rot, sc;

            // TODO: simplify
            
            tr.m11 = 1.0f; tr.m12 = 0.0f; tr.m13 = t.x;
            tr.m21 = 0.0f; tr.m22 = 1.0f; tr.m23 = t.y;
            tr.m31 = 0.0f; tr.m32 = 0.0f; tr.m33 = 1.0f;

            rot.m11 = MathF.Cos(r); rot.m12 = -MathF.Sin(r); rot.m13 = 0.0f;
            rot.m21 = MathF.Sin(r); rot.m22 = MathF.Cos(r); rot.m23 = 0.0f;
            rot.m31 = 0.0f; rot.m32 = 0.0f; rot.m33 = 1.0f;

            sc.m11 = s.x;  sc.m12 = 0.0f; sc.m13 = 0.0f;
            sc.m21 = 0.0f; sc.m22 = s.y;  sc.m23 = 0.0f;
            sc.m31 = 0.0f; sc.m32 = 0.0f; sc.m33 = 1.0f;

            return tr*rot*sc;
        }

        public static M3x3 operator* (M3x3 l, M3x3 r) {
            return new M3x3() {
                m11 = l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31,
                m21 = l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31,
                m31 = l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31,
                m12 = l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32,
                m22 = l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32,
                m32 = l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32,
                m13 = l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33,
                m23 = l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33,
                m33 = l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33,
            };
        }
    }
}
