using System;

namespace AnimLib {
    [Serializable]
    public class SceneTransform {
        public Vector3 Pos {get; set; }
        public Quaternion Rot {get; set; }
        public SceneTransform() {
            
        }
        public SceneTransform(Vector3 p, Quaternion r) {
            this.Pos = p;
            this.Rot = r;
        }
    }
}
