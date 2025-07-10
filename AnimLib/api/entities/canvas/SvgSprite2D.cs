namespace AnimLib;

using System;

/// <summary>
/// Internal state of a rectangle.
/// </summary>
public class SvgData
{
    /// <summary>
    /// SVG code.
    /// </summary>
    public required string svg;
    /// <summary>
    /// internal handle.
    /// </summary>
    public int handle;
}

[GenerateDynProperties(forType: typeof(SvgSprite))]
internal class SvgSpriteState : EntityState2D
{
    [Dyn]
    public float width;
    [Dyn]
    public float height;
    [Dyn]
    public SvgData svg;
    [Dyn]
    public Color color = Color.WHITE;

    public SvgSpriteState(SvgData svg, float width, float height)
    {
        this.svg = svg;
        this.width = width;
        this.height = height;
    }

    public SvgSpriteState(SvgSpriteState sprite) : base(sprite)
    {
        this.width = sprite.width;
        this.height = sprite.height;
        this.svg = sprite.svg;
        this.color = sprite.color;
    }

    public override Vector2 AABB
    {
        get
        {
            return new Vector2(width, height);
        }
    }
}

/// <summary>
/// A circle shaped entity.
/// </summary>
public partial class SvgSprite : DynVisualEntity2D
{
    /// <summary>
    /// Creates a new 2D Svg sprite with given svg source and dimensions.
    /// </summary>
    public SvgSprite(SvgData svg, float width = -1.0f, float height = -1.0f) : base()
    {
        this._svgP.Value = svg;
        this._widthP.Value = width;
        this._heightP.Value = height;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new SvgSpriteState(this._svgP.Value!, this._widthP.Value, this._heightP.Value);
        GetState(state, evaluator);
        return state;
    }
}
