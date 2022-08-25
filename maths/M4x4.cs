using System;

namespace AnimLib
{
    public struct M4x4 {
        public float m11, m21, m31, m41;
        public float m12, m22, m32, m42;
        public float m13, m23, m33, m43;
        public float m14, m24, m34, m44;

        public M4x4(ref M4x4 m) {
            m11 = m.m11; m12 = m.m12; m13 = m.m13; m14 = m.m14;
            m21 = m.m21; m22 = m.m22; m23 = m.m23; m24 = m.m24;
            m31 = m.m31; m32 = m.m32; m33 = m.m33; m34 = m.m34;
            m41 = m.m41; m42 = m.m42; m43 = m.m43; m44 = m.m44;
        }

        public static readonly M4x4 IDENTITY = new M4x4() {
            m11 = 1.0f, m12 = 0.0f, m13 = 0.0f, m14 = 0.0f,
            m21 = 0.0f, m22 = 1.0f, m23 = 0.0f, m24 = 0.0f,
            m31 = 0.0f, m32 = 0.0f, m33 = 1.0f, m34 = 0.0f,
            m41 = 0.0f, m42 = 0.0f, m43 = 0.0f, m44 = 1.0f,
        };
        
        public override string ToString() {
            return $"[\n{m11:N3} {m12:N3} {m13:N3} {m14:N3}\n{m21:N3} {m22:N3} {m23:N3} {m24:N3}\n{m31:N3} {m32:N3} {m33:N3} {m34:N3}\n{m41:N3} {m42:N3} {m43:N3} {m44:N3}\n]";
        }

        public float[] ToArray() {
            return new float[16] {m11, m21, m31, m41, m12, m22, m32, m42, m13, m23, m33, m43, m14, m24, m34, m44};
        }

        public static M4x4 Perspective(float fov, float aspectRatio, float zNear, float zFar) {
            float tanHalfFOV = (float)Math.Tan((fov/360.0f)*Math.PI);
            float d = zNear-zFar;
            var ret = new M4x4() {
                m11 = 1.0f / (tanHalfFOV * aspectRatio),
                m22 = 1.0f / tanHalfFOV,
                m33 = (-zNear - zFar) / d,
                m43 = 1.0f,
                m34 = 2.0f * zFar * zNear / d,
            }; 
            return ret;
        }

        public static M4x4 InvPerspective(float fov, float aspectRatio, float zNear, float zFar) {
            float tanHalfFOV = (float)Math.Tan((fov/360.0f)*Math.PI);
            float d = zNear-zFar;
            var ret = new M4x4() {
                m11 = tanHalfFOV * aspectRatio,
                m22 = tanHalfFOV,
                m43 = d / (2.0f * zFar * zNear),
                m34 = 1.0f,
                m44 = ((zNear + zFar) / d) / (2.0f * zFar * zNear / d),
            }; 
            return ret;
        }

        public static M4x4 Ortho(float l, float r, float t, float b, float f, float n) {
            var ret = new M4x4() {
                m11 = 2.0f / (r-l),
                m21 = 0.0f,
                m31 = 0.0f,
                m41 = 0.0f,
                m12 = 0.0f,
                m22 = 2.0f / (t-b),
                m32 = 0.0f,
                m42 = 0.0f,
                m13 = 0.0f,
                m23 = 0.0f,
                m33 = -2.0f / (f-n),
                m43 = 0.0f,
                m14 = -(r+l)/(r-l),
                m24 = -(t+b)/(t-b),
                m34 = -(f+n)/(f-n),
                m44 = 1.0f,
            };
            return ret;
        }

        public static M4x4 Rotate(Quaternion r) {
            var ret = new M4x4() {
                m11 = 1.0f - 2.0f*r.y*r.y - 2.0f*r.z*r.z,
                m21 = 2.0f*r.x*r.y + 2.0f*r.z*r.w,
                m31 = 2.0f*r.x*r.z - 2.0f*r.y*r.w,
                m41 = 0.0f,
                m12 = 2.0f*r.x*r.y - 2.0f*r.z*r.w,
                m22 = 1.0f - 2.0f*r.x*r.x - 2.0f*r.z*r.z,
                m32 = 2.0f*r.y*r.z + 2.0f*r.x*r.w,
                m42 = 0.0f,
                m13 = 2.0f*r.x*r.z + 2.0f*r.y*r.w,
                m23 = 2.0f*r.y*r.z - 2.0f*r.x*r.w,
                m33 = 1.0f - 2.0f*r.x*r.x - 2.0f*r.y*r.y,
                m43 = 0.0f,
                m14 = 0.0f,
                m24 = 0.0f,
                m34 = 0.0f,
                m44 = 1.0f,
            };
            return ret;
        }

        public void Transpose() {
            var mat = new M4x4(ref this);
            m11 = mat.m11; m12 = mat.m21; m13 = mat.m31; m14 = mat.m41;
            m21 = mat.m12; m22 = mat.m22; m23 = mat.m32; m24 = mat.m42;
            m31 = mat.m13; m32 = mat.m23; m33 = mat.m33; m34 = mat.m43;
            m41 = mat.m14; m42 = mat.m24; m43 = mat.m34; m44 = mat.m44;
        }

        public static M4x4 Translate(Vector3 t) {
            var ret = new M4x4() {
                m11 = 1.0f,
                m21 = 0.0f,
                m31 = 0.0f,
                m41 = 0.0f,
                m12 = 0.0f,
                m22 = 1.0f,
                m32 = 0.0f,
                m42 = 0.0f,
                m13 = 0.0f,
                m23 = 0.0f,
                m33 = 1.0f,
                m43 = 0.0f,
                m14 = t.x,
                m24 = t.y,
                m34 = t.z,
                m44 = 1.0f,
            };
            return ret;
        }

