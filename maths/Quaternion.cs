using System;

namespace AnimLib
{
    [Serializable]
    public struct Quaternion {
     
        public float w, x, y, z;
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
        public float W {
            get { return w; }
            set { w = value; }
        }
        public static readonly Quaternion IDENTITY = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        public Quaternion(float w, float x, float y, float z) {
            this.w = w;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Quaternion(ref M3x3 m) {
            this.w = (float)Math.Sqrt(1.0 + m.m11 + m.m22 + m.m33) / 2.0f;
            this.x = (m.m32 - m.m23)/(4.0f * this.w);
            this.y = (m.m13 - m.m31)/(4.0f * this.w);
            this.z = (m.m21 - m.m12)/(4.0f * this.w);
        }

        public static Quaternion AngleAxis(float angleRad, Vector3 axis) {
            var ret = new Quaternion();
            float halfAngle = angleRad * 0.5f;
            float sinH = (float)Math.Sin(halfAngle);
            ret.w = (float)Math.Cos(halfAngle); 
            ret.x = axis.x * sinH;
            ret.y = axis.y * sinH;
            ret.z = axis.z * sinH;
            return ret;
        }

        public static Vector3 operator*(Quaternion r, Vector3 l) {
            var rot = M4x4.Rotate(r);
            var vh = new Vector4(l.x, l.y, l.z, 1.0f);
            var rh = rot*vh;
            return new Vector3(rh.x, rh.y, rh.z);
        }

        public static Quaternion operator*(Quaternion r, Quaternion l) {
            Quaternion ret = new Quaternion();
            ret.w = l.w * r.w - l.x * r.x - l.y * r.y - l.z * r.z;
            ret.x = l.w * r.x + l.x * r.w + l.y * r.z - l.z * r.y;
            ret.y = l.w * r.y - l.x * r.z + l.y * r.w + l.z * r.x;
            ret.z = l.w * r.z + l.x * r.y - l.y * r.x + l.z * r.w;
            return ret;
        }

    }
}
