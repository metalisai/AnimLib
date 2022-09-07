
namespace AnimLib {
    public abstract class CameraState : EntityState3D {
        public Color clearColor = Color.WHITE;

        public CameraState() {}

        public CameraState(CameraState cs) : base(cs) {
        }

        public abstract M4x4 CreateWorldToClipMatrix(float aspect);
        public abstract Ray RayFromClip(Vector2 clipPos, float aspect);
    }

    public abstract class Camera : VisualEntity3D {
        public Camera(EntityState state) : base(state) {}
        public Camera(Camera c) : base(c) {}

        public Color ClearColor {
            get {
                return ((CameraState)state).clearColor;
            }
            set {
                World.current.SetProperty(this, "clearColor", value, ((CameraState)state).clearColor);
                ((CameraState)state).clearColor = value;
            }
        }
    }
}
