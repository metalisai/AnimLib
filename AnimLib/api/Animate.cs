using System;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections.Generic;

using EaseType = AnimLib.Ease.EaseType;

namespace AnimLib;

/// <summary>
/// Text creation mode.
/// </summary>
public enum TextCreationMode {
    /// <summary>
    /// The text instantly appears.
    /// </summary>
    Instant,
    /// <summary>
    /// Fade the text in;
    /// </summary>
    Fade,
    /// <summary>
    /// Fade the text in with a path animation.
    /// </summary>
    PathAndFade,
}

/// <summary>
/// Helper class to animate entities. Part of the AnimLib user API.
/// </summary>
public static class Animate {
    /// <summary>
    /// Orbits a transform perpendicular to the given axis. Movement starts at the current position and orbits the specified angle during the given duration.
    /// </summary>
    /// <param name="obj">The object's transform to orbit.</param>
    /// <param name="axis">The axis to orbit around.</param>
    /// <param name="p">The center of the orbit.</param>
    /// <param name="angle">The total angle to orbit.</param>
    /// <param name="duration">The duration of the orbit operation.</param>
    /*public static async Task OrbitPoint(Transform obj, Vector3 axis, Vector3 p, float angle, float duration) {
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
            var et = Ease.Evaluate(t, EaseType.EaseInOut);
            var a = (float)et * angle;
            var r = Quaternion.AngleAxis((a / 180.0f)*(float)Math.PI, axis);
            obj.Pos = pointToOrbit + (r * offset);
            await AnimLib.Time.WaitFrame();
        }
    }*/

    /// <summary>
    /// Orbits a transform perpendicular to the given axis. Movement starts at the current position and orbits the specified angle during the given duration.
    /// The transform will look at the center of the orbit point.
    /// </summary>
    /// <param name="obj">The object's transform to orbit.</param>
    /// <param name="axis">The axis to orbit around.</param>
    /// <param name="p">The center of the orbit.</param>
    /// <param name="angle">The total angle to orbit.</param>
    /// <param name="duration">The duration of the orbit operation.</param>
    /// <returns>A task that represents the animation operation.</returns>
    public static async Task OrbitAndLookAt(VisualEntity3D obj, Vector3 axis, Vector3 p, float angle, float duration)
    {
        // TODO: velocity ramping not instant
        double startTime = AnimLib.Time.T;
        double endTime = startTime + duration;

        var pointToOrbit = p;
        var offset = obj.Position - p;
        axis = axis.Normalized;

        _ = InterpF(obj.PositionProperty, time =>
        {
            var et = Ease.Evaluate(time, EaseType.EaseInOut);
            var a = (float)et * angle;
            var r = Quaternion.AngleAxis((a / 180.0f) * (float)Math.PI, axis);
            return pointToOrbit + (r * offset);
        }, duration);

        await InterpF(obj.RotationProperty, time =>
        {
            var et = Ease.Evaluate(time, EaseType.EaseInOut);
            var a = (float)et * angle;
            var r = Quaternion.AngleAxis((a / 180.0f) * (float)Math.PI, axis);
            var pos = pointToOrbit + (r * offset);
            return Quaternion.LookRotation((p - pos).Normalized, axis);
        }, duration); 
    }

    public static Task Offset(this VisualEntity3D ent, Vector3 offset, double duration = 1.0, EaseType curve = EaseType.EaseInOut)
    {
        return InterpT(ent.PositionProperty, ent.Position + offset, duration, curve);
    }

    public static Task Offset(this VisualEntity2D ent, Vector2 offset, double duration = 1.0, EaseType curve = EaseType.EaseInOut)
    {
        return InterpT(ent.PositionProperty, ent.Position + offset, duration, curve);
    }
    
    public static Task Move(this VisualEntity2D ent, Vector2 moveTo, double duration = 1.0, EaseType curve = EaseType.EaseInOut)
    {
        return InterpT(ent.PositionProperty, moveTo, duration, curve);
    }

