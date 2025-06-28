using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Sprite))]
internal class SpriteState : EntityState2D
{
    [Dyn]
    public float width;
    [Dyn]
    public float height;
    [Dyn]
    public Texture2D texture;
    [Dyn]
    public Color color = Color.WHITE;

    public SpriteState(Texture2D texture, float width, float height)
    {
        this.texture = texture;
        this.width = width;
        this.height = height;
    }

    public SpriteState(SpriteState sprite) : base(sprite)
    {
        this.width = sprite.width;
        this.height = sprite.height;
        this.texture = sprite.texture;
        this.color = sprite.color;
    }

    public override object Clone()
    {
        return new SpriteState(this);
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
/// A 2D sprite. Displays a bitmap image.
/// </summary>
public partial class Sprite : DynVisualEntity2D
{
    // <summary>
    /// Create a new sprite.
    /// </summary>
    /// <param name="texture">The texture of the sprite.</param>
    /// <param name="width">The width of the sprite.</param>
    /// <param name="height">The height of the sprite.</param>
    public Sprite(Texture2D texture, float width, float height)
    {
        _textureP.Value = texture;
        _widthP.Value = width;
        _heightP.Value = height;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new SpriteState(_textureP.Value, _widthP.Value, _heightP.Value);
        GetState(state, evaluator);
        return state;
    }
}