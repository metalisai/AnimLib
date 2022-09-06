using System.Text.Json.Serialization;

namespace AnimLib {
    public class Player2DText : SceneObject2D {
        [ShowInEditor]
        public float size {get; set;}
        [ShowInEditor]
        public Color color{get; set;}
        [ShowInEditor]
        public string text {get; set;}

        public override object Clone()
        {
            return new Player2DText() {
                timeslice = timeslice,
                transform = new SceneTransform2D(transform.Pos, transform.Rot),
                size = size,
                color = color,
                text = text,
            };
        }

        public override Vector2[] GetHandles2D()
        {
            return new Vector2[0];
        }


        public override void SetHandle(int id, Vector2 wpos)
        {
        }

        public override bool Intersects(Vector2 point) {
#warning Player2DText.Intersects is not implemented
            return false;
        }

        public override VisualEntity2D InitializeEntity() {
            return null;
        }
    }
}
