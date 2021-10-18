using System;
using System.Text.Json.Serialization;

namespace AnimLib {
    [Serializable]
    public class PlayerCircle : SceneObject {
        [ShowInEditor]
        public float radius { get; set; }
        [ShowInEditor]
        public Color color { get; set; } = Color.RED;
        [JsonIgnore]
        public override bool Is2D { get { return false; } }

        public override Vector3[] GetHandles2D() {
            var handle = transform.Pos + transform.Rot*(radius*Vector3.RIGHT);
            return new Vector3[] {handle};
        }
        public override Plane? GetSurface() {
            var norm = this.transform.Rot * -Vector3.FORWARD;
            return new Plane {
                n = norm,
                // TODO: is this right?
                o = -Vector3.Dot(transform.Pos, norm),
            };
        }

        public override void SetHandle(int id, Vector3 wpos) {
            System.Diagnostics.Debug.Assert(id == 0); // circle only has one handle for radius
            float r = (wpos - transform.Pos).Length;
            this.radius = r;
        }

        public override object Clone() {
            return new PlayerCircle() {
                radius = radius,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                color = color,
                timeslice = timeslice,
            };
        }
    }
}
