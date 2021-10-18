
namespace AnimLib 
{
    public class CubeState : EntityState
    {
        public Color color = Color.WHITE;

        public CubeState() {
        }

        public CubeState(CubeState c) : base(c) {
            this.color = c.color;
        }

        public override object Clone()
        {
            return new CubeState(this);
        }
    }
    public class Cube : VisualEntity/*, ICloneable*/ {
        public Color Color { 
            get {
                return ((CubeState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((CubeState)state).color);
                ((CubeState)state).color = value;
            }
        }

        public Cube() {
            state = new CubeState();
        }

        public Cube(Cube cube) : base(cube) {
        }

        public override object Clone() {
            return new Cube(this);
        }
    }
}
