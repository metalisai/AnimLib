namespace AnimLib;

/// <summary>
/// A blur effect applied to a canvas.
/// </summary>
public class CanvasBlurEffect : CanvasEffect {
    /// <summary>
    /// The radius of the blur. Setting this will set both the X and Y radius.
    /// </summary>
    public float Radius {
        get {
            var val = properties["radiusX"].Value;
            return (val as float?) ?? default(float);
        }
        set {
            properties["radiusX"].Value = value;
            properties["radiusY"].Value = value;
        }
    }

    /// <summary>
    /// The X radius of the blur.
    /// </summary>
    public float RadiusX {
        get {
            var val = properties["radiusX"].Value;
            return (val as float?) ?? 0.0f;
        }
        set {
            properties["radiusX"].Value = value;
        }
    }

    /// <summary>
    /// The Y radius of the blur.
    /// </summary>
    public float RadiusY {
        get {
            var val = properties["radiusY"].Value;
            return (val as float?) ?? default(float);
        }
        set {
            properties["radiusY"].Value = value;
        }
    }

    /// <summary>
    /// Create a new blur effect.
    /// </summary>
    public CanvasBlurEffect(float radius = 0.0f) {
        properties["radiusX"] = new DynProperty("radiusX", radius);
        properties["radiusY"] = new DynProperty("radiusY", radius);
    }
}

