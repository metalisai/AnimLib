namespace AnimLib;

/// <summary>
/// Internal state of a rectangle.
/// </summary>
public class SvgData {
    /// <summary>
    /// SVG code.
    /// </summary>
    public required string svg;
    /// <summary>
    /// internal handle.
    /// </summary>
    public int handle;
}

internal class SvgSpriteState : EntityState2D {
    public float width, height;
    public SvgData svg;
    public Color color = Color.WHITE;

    public SvgSpriteState(SvgData svg, float width, float height){
        this.svg = svg;
        this.width = width;
        this.height = height;
    }

    public SvgSpriteState(SvgSpriteState sprite) : base(sprite) {
        this.width = sprite.width;
        this.height = sprite.height;
        this.svg = sprite.svg;
        this.color = sprite.color;
    }

    public override object Clone() {
        return new SvgSpriteState(this);
    }

    public override Vector2 AABB {
        get {
            return new Vector2(width, height);
        }
    }
}

/// <summary>
/// A SVG sprite entity.
/// </summary>
public class SvgSprite : VisualEntity2D, IColored {
    /// <summary>
    /// Creates a new SVG sprite given the SVG code and the width and height.
    /// </summary>
    /// <param name="svg">The SVG code.</param>
    /// <param name="width">The width of the sprite.</param>
    /// <param name="height">The height of the sprite.</param>
    public SvgSprite(SvgData svg, float width = -1.0f, float height = -1.0f) : base(new SvgSpriteState(svg, width, height)) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="sprite">The sprite to copy.</param>
    public SvgSprite(SvgSprite sprite) : base(sprite) {
    }

    /// <summary>
    /// The tint color of the sprite.
    /// </summary>
    public Color Color {
        get {
            return ((SvgSpriteState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((SvgSpriteState)state).color);
            ((SvgSpriteState)state).color = value;
        }
    }

    /// <summary>
    /// The SVG code of the sprite.
    /// </summary>
    public SvgData Svg {
        get {
            return ((SvgSpriteState)state).svg;
        }
        set {
            World.current.SetProperty(this, "Svg", value, ((SvgSpriteState)state).svg);
            ((SvgSpriteState)state).svg = value;
        }
    }

    /// <summary>
    /// The width of the sprite.
    /// </summary>
    public float Width {
        get {
            return ((SvgSpriteState)state).width;
        }
        set {
            World.current.SetProperty(this, "Width", value, ((SvgSpriteState)state).width);
            ((SvgSpriteState)state).width = value;
        }
    }

    /// <summary>
    /// The height of the sprite.
    /// </summary>
    public float Height {
        get {
            return ((SvgSpriteState)state).height;
        }
        set {
            World.current.SetProperty(this, "Height", value, ((SvgSpriteState)state).height);
            ((SvgSpriteState)state).height = value;
        }
    }

    /// <summary>
    /// Clone this sprite.
    /// </summary>
    public override object Clone() {
        return new SvgSprite(this);
    }
}
