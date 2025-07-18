using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Glyph))]
internal class GlyphState : EntityState2D
{
    [Dyn]
    public char character;
    [Dyn]
    public float size;
    [Dyn]
    public Color color = Color.BLACK;

    public GlyphState() { }

    public GlyphState(GlyphState g) : base(g)
    {
        this.character = g.character;
        this.size = g.size;
        this.color = g.color;
    }

    public override Vector2 AABB
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}

/// <summary>
/// A (cached) glyph that's drawn from a font atlas.
/// </summary>
public partial class Glyph : VisualEntity2D {
    /// <summary>
    /// Creates a new glyph with given width and height.
    /// </summary>
    public Glyph(char character, float size) : base()
    {
        this._characterP.Value = character;
        this._sizeP.Value = size;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new GlyphState();
        GetState(state, evaluator);
        return state;
    }
}