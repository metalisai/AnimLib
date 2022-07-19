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
    }
}
