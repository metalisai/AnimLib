using System;
using System.Collections.Generic;
using System.Numerics;

namespace AnimLib {
    public class CubicBezier<T,F> 
        where T : 
            IMultiplyOperators<T, F, T>,
            IAdditionOperators<T, T, T>
        where F :
            IFloatingPoint<F>
    {
        public T p1, p2, p3, p4;
        public CubicBezier(T p1, T p2, T p3, T p4) {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
        public T Evaluate(F t) {
            return BezierCurve.Cubic<T, F>(this.p1, this.p2, this.p3, this.p4, t);
        }
    }

    /// <summary>
    /// Utility class for evaluating bezier curves.
    /// </summary>
    public class BezierCurve {
        /// <summary>
        /// Cubic bezier curve.
        /// </summary>
        public static T Cubic<T,F>(T p1, T p2, T p3, T p4, F t) 
            where T : 
                IMultiplyOperators<T, F, T>,
                IAdditionOperators<T, T, T>
            where F : 
                IFloatingPoint<F>
        {
            F omt = F.One - t;
            T p1d = p1, p2d = p2, p3d = p3, p4d = p4;
            return p1d*omt*omt*omt + p2d*F.CreateChecked(3.0)*omt*omt*t + p3d*F.CreateChecked(3.0)*omt*t*t + p4d*t*t*t;
        }

        /// <summary>
        /// Quadratic bezier curve.
        /// </summary>
        public static T Quadratic<T,F>(T p1, T p2, T p3, F t) 
            where T : 
                IMultiplyOperators<T, F, T>,
                IAdditionOperators<T, T, T>
            where F :
                IFloatingPoint<F>
        {
            F omt = F.One - t;
            T p1d = p1, p2d = p2, p3d = p3;
            return p1d*omt*omt + p2d*F.CreateChecked(2.0f)*omt*t + p3d*t*t;
        }

        /// <summary>
        /// Rational quadratic bezier curve.
        /// </summary>
        public static T Conic<T,F>(T p1, T p2, T p3, F conicWeight, F t) 
            where T : 
                IMultiplyOperators<T, F, T>,
                IAdditionOperators<T, T, T>
            where F :
                IFloatingPoint<F>
        {
            F omt = F.One - t;
            F omt2 = omt*omt;
            F denom = omt2 + F.CreateChecked(2.0)*conicWeight*t*omt + t*t;
            T inner = p1*omt2 + p2*F.CreateChecked(2.0)*conicWeight*t*omt + p3*t*t;
            return inner*(F.One / denom);
        }

        /// <summary>
        /// Linearizes a spline. First and last curves are quadratic
        /// Middle curves are cubic, but one of the handles is mirrored from next/previous curve.
        /// </summary>
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
