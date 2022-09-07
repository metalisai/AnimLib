using System;
using System.Threading.Tasks;

namespace AnimLib {
    public static class Animate {

        public enum InterpCurve {
            Linear,
            EaseInOut,
            Bouncy,
            EaseOutElastic,
            EaseInOutElastic,
        }

        static CubicBezier1 bouncy1 = new CubicBezier1(0.0f, 0.0f, 1.5f, 1.0f);
        static CubicBezier1 smooth1 = new CubicBezier1(0.0f, 0.0f, 1.0f, 1.0f);

        static CubicBezier<Vector2> smooth = new CubicBezier<Vector2>(new Vector2(0.0f, 0.0f), new Vector2(0.33f, 0.0f), new Vector2 (0.66f, 1.0f), new Vector2(1.0f, 1.0f));


        // Evaluate curve at t
        private static float EvtCurve(float t, InterpCurve curve) {
            switch(curve) {
                case InterpCurve.Bouncy:
                // TODO: this is not correct ?
                return bouncy1.Evaluate(t);
                case InterpCurve.Linear:
                return t;
                case InterpCurve.EaseInOut:
                return Interp.EaseInOut(t);
                case InterpCurve.EaseOutElastic:
                return Interp.EaseOutElastic(t);
                case InterpCurve.EaseInOutElastic:
                return Interp.EaseInOutElastic(t);
                default:
                throw new NotImplementedException();
            }
        }

        // TODO: velocity ramping not instant
        public static async Task OrbitPoint(Transform obj, Vector3 axis, Vector3 p, float angle, float duration) {
            bool infinite = false;;
            if(duration == 0.0f) {
                infinite = true;
                duration = 1.0f;
            }
            double startTime = AnimLib.Time.T;
            double endTime = startTime + duration;

            var pointToOrbit = p;
            var offset = obj.Pos - p;
            axis = axis.Normalized;

            while(infinite || AnimLib.Time.T < endTime) {
                var t = (AnimLib.Time.T - startTime)/ duration;
                var a = (float)t * angle;
                var r = Quaternion.AngleAxis((a / 180.0f)*(float)Math.PI, axis);
                obj.Pos = pointToOrbit + (r * offset);
                await AnimLib.Time.WaitFrame();
            }
        }

        public static async Task Offset(this Transform t, Vector3 offset, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
            await InterpT(x => {
                t.Pos = x;
            }, t.Pos, t.Pos+offset, duration, curve);
        }

        public static async Task Offset(this Transform2D t, Vector2 offset, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
            await InterpT(x => {
                t.Pos = x;
            }, t.Pos, t.Pos+offset, duration, curve);
        }

        public static async Task Move(this Transform2D t, Vector2 moveTo, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
            await InterpT(x => {
                t.Pos = x;
            }, t.Pos, moveTo, duration, curve);
        }

        public static async Task Move(this Transform t, Vector3 moveTo, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
            await InterpT(x => {
                t.Pos = x;
            }, t.Pos, moveTo, duration, curve);
        }

        // interpolates a float with given interpolation curve
        public static async Task InterpF(Action<float> action, float start, float end, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
            double endTime = AnimLib.Time.T + duration;
            while(AnimLib.Time.T < endTime) {
                double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                t = EvtCurve(t, curve);
                action.Invoke(start + (end - start) * t);
                await AnimLib.Time.WaitFrame();
            }
            action.Invoke(end);
        }

        // interpolates a dynamic type with given interpolation curve
        public static async Task InterpT<T>(Action<T> action, T start, T end, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
            dynamic startD = start;
            dynamic endD = end;
            double endTime = AnimLib.Time.T + duration;
            while(AnimLib.Time.T < endTime) {
                double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                t = EvtCurve(t, curve);
                action.Invoke(start + (endD - startD) * t);

                await AnimLib.Time.WaitFrame();
            }
            action.Invoke(end);
        }

        public static async Task Color(IColored entity, Color startColor, Color targetColor, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
            double endTime = AnimLib.Time.T + duration;
            while(AnimLib.Time.T < endTime) {
                double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                t = EvtCurve(t, curve);
                entity.Color = AnimLib.Color.LerpHSV(startColor, targetColor, (float)t);
                await AnimLib.Time.WaitFrame();
            }
            entity.Color = targetColor;
        }

        public static Task Color(IColored entity, Color targetColor, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
            return Color(entity, entity.Color, targetColor, duration, curve);
        }

        public static async Task Time(Action<double> action, double duration) {
            double startT = AnimLib.Time.T;
            double endT = AnimLib.Time.T + duration;
            while(AnimLib.Time.T < endT) {
                action.Invoke(AnimLib.Time.T - startT);
                await AnimLib.Time.WaitFrame();
            }
            action.Invoke(endT-startT);
        }

        public static async Task Sine(Action<float> action, double frequency, double amplitude = 1.0f, double timeOffset = 0.0f, double duration = 0.0) {
            bool infinite = duration <= 0.0;
            double startTime = AnimLib.Time.T;
            double endTime = startTime + duration;
            while(infinite || AnimLib.Time.T < endTime) {
                double t = AnimLib.Time.T - startTime + timeOffset;
                var f = (float)(amplitude * Math.Sin(t * frequency * Math.PI*0.5));
                action.Invoke(f);
                await AnimLib.Time.WaitFrame();
            }
            action.Invoke((float)(amplitude * Math.Sin(duration*frequency*Math.PI*0.5)));
        }

        // TODO: what is this for? just use quadratic spline?
        public static float SmoothSpeed(float startValue, float endValue, float t, float startTransitionTime, float duration) {
            if(t < startTransitionTime) {
                float pt = t/startTransitionTime;
                return smooth1.Evaluate(pt)*(endValue-startValue) + startValue;
            } else if(t < duration && t > duration-startTransitionTime) {
                float pt = 1.0f - ((t - duration + startTransitionTime) / startTransitionTime);
                return smooth1.Evaluate(pt)*(endValue-startValue) + startValue;
            } else if(t >= duration) {
                return startValue;
            }
            return endValue;
        }

    }
}
