using System;
using System.Collections.Generic;

namespace AnimLib {
    public static class Interp {
        public static float Lerp(float a, float b, float t) {
            // less numerical stability
            //return a + (b - a)*t;
            return (a * (1.0f - t)) + (b * t);
        }

        public static float EaseIn(float t) {
            return t*t;
        }

        public static float EaseOut(float t) {
            return 1.0f - (1.0f - t)*(1.0f - t);
        }

        public static float EaseInOut(float t) {
            float ein = t*t;
            float eout = 1.0f - (1.0f - t)*(1.0f - t);
            return Lerp(ein, eout, t);
        }

        // https://easings.net/#easeOutElastic
        public static float EaseOutElastic(float x) {
            const float c4 = (2.0f * MathF.PI) / 3.0f;
            return x == 0.0f
              ? 0.0f
              : x == 1.0f
              ? 1.0f
              : MathF.Pow(2.0f, -10.0f * x) * MathF.Sin((x * 10.0f - 0.75f) * c4) + 1.0f;
        }

        //https://easings.net/#easeInOutElastic
        // TODO: this does not seem to work properly
        public static float EaseInOutElastic(float x) {
            const float c5 = (2.0f * MathF.PI) / 4.5f;
            return x == 0.0f
              ? 0.0f
              : x == 1.0f
              ? 1.0f
              : x < 0.5f
              ? -(MathF.Pow(2.0f, 20.0f * x - 10.0f) * MathF.Sin((20.0f * x - 11.125f) * c5)) / 2.0f
              : (MathF.Pow(2.0f, -20.0f * x + 10.0f) * MathF.Sin((20.0f * x - 11.125f) * c5)) / 2.0f + 1.0f;

        }
    }
}
