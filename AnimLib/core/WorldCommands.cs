using System;
using System.Collections.Generic;

namespace AnimLib;

internal record WorldCommand(double time);

internal record WorldSoundCommand(float volume, double time) : WorldCommand(time);

internal record WorldPlaySoundCommand(SoundSample sound, float volume, double time) : WorldSoundCommand(volume, time);

internal record WorldDynPropertyCommand (
    DynPropertyId propertyId,
    object? newvalue,
    object? oldvalue,
    double time
) : WorldCommand(time);

internal record WorldPropertyEvaluatorCreate (
    DynPropertyId propertyId,
    Func<Dictionary<DynPropertyId, object?>, object?> evaluator,
    object? oldValue, // value before first evaluation
    double time
) : WorldCommand(time);

internal record WorldPropertyEvaluatorDestroy (
    DynPropertyId propertyId,
    Func<Dictionary<DynPropertyId, object?>, object?> evaluator,
    object? finalValue, // value after last evaluation
    double time
) : WorldCommand(time);

internal record WorldCreateDynPropertyCommand (
    DynPropertyId propertyId,
    object? value,
    double time
) : WorldCommand(time);

internal record WorldCreateCommand(VisualEntity entity, double time) : WorldCommand(time);
internal record WorldDestroyCommand(VisualEntity entity, double time) : WorldCommand(time);

internal record WorldSetActiveCameraCommand(int cameraEntId, int oldCamEntId, double time) : WorldCommand(time);

internal record WorldEndCommand(double time) : WorldCommand(time);

internal record WorldCreateRenderBufferCommand(int width, int height, int id, double time) : WorldCommand(time);

internal record WorldMarkerCommand(string id, double time) : WorldCommand(time);

/// <summary>
/// Property types that are internally evaluated.
/// </summary>
public enum SpecialWorldPropertyType
{
    /// <summary> The current time in seconds. Starting from the beginning of the animation. </summary>
    Time,
};

internal record WorldSpecialPropertyCommand(DynPropertyId propertyId, SpecialWorldPropertyType property, double time) : WorldCommand(time);
