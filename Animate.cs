using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// Helper class to animate entities. Part of the AnimLib user API.
/// </summary>
public static class Animate {

    /// <summary>
    /// Interpolation curve type.
    /// </summary>
    public enum InterpCurve {
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
    }

    static CubicBezier1 bouncy1 = new CubicBezier1(0.0f, 0.0f, 1.5f, 1.0f);
    static CubicBezier1 smooth1 = new CubicBezier1(0.0f, 0.0f, 1.0f, 1.0f);

    static CubicBezier<Vector2> smooth = new CubicBezier<Vector2>(new Vector2(0.0f, 0.0f), new Vector2(0.33f, 0.0f), new Vector2 (0.66f, 1.0f), new Vector2(1.0f, 1.0f));

    // Evaluate curve at t
    private static float EvtCurve(float t, InterpCurve curve) {
        switch (curve) {
            case InterpCurve.Bouncy:
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

    /// <summary>
    /// Orbits a transform perpendicular to the given axis. Movement starts at the current position and orbits the specified angle during the given duration.
    /// </summary>
    public static async Task OrbitPoint(Transform obj, Vector3 axis, Vector3 p, float angle, float duration) {
        // TODO: velocity ramping not instant
        bool infinite = false;;
        if (duration == 0.0f) {
            infinite = true;
            duration = 1.0f;
        }
        double startTime = AnimLib.Time.T;
        double endTime = startTime + duration;

        var pointToOrbit = p;
        var offset = obj.Pos - p;
        axis = axis.Normalized;

        while (infinite || AnimLib.Time.T < endTime) {
            var t = (AnimLib.Time.T - startTime)/ duration;
            var a = (float)t * angle;
            var r = Quaternion.AngleAxis((a / 180.0f)*(float)Math.PI, axis);
            obj.Pos = pointToOrbit + (r * offset);
            await AnimLib.Time.WaitFrame();
        }
    }

    /// <summary>
    /// Offset (move) a 3D entity from its current position.
    /// </summary>
    public static async Task Offset(this Transform t, Vector3 offset, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
        await InterpT(x => {
            t.Pos = x;
        }, t.Pos, t.Pos+offset, duration, curve);
    }

    /// <summary>
    /// Offset (move) a 2D entity from its current position.
    /// </summary>
    public static async Task Offset(this Transform2D t, Vector2 offset, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
        await InterpT(x => {
            t.Pos = x;
        }, t.Pos, t.Pos+offset, duration, curve);
    }

    /// <summary>
    /// Move a 2D entity to a given position.
    /// </summary>
    public static async Task Move(this Transform2D t, Vector2 moveTo, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
        await InterpT(x => {
            t.Pos = x;
        }, t.Pos, moveTo, duration, curve);
    }

    /// <summary>
    /// Move a 3D entity to a given position.
    /// </summary>
    public static async Task Move(this Transform t, Vector3 moveTo, double duration = 1.0, InterpCurve curve = InterpCurve.EaseInOut) {
        await InterpT(x => {
            t.Pos = x;
        }, t.Pos, moveTo, duration, curve);
    }

    /// <summary>
    /// Interpolate a float with given interpolation curve.
    /// </summary>
    public static async Task InterpF(Action<float> action, float start, float end, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
        double endTime = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endTime) {
            double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = EvtCurve(t, curve);
            action.Invoke(start + (end - start) * t);
            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(end);
    }

    /// <summary>
    /// Interpolate a dynamic type with given interpolation curve.
    /// </summary>
    public static async Task InterpT<T>(Action<T> action, T start, T end, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
        dynamic startD = start;
        dynamic endD = end;
        double endTime = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endTime) {
            double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = EvtCurve(t, curve);
            action.Invoke(start + (endD - startD) * t);

            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(end);
    }

    /// <summary>
    /// Interpolates a <c>IColored</c> entity from a start color to a target color. Uses HSV color space.
    /// </summary>
    public static async Task Color(IColored entity, Color startColor, Color targetColor, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
        double endTime = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endTime) {
            double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = EvtCurve(t, curve);
            entity.Color = AnimLib.Color.LerpHSV(startColor, targetColor, (float)t);
            await AnimLib.Time.WaitFrame();
        }
        entity.Color = targetColor;
    }

    /// <summary>
    /// Interpolates a <c>IColored</c> entity from its current color to a target color. Uses HSV color space.
    /// </summary>
    public static Task Color(IColored entity, Color targetColor, double duration, InterpCurve curve = InterpCurve.EaseInOut) {
        return Color(entity, entity.Color, targetColor, duration, curve);
    }

    /// <summary>
    /// Accepts an animation lambda that takes current time as a parameter. The lambda is called every frame until the duration is reached.
    /// </summary>
    public static async Task Time(Action<double> action, double duration) {
        double startT = AnimLib.Time.T;
        double endT = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endT) {
            action.Invoke(AnimLib.Time.T - startT);
            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(endT-startT);
    }

    /// <summary>
    /// Accepts a lambda that's called every frame.
    /// </summary>
    public static async Task Update(Action action, CancellationToken token) {
        while (!token.IsCancellationRequested) {
            action.Invoke();
            await AnimLib.Time.WaitFrame();
        }
    }

    /// <summary>
    /// Accepts a lambda that's called every frame with a parameter that follows a sine wave.
    /// </summary>
    public static async Task Sine(Action<float> action, double frequency, double amplitude = 1.0f, double timeOffset = 0.0f, double duration = 0.0) {
        bool infinite = duration <= 0.0;
        double startTime = AnimLib.Time.T;
        double endTime = startTime + duration;
        while (infinite || AnimLib.Time.T < endTime) {
            double t = AnimLib.Time.T - startTime + timeOffset;
            var f = (float)(amplitude * Math.Sin(t * frequency * Math.PI*0.5));
            action.Invoke(f);
            await AnimLib.Time.WaitFrame();
        }
        action.Invoke((float)(amplitude * Math.Sin(duration*frequency*Math.PI*0.5)));
    }

    /// <summary>
    /// Morphs a shape into another shape.
    /// </summary>
    public static async Task<Shape> CreateMorph(Shape startShape, Shape endShape, float duration, InterpCurve curve = InterpCurve.EaseInOut, bool destroyStartShape = true)
    {
        var morph = new MorphShape(startShape, endShape);
        if (destroyStartShape && startShape.created) {
            World.current.Destroy(startShape);
        }
        World.current.CreateInstantly(morph);
        double startTime = AnimLib.Time.T;
        double endTime = startTime + duration;
        while (AnimLib.Time.T - startTime < duration) {
            double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = EvtCurve(t, curve);
            morph.Progress = t;
            await AnimLib.Time.WaitFrame();
        }
        World.current.Destroy(morph);
        var newShape = (Shape)endShape.Clone();
        World.current.CreateInstantly(newShape);
        return newShape;
    }
}