    /// <summary>
    /// Interpolate a float with given interpolation curve.
    /// </summary>
    /// <param name="action">The action to perform with the interpolated value.</param>
    /// <param name="start">The start value of the interpolation.</param>
    /// <param name="end">The end value of the interpolation.</param>
    /// <param name="duration">The duration of the interpolation.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    public static async Task InterpF(Action<float> action, float start, float end, double duration, EaseType curve = EaseType.EaseInOut)
    {
        double endTime = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endTime)
        {
            double progress = 1.0 - (endTime - AnimLib.Time.T) / duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = Ease.Evaluate(t, curve);
            action.Invoke(start + (end - start) * t);
            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(end);
    }

    /// <summary>
    /// Interpolate a property using an evaluator that takes time-dependent value as input
    /// </summary>
    /// <param name="property">The property to interpolate.</param>
    /// <param name="evaluator">The evaluator that takes time-dependent value as input. Evaluators with captures or side-effects should be avoided.</param>
    /// <param name="start">The start value of the input parameter.</param>
    /// <param name="end">The end value of the input parameter.</param>
    /// <param name="duration">The duration of the interpolation.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    /// <typeparam name="T">The type of the property.</typeparam>
    public static async Task InterpF<T>(DynProperty<T> property, Func<float, T> evaluator, double duration, float start = 0.0f, float end = 1.0f, EaseType curve = EaseType.EaseInOut) {
        double endTime = AnimLib.Time.T + duration;
        double startT = AnimLib.Time.T;
        var timePropertyId = World.current.CurrentTime.Id;
        Func<Dictionary<DynPropertyId, object?>, object?> timeEvaluator = (dict) => {
            var time = dict[timePropertyId] as double? ?? default(float);
            var relativeTime = (double)time - startT;
            var t = (float)Math.Clamp(relativeTime / duration, 0.0f, 1.0f);
            t = Ease.Evaluate(t, curve);
            if (relativeTime < duration) {
                return evaluator.Invoke(start + (end - start) * t);
            } else {
                return evaluator.Invoke(end);
            }
        };
        World.current.BeginEvaluator(property, timeEvaluator);
        await AnimLib.Time.WaitUntilT(endTime);
        World.current.EndEvaluator(property, evaluator.Invoke(end));
    }

    /// <summary>
    /// Interpolate a chosen type with given interpolation curve.
    /// </summary>
    /// <param name="action">The action to perform with the interpolated value.</param>
    /// <param name="start">The start value of the interpolation.</param>
    /// <param name="end">The end value of the interpolation.</param>
    /// <param name="duration">The duration of the interpolation.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    /// <typeparam name="T">The type of the input and output values.</typeparam>
    /// <typeparam name="F">The type of the duration.</typeparam>
    public static async Task InterpT<T,F>(Action<T> action, T start, T end, F duration, EaseType curve = EaseType.EaseInOut) 
        where T : 
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IMultiplyOperators<T, F, T>
        where F : 
            IFloatingPoint<F>,
            IPowerFunctions<F>,
            ITrigonometricFunctions<F>
    {
        T startD = start;
        T endD = end;
        F endTime = F.CreateChecked(AnimLib.Time.T) + duration;
        while (F.CreateChecked(AnimLib.Time.T) < endTime) {
            F progress = F.One - (endTime - F.CreateChecked(AnimLib.Time.T))/ duration;
            var t = F.Clamp(progress, F.Zero, F.One);
            t = Ease.Evaluate(t, curve);
            action.Invoke(start + (endD - startD) * t);

            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(end);
    }

    public static async Task InterpT<T>(DynProperty<T> prop, T end, double duration, EaseType curve = EaseType.EaseInOut)
    where T :
            struct,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IMultiplyOperators<T, double, T>
    {
        T startD = prop.Value!;
        T endD = end;
        double endTime = AnimLib.Time.T + duration;
        await InterpF(prop, time =>
        {
            var t = Ease.Evaluate(time, curve);
            return startD + (endD - startD) * t;
        }, duration);           
    }

    /// <summary>
    /// Interpolates a <c>IColored</c> entity from a start color to a target color. Uses HSV color space.
    /// </summary>
    /// <param name="entity">The entity to change color.</param>
    /// <param name="startColor">The start color.</param>
    /// <param name="targetColor">The target color.</param>
    /// <param name="duration">The duration of the color change.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    public static async Task Color(IColored entity, Color startColor, Color targetColor, double duration, EaseType curve = EaseType.EaseInOut)
    {
        double endTime = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endTime)
        {
            double progress = 1.0 - (endTime - AnimLib.Time.T) / duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = Ease.Evaluate(t, curve);
            entity.Color = AnimLib.Color.LerpHSV(startColor, targetColor, (float)t);
            await AnimLib.Time.WaitFrame();
        }
        entity.Color = targetColor;
    }

