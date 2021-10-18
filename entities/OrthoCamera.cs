
namespace AnimLib {
    public class OrthoCameraState : CameraState {
        public float width;
        public float height;

        public OrthoCameraState() {}

        public OrthoCameraState(OrthoCameraState ocs) : base(ocs) {
            this.width = ocs.width;
            this.height = ocs.height;
        }

        public override object Clone()
        {
            return new OrthoCameraState(this);
        }

        public override M4x4 CreateWorldToClipMatrix(float aspect) {
            float hw = width/2.0f;
            float hh = height/2.0f;
            return M4x4.Ortho(0, width, 0, height, -1.0f, 1.0f);
        }
    }

    public class OrthoCamera : Camera {
        //public float /*left, right, bottom, top, front, back*/;
        
        public OrthoCamera() {}

        public OrthoCamera(OrthoCamera oc) : base(oc) {}

        public float Width {
            get {
                return ((OrthoCameraState)state).width;
            } set {
                World.current.SetProperty(this, "Width", value, ((OrthoCameraState)state).width);
                ((OrthoCameraState)state).width = value;
            }
        }
        public float Height {
            get {
                return ((OrthoCameraState)state).height;
            } set {
                World.current.SetProperty(this, "Height", value, ((OrthoCameraState)state).height);
                ((OrthoCameraState)state).height = value;
            }
        }
        
        public override object Clone() {
            return new OrthoCamera(this);
        }
    }
}
