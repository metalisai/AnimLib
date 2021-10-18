
namespace AnimLib {
    public class CircleState : EntityState {
        public float radius;
        public Color color;
        public Color outline = Color.BLACK;
        public float outlineWidth = 1.0f;
        public bool is2d = false;
        public Vector2 sizeRect;

        public CircleState() {}

        public CircleState(CircleState cs) : base(cs) {
            this.radius = cs.radius;
            this.color = cs.color;
            this.outline = cs.outline;
            this.outlineWidth = cs.outlineWidth;
            this.is2d = cs.is2d;
            this.sizeRect = cs.sizeRect;
        }

        public override object Clone() {
            return new CircleState(this);
        }
    }

    public class Circle : VisualEntity, IColored {
        public Circle() {
            state = new CircleState();
            Transform = new Transform(this);
        }
        public Circle(bool is2d) {
            state = new CircleState();
            if(is2d) {
                Transform = new RectTransform(this);
                ((CircleState)state).is2d = true;
            }
        }
        public Circle(Circle c) : base(c) {
        }
        public float Radius { 
            get {
                return ((CircleState)state).radius;
            }
            set {
                World.current.SetProperty(this, "Radius", value, ((CircleState)state).radius);
                ((CircleState)state).radius = value;
            }
        }
        public Color Color {
            get {
                return ((CircleState)state).color;
            } 
            set {
                World.current.SetProperty(this, "Color", value, ((CircleState)state).color);
                ((CircleState)state).color = value;
            }
        }
        public Color Outline {
            get {
                return ((CircleState)state).outline;
            }
            set {
                World.current.SetProperty(this, "Outline", value, ((CircleState)state).outline);
                ((CircleState)state).outline = value;
            }
        }

        public float OutlineWidth {
            get {
                return ((CircleState)state).outlineWidth;
            }
            set {
                World.current.SetProperty(this, "OutlineWidth", value, ((CircleState)state).outlineWidth);
                ((CircleState)state).outlineWidth = value;
            }
        }

        public Circle Pos(Vector3 p) {
            this.Transform.Pos = p;
            return this;
        }

        public Circle Rot(Quaternion quaternion) {
            Transform.Rot = quaternion;
            return this;
        }

        public Circle R(float r){
            this.Radius = r;
            return this;
        }

        public Circle C(Color color){ 
            this.Color = color;
            return this;
        }
        public override object Clone() {
            return new Circle(this);
        }
    }
}
