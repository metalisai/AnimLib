using System.Text.Json.Serialization;

namespace AnimLib {
    public class Player2DText : SceneObject {
        [ShowInEditor]
        public float size {get; set;}
        [ShowInEditor]
        public Color color{get; set;}
        [ShowInEditor]
        public string text {get; set;}
        [JsonIgnore]
        public override bool Is2D { get { return true; } }

        public override object Clone()
        {
            return new Player2DText() {
                timeslice = timeslice,
                transform = new SceneTransform(transform.Pos, transform.Rot),
                size = size,
                color = color,
                text = text,
            };
        }

        public override Vector3[] GetHandles2D()
        {
            return new Vector3[0];
        }

        public override Plane? GetSurface()
        {
            return new Plane() {
                o = 0.0f,
                n = new Vector3(0.0f, 0.0f, -1.0f),
            };
        }

        public override void SetHandle(int id, Vector3 wpos)
        {
        }
    }
}
