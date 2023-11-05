using System.Text.Json.Serialization;

namespace AnimLib {
    public class PlayerLine : SceneObject2D {
        [ShowInEditor]
        public float width {get; set; }
        [ShowInEditor]
        public Vector2 start {get ; set; }
        [ShowInEditor]
        public Vector2 end {get ; set; }
        [ShowInEditor]
        public Color color {get; set; } = Color.BLACK;
        public override Vector2[] GetHandles2D() {
            var h1 = transform.Pos + start;
            var h2 = transform.Pos + end;
            return new Vector2[] {h1, h2};
        }
        public override void SetHandle(int id, Vector2 wpos) {
            if(id == 0) {
                start = wpos - transform.Pos;
            } else {
                end = wpos - transform.Pos;
            }
        }

        public override object Clone() {
            return new PlayerLine() {
                timeslice = timeslice,
                color = color,
                start = start, 
                end = end, 
                width = width, 
                transform = new SceneTransform2D(transform.Pos, transform.Rot)
            };
        }

        public override bool Intersects(Vector2 point) {
#warning PlayerLine.Intersects is not implemented
            return false;
        }

        public override VisualEntity2D InitializeEntity() {
            return null;
        }
    }
}
