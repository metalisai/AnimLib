namespace AnimLib {
    public class SpriteState : EntityState2D {
        public float width, height;
        public Texture2D texture;

        public SpriteState(Texture2D texture, float width, float height){
            this.texture = texture;
            this.width = width;
            this.height = height;
        }

        public SpriteState(SpriteState sprite) : base(sprite) {
            this.width = sprite.width;
            this.height = sprite.height;
            this.texture = sprite.texture;
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

    public class Sprite : VisualEntity2D {
        public Sprite(Texture2D texture, float width, float height) : base(new SpriteState(texture, width, height)) {
        }

        public Sprite(Sprite sprite) : base(sprite) {
        }

        public Texture2D Texture {
            get {
                return ((SpriteState)state).texture;
            }
            set {
                World.current.SetProperty(this, "Texture", value, ((SpriteState)state).texture);
                ((SpriteState)state).texture = value;
            }
        }

        public float Width {
            get {
                return ((SpriteState)state).width;
            }
            set {
                World.current.SetProperty(this, "Width", value, ((SpriteState)state).width);
                ((SpriteState)state).width = value;
            }
        }

        public float Height {
            get {
                return ((SpriteState)state).height;
            }
            set {
                World.current.SetProperty(this, "Height", value, ((SpriteState)state).height);
                ((SpriteState)state).height = value;
            }
        }

        public override object Clone() {
            return new Sprite(this);
        }
    }
}
