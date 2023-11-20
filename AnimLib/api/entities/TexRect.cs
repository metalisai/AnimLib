/*
namespace AnimLib {
    public class TexRectState : RectangleState {
        public Texture2D texture;

        public TexRectState() {
        }

        public TexRectState(TexRectState trs) : base(trs) {
            this.texture = trs.texture;
        }

        public override object Clone() {
            return new TexRectState(this);
        }
    }

    public class TexRect : Rectangle {
        public Texture2D Texture {
            get {
                return ((TexRectState)state).texture;
            }
            set {
                World.current.SetProperty(this, "Texture", value, ((TexRectState)state).texture);
                ((TexRectState)state).texture = value;
            }
        }
        public TexRect() {
            state = new TexRectState();
        }
    }
}*/
