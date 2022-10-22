namespace AnimLib {

    public class SvgData {
        public string svg;
        public int handle;
    }

    public class SvgSpriteState : EntityState2D {
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

    public class SvgSprite : VisualEntity2D, IColored {
        public SvgSprite(SvgData svg, float width, float height) : base(new SvgSpriteState(svg, width, height)) {
        }

        public SvgSprite(SvgSprite sprite) : base(sprite) {
        }

        public Color Color {
            get {
                return ((SvgSpriteState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((SvgSpriteState)state).color);
                ((SvgSpriteState)state).color = value;
            }
        }

        public SvgData Svg {
            get {
                return ((SvgSpriteState)state).svg;
            }
            set {
                World.current.SetProperty(this, "Svg", value, ((SvgSpriteState)state).svg);
                ((SvgSpriteState)state).svg = value;
            }
        }

        public float Width {
            get {
                return ((SvgSpriteState)state).width;
            }
            set {
                World.current.SetProperty(this, "Width", value, ((SvgSpriteState)state).width);
                ((SvgSpriteState)state).width = value;
            }
        }

        public float Height {
            get {
                return ((SvgSpriteState)state).height;
            }
            set {
                World.current.SetProperty(this, "Height", value, ((SvgSpriteState)state).height);
                ((SvgSpriteState)state).height = value;
            }
        }

        public override object Clone() {
            return new SvgSprite(this);
        }
    }
}
