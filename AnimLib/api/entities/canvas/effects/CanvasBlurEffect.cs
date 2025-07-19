namespace AnimLib;

/// <summary>
/// A blur effect applied to a canvas.
/// </summary>
public class CanvasBlurEffect : CanvasEffect {
    private protected DynProperty<float> RadiusP;
    /// <summary>
    /// The radius of the blur.
    /// </summary>
    public DynProperty<float> Radius {
        get {
            return RadiusP;
        }
        set {
            RadiusP.Value = value.Value;
        }
    }

    /// <summary>
    /// Create a new blur effect.
    /// </summary>
    public CanvasBlurEffect(float radius = 0.0f) {
        this.RadiusP = new DynProperty<float>("radius", radius, RadiusP);
        properties["radius"] = this.RadiusP;
    }
}

