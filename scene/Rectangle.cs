using System;
using System.Text.Json.Serialization;

namespace AnimLib {
    [Serializable]
    public class PlayerRect : SceneObject2D {
        [ShowInEditor]
        public Vector2 size { get; set; }
        [ShowInEditor]
        public Color color { get; set; } = Color.BLUE;
        [ShowInEditor]
        public Color outline { get; set; } = Color.BLACK;
        [ShowInEditor]
        public float outlineWidth { get; set; } = 0.0f;
        [ShowInEditor]
        public ShapeMode mode { get; set; } = ShapeMode.FilledContour;

        protected PlayerRect(PlayerRect r) : base(r) {
            this.size = r.size;
            this.color = r.color;
        }

        [JsonConstructor]
        public PlayerRect(Vector2 size, string canvasName) : base() {
            this.size = size;
            this.CanvasName = canvasName;
        }

        public PlayerRect(float w, float h, string canvasName) : base() {
            this.size = new Vector2(w, h);
            this.CanvasName = canvasName;
        }

        public override Vector2[] GetHandles2D() {
            /*var handle = transform.Pos + transform.Rot*(radius*Vector3.RIGHT);
            return new Vector3[] {handle};*/
            return new Vector2[0];
        }

        public override bool Intersects(Vector2 point) {
            var pivot = transform.Pos;
            var pivotToPoint = point - pivot;
            // undo rotation, so the rectangle is axis aligned
            // use mathematical modulus (c# % operator is a remainder)
            var withoutRotation = pivotToPoint.Rotated(-transform.Rot);
            // put back to world space
            var halfSize = 0.5f*size;
            var min = -halfSize;
            var max = halfSize;
            return (withoutRotation.x >= min.x && withoutRotation.x <= max.x 
                    && withoutRotation.y >= min.y && withoutRotation.y <= max.y);
        }

        public override void SetHandle(int id, Vector2 wpos) {
            /*System.Diagnostics.Debug.Assert(id == 0); // circle only has one handle for radius
            float r = (wpos - transform.Pos).Length;
            this.radius = r;*/
        }

        public override object Clone() {
            return new PlayerRect(this);
        }

        public override VisualEntity2D InitializeEntity() {
            var ent = new Rectangle(this.size.x, this.size.y);
            ent.Color = this.color;
            ent.ContourColor = this.outline;
            ent.ContourSize = this.outlineWidth;
            ent.Mode = this.mode;
            ent.Transform.Pos = transform.Pos;
            ent.Transform.Rot = transform.Rot;
            return ent;
        }
    }
}
