using System;

namespace AnimLib {
    public struct Ray {
        public Vector3 o; // origin
        public Vector3 d; // direction

        public Vector3? Intersect(Plane plane) {
            float denom = Vector3.Dot(this.d, plane.n);
            if(MathF.Abs(denom) <= float.Epsilon) {
                return null;
            }
            float t = -(Vector3.Dot(this.o, plane.n) + plane.o) / denom;
            if(t < 0) {
                return null;
            }
            Vector3 ret = t*this.d;
            return this.o + ret;
        }
    }

    public struct Plane {
        public Vector3 n; // normal
        public float o; // offset

        public Plane(Vector3 normal, Vector3 pointOnPlane) {
            n = normal.Normalized;
            o = Vector3.Dot(n, pointOnPlane);
        }

        public override string ToString() {
            return $"{n.x} {n.y} {n.z} {o}";
        }
    }
}
