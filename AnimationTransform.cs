using System;
using System.Threading.Tasks;

namespace AnimLib {
    public static class AnimationTransform {
        public static async Task OrbitPoint(Transform obj, Vector3 axis, Vector3 p, float angle, float duration) {
            bool infinite = false;;
            if(duration == 0.0f) {
                infinite = true;
                duration = 1.0f;
            }
            double startTime = AnimationTime.Time;
            double endTime = startTime + duration;

            var pointToOrbit = p;
            var offset = obj.Pos - p;
            axis = axis.Normalized;

            while(infinite || AnimationTime.Time < endTime) {
                var t = (AnimationTime.Time - startTime)/duration;
                var a = (float)t*angle;
                var r = Quaternion.AngleAxis((a/180.0f)*(float)Math.PI, axis);
                obj.Pos = pointToOrbit + (r*offset);
                await AnimationTime.WaitFrame();
            }
        }

        public static async Task Offset(this Transform t, Vector3 offset, double duration = 1.0) {
            await SmoothT(x => {
                t.Pos = x;
            }, t.Pos, t.Pos+offset, duration);
        }

        public static async Task LerpFloat(Action<float> action, float start, float end, double duration) {
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (end-start)*t);
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        static CubicBezier1 bouncy1 = new CubicBezier1(0.0f, 0.0f, 1.5f, 1.0f);

        public static async Task BouncyFloat(Action<float> action, float start, float end, double duration) {
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (end-start)*bouncy1.Evaluate(t));
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        static CubicBezier1 smooth1 = new CubicBezier1(0.0f, 0.0f, 1.0f, 1.0f);
        public static async Task SmoothFloat(Action<float> action, float start, float end, double duration) {
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (end-start)*smooth1.Evaluate(t));
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        public static async Task LerpT<T>(Action<T> action, T start, T end, double duration) {
            dynamic startD = start;
            dynamic endD = end;
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (endD-startD)*t);
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        public static async Task BouncyT<T>(Action<T> action, T start, T end, double duration) {
            dynamic startD = start;
            dynamic endD = end;
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (endD-startD)*bouncy1.Evaluate(t));
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        public static async Task SmoothT<T>(Action<T> action, T start, T end, double duration) {
            dynamic startD = start;
            dynamic endD = end;
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                action.Invoke(start + (endD-startD)*smooth1.Evaluate(t));
                await AnimationTime.WaitFrame();
            }
            action.Invoke(end);
        }

        public static async Task SmoothColor(IColored entity, Color startColor, Color targetColor, double duration) {
            double endTime = AnimationTime.Time + duration;
            while(AnimationTime.Time < endTime) {
                double progress = 1.0 - (endTime - AnimationTime.Time)/duration;
                var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
                entity.Color = Color.Lerp(startColor, targetColor, (float)progress);
                await AnimationTime.WaitFrame();
            }
            entity.Color = targetColor;
        }

        public static Task SmoothColor(IColored entity, Color targetColor, double duration) {
            return SmoothColor(entity, entity.Color, targetColor, duration);
        }

        public static async Task AnimT(Action<double> action, double duration) {
            double startT = AnimationTime.Time;
            double endT = AnimationTime.Time + duration;
            while(AnimationTime.Time < endT) {
                action.Invoke(AnimationTime.Time - startT);
                await AnimationTime.WaitFrame();
            }
            action.Invoke(endT-startT);
        }

        public static async Task Sine(Action<float> action, double frequency, double amplitude = 1.0f, double timeOffset = 0.0f, double duration = 0.0) {
            bool infinite = duration <= 0.0;
            double startTime = AnimationTime.Time;
            double endTime = startTime + duration;
            while(infinite || AnimationTime.Time < endTime) {
                double t = AnimationTime.Time - startTime + timeOffset;
                var f = (float)(amplitude * Math.Sin(t*frequency*Math.PI*0.5));
                action.Invoke(f);
                await AnimationTime.WaitFrame();
            }
            action.Invoke((float)(amplitude * Math.Sin(duration*frequency*Math.PI*0.5)));
        }

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
