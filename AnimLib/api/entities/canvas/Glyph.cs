using System;

namespace AnimLib;

internal class GlyphState : EntityState2D
{
    public char glyph;
    public float size;
    public Color color;

    public GlyphState() {}

    public GlyphState(GlyphState g) : base(g) {
        this.glyph = g.glyph;
        this.size = g.size;
        this.color = g.color;
    }

    public override object Clone()
    {
        return new GlyphState(this);
    }

    public override Vector2 AABB {
        get {
            throw new NotImplementedException();
        }
    }
}

/// <summary>
/// A (cached) glyph that's drawn from a font atlas.
/// Faster than drawing Shapes, but less flexible.
/// </summary>
public class Glyph : VisualEntity2D, IColored {
    /// <summary>
    /// The character this glyph represents.
    /// </summary>
    public char Character {
        get {
            return ((GlyphState)state).glyph;
        }
        set {
            World.current.SetProperty(this, "Character", value, ((GlyphState)state).glyph);
            ((GlyphState)state).glyph = value;
        }
    }

    /// <summary>
    /// The color of this glyph.
    /// </summary>
    public Color Color
    {
        get {
            return ((GlyphState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((GlyphState)state).color);
            ((GlyphState)state).color = value;
        }
    }

    /// <summary>
    /// The font size of this glyph.
    /// </summary>
    public float Size
    {
        get {
            return ((GlyphState)state).size;
        }
        set {
            World.current.SetProperty(this, "Size", value, ((GlyphState)state).size);
            ((GlyphState)state).size = value;
        }
    }

    /// <summary>
    /// Creates a new Glyph.
    /// </summary>
    public Glyph() : base(new GlyphState()) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// /// <param name="g">The Glyph to copy.</param>
    public Glyph(Glyph g) : base(g) {
    }

    /// <summary>
    /// Clone this Glyph.
    /// </summary>
    public override object Clone() {
        return new Glyph(this);
    }
}
