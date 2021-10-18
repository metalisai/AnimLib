using System.Linq;
using System.Text.Json.Serialization;

namespace AnimLib {
    public class PlayerQSpline : SceneObject {
        [ShowInEditor]
        public float width { get; set; }
        [ShowInEditor]
        public Color color { get; set; }
        public Vector3[] points { get; set; }
        [JsonIgnore]
        public override bool Is2D { get { return false; } }
        public override Vector3[] GetHandles2D() {
            return points.Select(x => x + transform.Pos).ToArray();
        }
        public override Plane? GetSurface() {
            var norm = this.transform.Rot * -Vector3.FORWARD;
            var ret = new Plane {
                n = norm,
                // TODO: is this right?
                o = -Vector3.Dot(transform.Pos, norm),
            };
            return ret;

        }
        public override void SetHandle(int id, Vector3 wpos) {
            if(id >= points.Length || id < 0)
                return;
            points[id] = wpos - transform.Pos;
        }

        public override object Clone() {
            return new PlayerQSpline() {
                timeslice = timeslice,
                color = color,
                points = points,
                width = width,
                transform = new SceneTransform(transform.Pos, transform.Rot)
            };
        }
    }
}
