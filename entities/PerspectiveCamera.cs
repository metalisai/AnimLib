
namespace AnimLib {
    public class PerspectiveCameraState : CameraState
    {
        public float fov = 60.0f;
        public float zNear = 0.1f;
        public float zFar = 1000.0f;

        public PerspectiveCameraState() {}

        public PerspectiveCameraState(PerspectiveCameraState pcs) : base(pcs) {
            this.fov = pcs.fov;
            this.zNear = pcs.zNear;
            this.zFar = pcs.zFar;
        }

        public override object Clone()
        {
            return new PerspectiveCameraState(this);
        }

        public M4x4 CreateWorldToViewMatrix() {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 worldToView = M4x4.RT(invRot, invPos);
            return worldToView;
        }

        public M4x4 CreateViewToClipMatrix(float aspect) {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 viewToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar);
            return viewToClip;
        }

        public override M4x4 CreateWorldToClipMatrix(float aspect) {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 worldToView = M4x4.RT(invRot, invPos);
            M4x4 worldToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar) * worldToView;
            return worldToClip;
        }

        public Vector3 WorldToClipPos(float aspect, Vector3 pos) {
            var mat = CreateWorldToClipMatrix(aspect);
            Vector4 v4 = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            var ret = mat * v4;
            return new Vector3(ret.x/ret.w, ret.y/ret.w, ret.z/ret.w);
        }

        public Vector2 WorldToNormScreenPos(Vector3 pos, float aspect) {
            var clip = WorldToClipPos(aspect, pos);
            return new Vector2((clip.x+1.0f)*0.5f, 1.0f - (clip.y+1.0f)*0.5f);
        }

        public Vector2 WorldToScreenPos(Vector3 pos, Vector2 screenSize) {
            var clip = WorldToClipPos(screenSize.x/screenSize.y, pos);
            return new Vector2((clip.x+1.0f)*0.5f*screenSize.x, screenSize.y - (clip.y+1.0f)*0.5f*screenSize.y);
        }   

        public M4x4 CreateClipToWorldMatrix(float aspect) {
            M4x4 clipToView = M4x4.InvPerspective(fov, aspect, this.zNear, this.zFar);
            M4x4 viewToWorld = M4x4.TRS(position, rotation, Vector3.ONE);
            return viewToWorld * clipToView;
        }

        public Ray RayFromClip(Vector2 clipPos, float aspect) {
            M4x4 mat = CreateClipToWorldMatrix(aspect);
            Vector4 dirw = mat * new Vector4(clipPos.x, clipPos.y, 1.0f, 1.0f);
            Vector3 pos = new Vector3(dirw.x / dirw.w, dirw.y / dirw.w, dirw.z / dirw.w);
            return new Ray() {
                o = position,
                d = (pos - position).Normalized,
            };
        }
    }

    public class PerspectiveCamera : Camera {
        public PerspectiveCamera() {
            this.state = new PerspectiveCameraState();
        }
        public PerspectiveCamera(PerspectiveCamera pc) : base(pc) {}
        public float Fov {
            get {
                return ((PerspectiveCameraState)state).fov;
            } set {
                World.current.SetProperty(this, "Fov", value, ((PerspectiveCameraState)state).fov);
                ((PerspectiveCameraState)state).fov = value;
            }
        }
        public float ZNear {
            get {
                return ((PerspectiveCameraState)state).zNear;
            } set {
                World.current.SetProperty(this, "ZNear", value, ((PerspectiveCameraState)state).zNear);
                ((PerspectiveCameraState)state).zNear = value;
            }
        }
        public float ZFar {
            get {
                return ((PerspectiveCameraState)state).zFar;
            } set {
                World.current.SetProperty(this, "ZFar", value, ((PerspectiveCameraState)state).zFar);
                ((PerspectiveCameraState)state).zFar = value;
            }
        }

        public Vector3 WorldToClipPos(float aspect, Vector3 pos) {
            return ((PerspectiveCameraState)state).WorldToClipPos(aspect, pos);
        }   

        public M4x4 CreateClipToWorldMatrix(float aspect) {
            return ((PerspectiveCameraState)state).CreateClipToWorldMatrix(aspect);
        }

        public Ray RayFromClip(Vector2 clipPos, float aspect) {
            return ((PerspectiveCameraState)state).RayFromClip(clipPos, aspect);
        }

        public override object Clone() {
            return new PerspectiveCamera(this);
        }
    }
}
