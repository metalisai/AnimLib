using System;

namespace AnimLib {
    internal static class AMath {
        public static float Mod(float a,float b) {
            return (MathF.Abs(a * b) + a) % b;
        }
    }
}
