
namespace AnimLib {
    public class RectangleState : EntityState2D {
        public float width, height;
        public Color color;
        public Color outline = Color.BLACK;
        public float outlineWidth = 1.0f;

        public RectangleState() {
        }

        public RectangleState(RectangleState rs) : base(rs) {
            this.width = rs.width;
            this.height = rs.height;
            this.color = rs.color;
            this.outline = rs.outline;
            this.outlineWidth = rs.outlineWidth;
        }

        public override Vector2 AABB {
            get {
                return new Vector2(width, height);
            }
        }

        public override object Clone()
        {
            return new RectangleState(this);
        }
    }

    public class Rectangle : Visual2DEntity, IColored {

        public Rectangle() : base(new RectangleState()) {
        }

        public Rectangle(Rectangle r) : base(r) {
        }

        public float Width {
            get {
                return ((RectangleState)state).width;
            }
            set {
                World.current.SetProperty(this, "Width", value, ((RectangleState)state).width);
                ((RectangleState)state).width = value;
            }
        }
        public float Height {
            get {
                return ((RectangleState)state).height;
            }
            set {
                World.current.SetProperty(this, "Height", value, ((RectangleState)state).height);
                ((RectangleState)state).height = value;
            }
        }
        public Color Color {
            get {
                return ((RectangleState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((RectangleState)state).color);
                ((RectangleState)state).color = value;
            }
        }
        public Color Outline {
            get {
                return ((RectangleState)state).outline;
            }
            set {
                World.current.SetProperty(this, "Outline", value, ((RectangleState)state).outline);
                ((RectangleState)state).outline = value;
            }
        }
        public float OutlineWidth {
            get {
                return ((RectangleState)state).outlineWidth;
            }
            set {
                World.current.SetProperty(this, "OutlineWidth", value, ((RectangleState)state).outlineWidth);
                ((RectangleState)state).outlineWidth = value;
            }
        }


        public Rectangle Pos(Vector3 pos) {
            Transform.Pos = pos;
            return this;
        }

        public Rectangle W(float width) {
            this.Width =width;
            return this;
        }

        public Rectangle H(float height) {
            this.Height = height;
            return this;
        }

        public Rectangle C(Color color) {
            this.Color = color;
            return this;
        }

        public override object Clone() {
            return new Rectangle(this);
        }

    }
}
