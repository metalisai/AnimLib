namespace AnimLib;

/// <summary>
/// A blur effect applied to a canvas.
/// </summary>
public class CanvasAlphaThresholdEffect : CanvasEffect {
    private protected DynProperty<float> innerP;
    /// <summary>
    /// Lower bound of the alpha threshold.
    /// </summary>
    public DynProperty<float> Inner {
        get {
            return innerP;
        }
        set {
            innerP.Value = value.Value;
        }
    }

    private protected DynProperty<float> outerP;
    /// <summary>
    /// Upper bound of the alpha threshold.
    /// </summary>
    public DynProperty<float> Outer {
        get {
            return outerP;
        }
        set {
            outerP.Value = value.Value;
        }
    }

    private protected DynProperty<Vector4> rectP;
    /// <summary>
    /// The rectangle to apply the alpha threshold to.
    /// </summary>
    public DynProperty<Vector4> Rect {
        get {
            return rectP;
        }
        set {
            rectP.Value = value.Value;
        }
    }


    /// <summary>
    /// Create a new blur effect.
    /// </summary>
    public CanvasAlphaThresholdEffect(float inner, float outer, Vector4 rect) {
        innerP = new DynProperty<float>("inner", inner, innerP);
        outerP = new DynProperty<float>("outer", outer, outerP);
        rectP = new DynProperty<Vector4>("rect", rect, rectP);
        properties["inner"] = this.innerP;
        properties["outer"] = this.outerP;
        properties["rect"] = this.rectP;
    }
}

