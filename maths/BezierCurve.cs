using System;
using System.Collections.Generic;

namespace AnimLib {
    public class CubicBezier1 {
        public float p1, p2, p3, p4;
        public CubicBezier1(float p1, float p2, float p3, float p4) {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
        public float Evaluate(float t) {
            return BezierCurve.Cubic(this.p1, this.p2, this.p3, this.p4, t);
        }
    }

    public class CubicBezier<T> {
        public T p1, p2, p3, p4;
        public CubicBezier(T p1, T p2, T p3, T p4) {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
        public T Evaluate(float t) {
            return BezierCurve.Cubic<T>(this.p1, this.p2, this.p3, this.p4, t);
        }
    }

    public class BezierCurve {
        public static float Quadratic(float p1, float p2, float p3, float t) {
            float omt = 1.0f - t;
            return omt*omt*p1 + 2.0f*omt*t*p2 + t*t*p3;
        }

        public static Vector2 Quadratic(Vector2 p1, Vector2 p2, Vector2 p3, float t) {
            float omt = 1.0f - t;
            return omt*omt*p1 + 2.0f*omt*t*p2 + t*t*p3;
        }

        public static Vector3 Quadratic(Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float omt = 1.0f - t;
            return omt*omt*p1 + 2.0f*omt*t*p2 + t*t*p3;
        }

        public static float Cubic(float p1, float p2, float p3, float p4, float t) {
            float omt = 1.0f - t;
            return omt*omt*omt*p1 + 3*omt*omt*t*p2 + 3*omt*t*t*p3 + t*t*t*p4;
        }

        public static Vector2 Cubic(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float t) {
            float omt = 1.0f - t;
            return omt*omt*omt*p1 + 3*omt*omt*t*p2 + 3*omt*t*t*p3 + t*t*t*p4;
        }

        public static Vector3 Cubic(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t) {
            float omt = 1.0f - t;
            return omt*omt*omt*p1 + 3*omt*omt*t*p2 + 3*omt*t*t*p3 + t*t*t*p4;
        }

        public static T Cubic<T>(T p1, T p2, T p3, T p4, float t) {
            float omt = 1.0f - t;
            dynamic p1d = p1, p2d = p2, p3d = p3, p4d = p4;
            return omt*omt*omt*p1d + 3.0f*omt*omt*t*p2d + 3.0f*omt*t*t*p3d + t*t*t*p4d;
        }

        // first and last curves are quadratic
        // middle curves are cubic, but one of the handles is mirrored from next/previous curve
        public static Vector2[] LinearizeSpline(Vector2[] spline, int segsPerCurve) {
            float step = 1.0f / segsPerCurve;
            var ret = new List<Vector2>();
            
            int count = (spline.Length - 1) / 3;
            if(spline.Length < 4 || (spline.Length-1)%3 != 0)
                throw new ArgumentException();
            for(int i = 0; i < count; i++) {
                for(int s = 0; s <= segsPerCurve; s++) {
                    float t = s * step;
                    ret.Add(Cubic(spline[i*3 + 0], spline[i*3 + 1], spline[i*3 + 2], spline[i*3 + 3], t));
                }
            }

            return ret.ToArray();
        }
    }
}