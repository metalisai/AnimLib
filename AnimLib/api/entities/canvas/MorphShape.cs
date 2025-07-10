using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(MorphShape))]
internal class MorphShapeState : EntityState2D
{
    [Dyn]
    public float progress = 0.0f;
    [Dyn]
    public ShapePath shape1;
    [Dyn]
    public ShapePath shape2;
    [Dyn]
    public Color color1 = Color.RED;
    [Dyn]
    public Color color2 = Color.RED;
    [Dyn]
    public Color contourColor1 = Color.BLACK;
    [Dyn]
    public Color contourColor2 = Color.BLACK;
    [Dyn]
    public float contourSize1 = 0.0f;
    [Dyn]
    public float contourSize2 = 0.0f;
    [Dyn]
    public ShapeMode mode1 = ShapeMode.FilledContour;
    [Dyn]
    public ShapeMode mode2 = ShapeMode.FilledContour;

    public MorphShapeState(ShapePath shape1, ShapePath shape2,
        Color color1, Color color2,
        Color contourColor1, Color contourColor2,
        float contourSize1, float contourSize2,
        ShapeMode mode1, ShapeMode mode2
    ) : base()
    {
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

    public MorphShapeState(MorphShapeState rs) : base(rs)
    {
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

    public override Vector2 AABB
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal bool HasBody(ShapeMode mode)
    {
        return mode == ShapeMode.Filled || mode == ShapeMode.FilledContour;
    }

    internal bool HasContour(ShapeMode mode)
    {
        return mode == ShapeMode.Contour || mode == ShapeMode.FilledContour;
    }

    public Color CurrentColor
    {
        get
        {
            var a1 = color1.ToVector4().w;
            var a2 = color2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasBody(mode1) && !HasBody(mode2))
            {
                alpha *= 1.0f - progress;
            }
            else if (!HasBody(mode1) && HasBody(mode2))
            {
                alpha *= progress;
            }
            var col = Color.LerpHSV(color1, color2, progress);
            var rgb = col.WithA((byte)(alpha * 255));
            return rgb;
        }
    }

    public Color CurrentContourColor
    {
        get
        {
            var a1 = contourColor1.ToVector4().w;
            var a2 = contourColor2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasContour(mode1) && !HasContour(mode2))
            {
                alpha *= 1.0f - progress;
            }
            else if (!HasContour(mode1) && HasContour(mode2))
            {
                alpha *= progress;
            }
            var col = Color.LerpHSV(contourColor1, contourColor2, progress);
            var rgb = col.WithA((byte)(alpha * 255));
            return rgb;
        }
    }

    public ShapeMode CurrentMode
    {
        get
        {
            if (progress <= 0.0f)
            {
                return mode1;
            }
            else if (progress >= 1.0f)
            {
                return mode2;
            }
            else
            {
                bool needBody = HasBody(mode1) || HasBody(mode2);
                bool needContour = HasContour(mode1) || HasContour(mode2);
                if (needBody && needContour)
                {
                    return ShapeMode.FilledContour;
                }
                else if (needBody)
                {
                    return ShapeMode.Filled;
                }
                else if (needContour)
                {
                    return ShapeMode.Contour;
                }
                else
                {
                    return ShapeMode.None;
                }
            }
        }
    }

    public float CurrentContourSize
    {
        get
        {
            return contourSize1 + (contourSize2 - contourSize1) * progress;
        }
    }
}

/// <summary>
/// A morph shape between two shapes.
/// </summary>
public partial class MorphShape : DynVisualEntity2D
{
    /// <summary>
    /// Creates a new morph shape between two shapes.
    /// </summary>
    public MorphShape(DynShape a, DynShape b, float progress = 0.0f) : base(a)
    {
        this._shape1P.Value = a.Path;
        this._shape2P.Value = b.Path;
        this._progressP.Value = progress;
        this._color1P.Value = a.Color;
        this._color2P.Value = b.Color;
        this._contourColor1P.Value = a.ContourColor;
        this._contourColor2P.Value = b.ContourColor;
        this._mode1P.Value = a.Mode;
        this._mode2P.Value = b.Mode;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new MorphShapeState(
            _shape1P.Value!,
            _shape2P.Value!,
            _color1P.Value,
            _color2P.Value,
            _contourColor1P.Value,
            _contourColor2P.Value,
            _contourSize1P.Value,
            _contourSize2P.Value,
            _mode1P.Value,
            _mode2P.Value);
        GetState(state, evaluator);
        return state;
    }
}
