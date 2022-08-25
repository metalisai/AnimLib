using System;

namespace AnimLib {
    public class ShapeState : EntityState2D {
        public Vector2[] path;
        public Color color;

        public ShapeState() {
        }

        public ShapeState(ShapeState ss) : base(ss) {
            this.path = ss.path.Clone() as Vector2[];
            this.color = ss.color;
        }

        public override Vector2 AABB {
            get {
                throw new NotImplementedException();
            }
        }

        public override object Clone() {
            return new ShapeState(this);
        }
    }

    public class Shape : Visual2DEntity, IColored {
        public Shape() : base(new ShapeState()) {
        }

        public Shape(Shape s) : base(s) {
        }

        public Vector2[] Path {
            get {
                return ((ShapeState)state).path;
            }
            set {
                World.current.SetProperty(this, "Path", value, ((ShapeState)state).path);
                ((ShapeState)state).path = value;
            }
        }
        public Color Color {
            get {
                return ((ShapeState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((ShapeState)state).color);
                ((ShapeState)state).color = value;
            }
        }

        public override object Clone() {
            return new Shape(this);
        }
    }
}
