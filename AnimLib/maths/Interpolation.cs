using System.Numerics;

namespace AnimLib;

/// <summary>
/// Utility class for easing functions.
/// </summary>
public static class Interp {
    /// <summary>
    /// Linear interpolation.
    /// </summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F Lerp<F>(F a, F b, F t) 
        where F : IFloatingPoint<F>
    {
        // less numerical stability
        //return a + (b - a)*t;
        return (a * (F.One - t)) + (b * t);
    }

    /// <summary>
    /// Ease in
    /// </summary>
    /// <param name="t">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value with an ease in.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F EaseIn<F>(F t) 
        where F : IFloatingPoint<F>
    {
        return t*t;
    }

    /// <summary>
    /// Ease out
    /// </summary>
    /// <param name="t">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value with an ease out.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F EaseOut<F>(F t) 
        where F : IFloatingPoint<F>
    {
        return F.One - (F.One - t)*(F.One - t);
    }

    /// <summary>
    /// Ease in and out
    /// </summary>
    /// <param name="t">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value with an ease in and out.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F EaseInOut<F>(F t) 
        where F : IFloatingPoint<F>
    {
        F ein = t*t;
        F eout = F.One - (F.One - t)*(F.One - t);
        return Lerp(ein, eout, t);
    }

    // https://easings.net/#easeOutElastic
    /// <summary>
    /// Ease in
    /// </summary>
    /// <param name="x">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value with an elastic ease out.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F EaseOutElastic<F>(F x) 
        where F : IFloatingPoint<F>,
            IPowerFunctions<F>,
            ITrigonometricFunctions<F>
    {
        F c4 = (F.CreateChecked(2.0) * F.Pi) / F.CreateChecked(3.0);
        return x == F.Zero
          ? F.Zero
          : x == F.One
          ? F.One
          : F.Pow(F.CreateChecked(2.0), F.CreateChecked(-10.0)*x) * F.Sin((x * F.CreateChecked(10.0) - F.CreateChecked(0.75)) * c4) + F.One;
    }

    //https://easings.net/#easeInOutElastic
    // TODO: this does not seem to work properly
    /// <summary>
    /// Ease in and out elastic
    /// </summary>
    /// <param name="x">Interpolation factor [0..1]</param>
    /// <returns>The interpolated value with an elastic ease in and out.</returns>
    /// <typeparam name="F">Floating point type.</typeparam>
    public static F EaseInOutElastic<F>(F x) 
        where F : IFloatingPoint<F>,
            IPowerFunctions<F>,
            ITrigonometricFunctions<F>
    {
        F c5 = (F.CreateChecked(2.0) * F.Pi) / F.CreateChecked(4.5f);
        return x == F.Zero
          ? F.Zero
          : x == F.One
          ? F.One
          : x < F.CreateChecked(0.5f)
          ? -(F.Pow(F.CreateChecked(2.0), F.CreateChecked(20.0) * x - F.CreateChecked(10.0)) * F.Sin((F.CreateChecked(20.0) * x - F.CreateChecked(11.125)) * c5)) / F.CreateChecked(2.0)
          : (F.Pow(F.CreateChecked(2.0), F.CreateChecked(-20.0) * x + F.CreateChecked(10.0)) * F.Sin((F.CreateChecked(20.0) * x - F.CreateChecked(11.125f)) * c5)) / F.CreateChecked(2.0f) + F.One;

    }
}
