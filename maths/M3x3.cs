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
    }
}
