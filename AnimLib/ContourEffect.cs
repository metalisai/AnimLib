namespace AnimLib;

internal class PathEffectState {
    public int effectId;
    public PathEffectState() {
    }
}

/// <summary>
/// Applies a trim effect to a shape contour.
/// </summary>
public record TrimPathEffectState(float start, float end);

/// <summary>
/// Applies a contour effect to a shape.
/// </summary>
public class PathEffect {
    /// <summary>
    /// The effect to apply.
    /// </summary>
    public object Effect { get; init; }
}
