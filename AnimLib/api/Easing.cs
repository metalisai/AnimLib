using System;
using System.Numerics;

namespace AnimLib;

/// <summary>
/// Easing functions for animations.
/// </summary>
public static class Ease {
    /// <summary>
    /// Interpolation curve type.
    /// </summary>
    public enum EaseType {
        /// <summary>Linear interpolation.</summary>
        Linear,
        /// <summary>Smooth interpolation.</summary>
        EaseInOut,
        /// <summary>Smooth interpolation with a overshoot bounce.</summary>
        Bouncy,
        /// <summary>Smooth interpolation with an elastic oscillation at the end.</summary>
        EaseOutElastic,
        /// <summary>Smooth interpolation with an elastic oscillation at beginning and end.</summary>
        EaseInOutElastic,
        /// <summary>Only ease in with a quadratic function.</summary>
        EaseInQuad,
    }

    /// <summary>
    /// Interpolate between two values using an easing function.
    /// </summary>
    /// <param name="t">Interpolation parameter.</param>
    /// <param name="type">Easing function type.</param>
    /// <returns>Interpolated value.</returns>
    /// <exception cref="NotImplementedException">Thrown if the easing function is not implemented.</exception>
    /// <typeparam name="T">Floating point type or vector type.</typeparam>
    public static T Evaluate<T>(T t, EaseType type)
        where T : IFloatingPoint<T>,
        ISubtractionOperators<T, T, T>,
        IMultiplyOperators<T, T, T>,
        IPowerFunctions<T>,
        ITrigonometricFunctions<T>
    {
        switch (type) {
            case EaseType.Bouncy:
            CubicBezier<T, T> bouncy11 = new (T.Zero, T.Zero, T.CreateChecked(1.5), T.One);
            return bouncy11.Evaluate(t);
            case EaseType.Linear:
            return t;
            case EaseType.EaseInOut:
            return Interp.EaseInOut(t);
            case EaseType.EaseOutElastic:
            return Interp.EaseOutElastic(t);
            case EaseType.EaseInOutElastic:
            return Interp.EaseInOutElastic(t);
            case EaseType.EaseInQuad:
            return Interp.EaseInQuad(t);
            default:
            throw new NotImplementedException();
        }
    }
}
