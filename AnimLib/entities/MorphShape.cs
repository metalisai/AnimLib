namespace AnimLib;

internal class MorphShapeState : EntityState2D {
    public float progress = 0.0f;
    public ShapePath shape1;
    public ShapePath shape2;
    public Color color1 = Color.RED;
    public Color color2 = Color.RED;
    public Color contourColor1 = Color.BLACK;
    public Color contourColor2 = Color.BLACK;
    public float contourSize1 = 0.0f;
    public float contourSize2 = 0.0f;
    public ShapeMode mode1 = ShapeMode.FilledContour;
    public ShapeMode mode2 = ShapeMode.FilledContour;

    public MorphShapeState(Shape shape1, Shape shape2) : base(shape1.state) {
        this.shape1 = shape1.Path;
        this.shape2 = shape2.Path;
        this.color1 = shape1.Color;
        this.color2 = shape2.Color;
        this.contourColor1 = shape1.ContourColor;
        this.contourColor2 = shape2.ContourColor;
        this.contourSize1 = shape1.ContourSize;
        this.contourSize2 = shape2.ContourSize;
        this.mode1 = shape1.Mode;
        this.mode2 = shape2.Mode;
    }

    public MorphShapeState(ShapePath shape1, ShapePath shape2,
        Color color1, Color color2,
        Color contourColor1, Color contourColor2,
        float contourSize1, float contourSize2,
        ShapeMode mode1, ShapeMode mode2
    ) : base() {
        this.shape1 = shape1;
        this.shape2 = shape2;
        this.color1 = color1;
        this.color2 = color2;
        this.contourColor1 = contourColor1;
        this.contourColor2 = contourColor2;
        this.contourSize1 = contourSize1;
        this.contourSize2 = contourSize2;
        this.mode1 = mode1;
        this.mode2 = mode2;
    }

    public MorphShapeState(MorphShapeState rs) : base(rs) {
        this.progress = rs.progress;
        this.shape1 = rs.shape1;
        this.shape2 = rs.shape2;
        this.color1 = rs.color1;
        this.color2 = rs.color2;
        this.contourColor1 = rs.contourColor1;
        this.contourColor2 = rs.contourColor2;
        this.contourSize1 = rs.contourSize1;
        this.contourSize2 = rs.contourSize2;
        this.mode1 = rs.mode1;
        this.mode2 = rs.mode2;
    }

    public override Vector2 AABB {
        get {
            throw new System.NotImplementedException();
        }
    }

    public override object Clone()
    {
        return new MorphShapeState(this);
    }

    internal bool HasBody(ShapeMode mode) {
        return mode == ShapeMode.Filled || mode == ShapeMode.FilledContour;
    }

    internal bool HasContour(ShapeMode mode) {
        return mode == ShapeMode.Contour || mode == ShapeMode.FilledContour;
    }

    public Color CurrentColor {
        get {
            var a1 = color1.ToVector4().w;
            var a2 = color2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasBody(mode1) && !HasBody(mode2)) {
                alpha *= 1.0f - progress;
            } else if (!HasBody(mode1) && HasBody(mode2)) {
                alpha *= progress;
            } 
            var col = Color.LerpHSV(color1, color2, progress);
            var rgb = col.WithA((byte)(alpha*255));
            return rgb;
        }
    }

    public Color CurrentContourColor {
        get {
            var a1 = contourColor1.ToVector4().w;
            var a2 = contourColor2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasContour(mode1) && !HasContour(mode2)) {
                alpha *= 1.0f - progress;
            } else if (!HasContour(mode1) && HasContour(mode2)) {
                alpha *= progress;
            } 
            var col = Color.LerpHSV(contourColor1, contourColor2, progress);
            var rgb = col.WithA((byte)(alpha*255));
            return rgb;
        }
    }

    public ShapeMode CurrentMode {
        get {
            if (progress <= 0.0f) {
                return mode1;
            } else if (progress >= 1.0f) {
                return mode2;
            } else {
                bool needBody = HasBody(mode1) || HasBody(mode2);
                bool needContour = HasContour(mode1) || HasContour(mode2);
                if (needBody && needContour) {
                    return ShapeMode.FilledContour;
                } else if (needBody) {
                    return ShapeMode.Filled;
                } else if (needContour) {
                    return ShapeMode.Contour;
                } else {
                    return ShapeMode.None;
                }
            }
        }
    }

    public float CurrentContourSize {
        get {
            return contourSize1 + (contourSize2 - contourSize1) * progress;
        }
    }
}

/// <summary>
/// A morph shape between two shapes.
/// </summary>
public class MorphShape : VisualEntity2D {
    /// <summary>
    /// Creates a new morph shape between two shapes.
    /// </summary>
    public MorphShape(Shape a, Shape b, float progress = 0.0f) : base(new MorphShapeState(a, b)) {
        this.Progress = progress;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public MorphShape(MorphShape ms) : base(ms) {
    }

    /// <summary>
    /// The progress of the morphing.
    /// </summary>
    public float Progress {
        get {
            return ((MorphShapeState)state).progress;
        }
        set {
            World.current.SetProperty(this, "Progress", value, ((MorphShapeState)state).progress);
            ((MorphShapeState)state).progress = value;
        }
    }

    /// <summary>
    /// Clone this morph shape.
    /// </summary>
    public override object Clone() {
        return new MorphShape(this);
    }
}
