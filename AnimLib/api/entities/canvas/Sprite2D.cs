namespace AnimLib;

internal class SpriteState : EntityState2D {
    public float width, height;
    public Texture2D texture;
    public Color color = Color.WHITE;

    public SpriteState(Texture2D texture, float width, float height){
        this.texture = texture;
        this.width = width;
        this.height = height;
    }

    public SpriteState(SpriteState sprite) : base(sprite) {
        this.width = sprite.width;
        this.height = sprite.height;
        this.texture = sprite.texture;
        this.color = sprite.color;
    }

    public override object Clone() {
        return new SpriteState(this);
    }

    public override Vector2 AABB {
        get {
            return new Vector2(width, height);
        }
    }
}

/// <summary>
/// A 2D sprite. Displays a bitmap image.
/// </summary>
public class Sprite : VisualEntity2D, IColored {
    /// <summary>
    /// Create a new sprite.
    /// </summary>
    /// <param name="texture">The texture of the sprite.</param>
    /// <param name="width">The width of the sprite.</param>
    /// <param name="height">The height of the sprite.</param>
    public Sprite(Texture2D texture, float width, float height) : base(new SpriteState(texture, width, height)) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="sprite">The sprite to copy.</param>
    public Sprite(Sprite sprite) : base(sprite) {
    }

    /// <summary>
    /// Tint color of the sprite.
    /// </summary>
    public Color Color {
        get {
            return ((SpriteState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((SpriteState)state).color);
            ((SpriteState)state).color = value;
        }
    }

    /// <summary>
    /// The texture of the sprite.
    /// </summary>
    public Texture2D Texture {
        get {
            return ((SpriteState)state).texture;
        }
        set {
            World.current.SetProperty(this, "Texture", value, ((SpriteState)state).texture);
            ((SpriteState)state).texture = value;
        }
    }

    /// <summary>
    /// The width of the sprite.
    /// </summary>
    public float Width {
        get {
            return ((SpriteState)state).width;
        }
        set {
            World.current.SetProperty(this, "Width", value, ((SpriteState)state).width);
            ((SpriteState)state).width = value;
        }
    }

    /// <summary>
    /// The height of the sprite.
    /// </summary>
    public float Height {
        get {
            return ((SpriteState)state).height;
        }
        set {
            World.current.SetProperty(this, "Height", value, ((SpriteState)state).height);
            ((SpriteState)state).height = value;
        }
    }

    /// <summary>
    /// Clone this sprite.
    /// </summary>
    public override object Clone() {
        return new Sprite(this);
    }
}
