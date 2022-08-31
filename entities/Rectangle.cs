
namespace AnimLib {
    public class RectangleState : ShapeState {
        public float width, height;

        public RectangleState(ShapePath path) : base(path) {
        }

        public RectangleState(RectangleState rs) : base(rs) {
            this.width = rs.width;
            this.height = rs.height;
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

    public class Rectangle : Shape, IColored {

        private static ShapePath CreateRectanglePath(float w, float h) {
            var pb = new PathBuilder();
            pb.Rectangle(new Vector2(-0.5f*w, -0.5f*h), new Vector2(0.5f*w, 0.5f*h));
            return pb;
        }

        public Rectangle(float w, float h) : base(new RectangleState(CreateRectanglePath(w, h))) {
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
