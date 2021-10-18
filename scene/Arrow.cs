using System;
using System.Text.Json.Serialization;

namespace AnimLib {
    [Serializable]
    public class PlayerArrow : SceneObject {
        [ShowInEditor]
        public float width {get; set; }
        [ShowInEditor]
        public Vector3 start {get ; set; }
        [ShowInEditor]
        public Vector3 end {get ; set; }
        [ShowInEditor]
        public Color startColor {get; set; } = Color.BLACK;
        [ShowInEditor]
        public Color endColor {get; set; } = Color.BLACK;
        [JsonIgnore]
        public override bool Is2D { get { return false; } }
        public override Vector3[] GetHandles2D() {
            var h1 = transform.Pos + start;
            var h2 = transform.Pos + end;
            return new Vector3[] {h1, h2};
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
            if(id == 0) {
                start = wpos - transform.Pos;
            } else {
                end = wpos - transform.Pos;
            }
        }

        public override object Clone() {
            return new PlayerArrow() {
                timeslice = timeslice,
                width = width,
                start = start, 
                end = end,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                startColor = startColor,
                endColor = endColor,
            };
        }
    }
}