    /// <summary>
    /// Interpolates a <c>IColored</c> entity from its current color to a target color. Uses HSV color space.
    /// </summary>
    /// <param name="entity">The entity to change color.</param>
    /// <param name="targetColor">The target color.</param>
    /// <param name="duration">The duration of the color change.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    public static Task Color(IColored entity, Color targetColor, double duration, EaseType curve = EaseType.EaseInOut) {
        return Color(entity, entity.Color, targetColor, duration, curve);
    }

    /// <summary>
    /// Accepts an animation lambda that takes current time as a parameter. The lambda is called every frame until the duration is reached.
    /// </summary>
    /// <param name="action">The animation lambda.</param>
    /// <param name="duration">The duration of the animation.</param>
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
    /// Animates according to equations of motion.
    /// </summary>
    /// <param name="action">The action to call every frame.</param>
    /// <param name="x0">The initial position.</param>
    /// <param name="a">The acceleration.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="v0">The initial velocity.</param>
    /// <typeparam name="T">The type of the values. Scalar or a vector.</typeparam>
    public static async Task Accelerate<T>(Action<T> action, T x0, T a, float duration, T v0 = default(T))
        where T : 
            struct,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IMultiplyOperators<T, float, T>
    {
        double startT = AnimLib.Time.T;
        double endT = AnimLib.Time.T + duration;
        while (AnimLib.Time.T < endT) {
            double currentT = AnimLib.Time.T - startT;
            T currentValue = x0 + v0*(float)currentT + a*(float)currentT*(float)currentT;
            action.Invoke(currentValue);
            await AnimLib.Time.WaitFrame();
        }
        action.Invoke(x0 + v0*duration + a*duration*duration);
    }

    /// <summary>
    /// Animates a transform according to equations of motion.
    /// </summary>
    /// <param name="t">The transform.</param>
    /// <param name="a">The acceleration.</param>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="v0">The initial velocity.</param>
    /*public static Task Accelerate(Transform2D t, Vector2 a, float duration, Vector2 v0)
    {
        Action<Vector2> action = x => t.Pos = x;
        return Accelerate(action, t.Pos, a, duration, v0);
    }*/

    /// <summary>
    /// Accepts a lambda that's called every frame.
    /// </summary>
    /// <param name="action">The lambda to call every frame.</param>
    /// <param name="token">The cancellation token that allows cancelling the operation.</param>
    public static async Task Update(Action action, CancellationToken token) {
        while (!token.IsCancellationRequested) {
            action.Invoke();
            await AnimLib.Time.WaitFrame();
        }
    }

    /// <summary>
    /// Accepts a lambda that's called every frame with a parameter that follows a sine wave.
    /// </summary>
    /// <param name="action">The action to call every frame.</param>
    /// <param name="frequency">The frequency of the sine wave.</param>
    /// <param name="amplitude">The amplitude of the sine wave.</param>
    /// <param name="timeOffset">The time offset of the sine wave.</param>
    /// <param name="duration">The duration of the sine wave.</param>
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
    /// <param name="startShape">The shape to morph from.</param>
    /// <param name="endShape">The shape to morph to.</param>
    /// <param name="duration">The duration of the morph.</param>
    /// <param name="curve">The interpolation curve to use.</param>
    /// <param name="destroyStartShape">Whether to destroy the start shape when creating the morph shape.</param>
    public static async Task<Shape> CreateMorph(Shape startShape, Shape endShape, float duration, EaseType curve = EaseType.EaseInOut, bool destroyStartShape = true)
    {
        var morph = new MorphShape(startShape, endShape);
        if (destroyStartShape && startShape.Created) {
            World.current.Destroy(startShape);
        }
        World.current.CreateInstantly(morph);
        double startTime = AnimLib.Time.T;
        double endTime = startTime + duration;
        while (AnimLib.Time.T - startTime < duration) {
            double progress = 1.0 - (endTime - AnimLib.Time.T)/ duration;
            var t = (float)Math.Clamp(progress, 0.0f, 1.0f);
            t = Ease.Evaluate(t, curve);
            morph.Progress = t;
            await AnimLib.Time.WaitFrame();
        }
        World.current.Destroy(morph);
        var newShape = (Shape)endShape.Clone();
        newShape.Anchor = startShape.Anchor;
        newShape.Position = startShape.Position;
        newShape.Rotation = startShape.Rotation;
        newShape.Scale = startShape.Scale;
        World.current.CreateInstantly(newShape);
        return newShape;
    }

    /// <summary>
    /// Create text with a fancy animation.
    /// </summary>
    /// <param name="text">The text to create.</param>
    /// <param name="mode">The creation mode.</param>
    /// <param name="charDelay">The delay between creating each character.</param>
    public static async Task CreateText(Text2D text, TextCreationMode mode = TextCreationMode.PathAndFade, float charDelay = 0.1f)
    {
        var last = Task.CompletedTask;
        switch (mode)
        {
            default:
            case TextCreationMode.Instant:
                World.current.CreateInstantly(text);
                break;
            case TextCreationMode.Fade:
                foreach (var c in text.CurrentShapes)
                {
                    c.s.Mode = ShapeMode.Filled;
                    c.s.Color = text.Color.WithA(0);
                    c.s.Trim = (0.0f, 1.0f);
                }
                text.Color = text.Color.WithA(0);
                World.current.CreateInstantly(text);
                foreach (var c in text.CurrentShapes)
                {
                    last = Animate.Color(c.s, c.s.Color.WithA(0), c.s.Color.WithA(255), 0.5f);
                    await AnimLib.Time.WaitSeconds(charDelay);
                }
                await last;
                break;
            case TextCreationMode.PathAndFade:
                foreach (var c in text.CurrentShapes)
                {
                    c.s.ContourColor = AnimLib.Color.BLACK;
                    c.s.Mode = ShapeMode.Contour;
                    c.s.Trim = (0.0f, 0.0f);
                }
                World.current.CreateInstantly(text);
                foreach (var c in text.CurrentShapes)
                {
                    async Task AnimateCreation(float from, float to)
                    {
                        await Animate.InterpF(x => c.s.Trim = (from, x), from, to, 0.5f);
                        c.s.Color = text.Color.WithA(0);
                        c.s.Mode = ShapeMode.FilledContour;
                        await Animate.Color(c.s, c.s.Color.WithA(0), c.s.Color.WithA(255), 0.5f);
                        c.s.Mode = ShapeMode.FilledContour;
                    }
                    _ = AnimateCreation(0.0f, 1.0f);
                    await AnimLib.Time.WaitSeconds(charDelay);
                }
                await last;
                break;
        }
    }
}