        public static M4x4 Scale(Vector3 s) {
            var ret = new M4x4() {
                m11 = s.x,
                m22 = s.y,
                m33 = s.z,
                m44 = 1.0f
            };
            return ret;
        }

        public static M4x4 operator* (M4x4 l, M4x4 r) {
            return new M4x4() {
                m11 = l.m11 * r.m11 + l.m12 * r.m21 + l.m13 * r.m31 + l.m14 * r.m41,
                m21 = l.m21 * r.m11 + l.m22 * r.m21 + l.m23 * r.m31 + l.m24 * r.m41,
                m31 = l.m31 * r.m11 + l.m32 * r.m21 + l.m33 * r.m31 + l.m34 * r.m41,
                m41 = l.m41 * r.m11 + l.m42 * r.m21 + l.m43 * r.m31 + l.m44 * r.m41,
                m12 = l.m11 * r.m12 + l.m12 * r.m22 + l.m13 * r.m32 + l.m14 * r.m42,
                m22 = l.m21 * r.m12 + l.m22 * r.m22 + l.m23 * r.m32 + l.m24 * r.m42,
                m32 = l.m31 * r.m12 + l.m32 * r.m22 + l.m33 * r.m32 + l.m34 * r.m42,
                m42 = l.m41 * r.m12 + l.m42 * r.m22 + l.m43 * r.m32 + l.m44 * r.m42,
                m13 = l.m11 * r.m13 + l.m12 * r.m23 + l.m13 * r.m33 + l.m14 * r.m43,
                m23 = l.m21 * r.m13 + l.m22 * r.m23 + l.m23 * r.m33 + l.m24 * r.m43,
                m33 = l.m31 * r.m13 + l.m32 * r.m23 + l.m33 * r.m33 + l.m34 * r.m43,
                m43 = l.m41 * r.m13 + l.m42 * r.m23 + l.m43 * r.m33 + l.m44 * r.m43,
                m14 = l.m11 * r.m14 + l.m12 * r.m24 + l.m13 * r.m34 + l.m14 * r.m44,
                m24 = l.m21 * r.m14 + l.m22 * r.m24 + l.m23 * r.m34 + l.m24 * r.m44,
                m34 = l.m31 * r.m14 + l.m32 * r.m24 + l.m33 * r.m34 + l.m34 * r.m44,
                m44 = l.m41 * r.m14 + l.m42 * r.m24 + l.m43 * r.m34 + l.m44 * r.m44,
            };
        }

        public static M4x4 RT(Quaternion r, Vector3 t) {
            // TODO: optimize
            var tm = M4x4.Translate(t);
            var rm = M4x4.Rotate(r);
            return rm*tm;
        }

        public static M4x4 TS(Vector3 t, Vector3 s) {
            // TODO: optimize
            var tm = M4x4.Translate(t);
            var sm = M4x4.Scale(s);
            return tm*sm;
        }

        public static M4x4 TRS(Vector3 t, Quaternion r, Vector3 s) {
            return new M4x4() {
                // TODO: rearrange to make compiler's life easier (write order)
                m11 = (1.0f-2.0f*(r.y*r.y+r.z*r.z))*s.x,
                m12 = (r.x*r.y-r.z*r.w)*s.y*2.0f,
                m13 = (r.x*r.z+r.y*r.w)*s.z*2.0f,
                m14 = t.x,
                m21 = (r.x*r.y+r.z*r.w)*s.x*2.0f,
                m22 = (1.0f-2.0f*(r.x*r.x+r.z*r.z))*s.y,
                m23 = (r.y*r.z-r.x*r.w)*s.z*2.0f,
                m24 = t.y,
                m31 = (r.x*r.z-r.y*r.w)*s.z*2.0f,
                m32 = (r.y*r.z+r.x*r.w)*s.y*2.0f,
                m33 = (1.0f-2.0f*(r.x*r.x+r.y*r.y))*s.z,
                m34 = t.z,
                m41 = 0.0f,
                m42 = 0.0f,
                m43 = 0.0f,
                m44 = 1.0f,
            };
        }

        public static M4x4 FromColumns(Vector4 c1, Vector4 c2, Vector4 c3, Vector4 c4) {
            M4x4 ret;
            ret.m11 = c1.x; ret.m21 = c1.y; ret.m31 = c1.z; ret.m41 = c1.w;
            ret.m12 = c2.x; ret.m22 = c2.y; ret.m32 = c2.z; ret.m42 = c2.w;
            ret.m13 = c3.x; ret.m23 = c3.y; ret.m33 = c3.z; ret.m43 = c3.w;
            ret.m14 = c4.x; ret.m24 = c4.y; ret.m34 = c4.z; ret.m44 = c4.w;
            return ret;
        }

        public static Vector4 operator* (M4x4 m, Vector4 v) {
            var ret = new Vector4() {
                x = v.x*m.m11 + v.y*m.m12 + v.z*m.m13 + v.w*m.m14,
                y = v.x*m.m21 + v.y*m.m22 + v.z*m.m23 + v.w*m.m24,
                z = v.x*m.m31 + v.y*m.m32 + v.z*m.m33 + v.w*m.m34,
                w = v.x*m.m41 + v.y*m.m42 + v.z*m.m43 + v.w*m.m44
            };
            return ret;
        }
    }
}
