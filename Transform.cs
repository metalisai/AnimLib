namespace AnimLib
{
    public class Transform
    {
        public enum CoordinateSpace {
            Camera,
            Screen,
        }
        protected VisualEntity entity;

        public Transform parent {
            get {
                return entity.state.parentId == 0 ? null : World.current.EntityResolver.GetEntity(entity.state.parentId).Transform;
            } set {
                World.current.SetProperty(entity, "parentId", value.entity.state.entityId, entity.state.parentId);
                entity.state.parentId = value.entity.state.entityId;
            }
        }
        public virtual Vector3 WorldPos {
            get {
                // TODO
                return Pos;
            }
        }
        public Vector3 Pos {
            get {
                return entity.state.position;
            } set {
                World.current.SetProperty(entity, "position", value, entity.state.position);
                entity.state.position = value;
            }
        }
        public Quaternion Rot {
            get {
                return entity.state.rotation;
            }
            set {
                World.current.SetProperty(entity, "rotation", value, entity.state.rotation);
                entity.state.rotation = value;
            }
        }
        public Vector3 Scale {
            get {
                return entity.state.scale;
            }
            set {
                World.current.SetProperty(entity, "scale", value, entity.state.scale);
                entity.state.scale = value;
            }
        }
        public Transform(VisualEntity entity, Vector3 pos, Quaternion rot) {
            this.entity = entity;
            this.Pos = pos;
            this.Rot = rot;
            this.Scale = Vector3.ONE;
        }
        public Transform(VisualEntity entity, Vector3 pos, Quaternion rot, Vector3 scale) {
            this.entity = entity;
            this.Pos = pos;
            this.Rot = rot;
            this.Scale = scale;
        }
        public Transform(Transform t) {
            this.entity = t.entity;
            this.Pos = t.Pos;
            this.Rot = t.Rot;
            this.Scale = t.Scale;
        }
        public Transform(VisualEntity entity) {
            this.entity = entity;
        }
    }

    public class RectTransform : Transform {
        public static RectTransform RootTransform = new RectTransform(new Dummy());
        public Vector2 Size;
        public Vector2 Anchor;
        public RectTransform(RectTransform rect) : base(rect.entity) {
            this.Pos = rect.Pos;
            this.Rot = rect.Rot;
            this.Scale = rect.Scale;
            this.Size = rect.Size;
            this.Anchor = rect.Anchor;
            this.parent = rect.parent;
        }
        public RectTransform(VisualEntity ent) : base(ent) {
        }
        public override Vector3 WorldPos {
            get {
                float x,y;
                if(parent != null) {
                    x = ((RectTransform)parent).Size.x*Anchor.x;
                    y = ((RectTransform)parent).Size.y*Anchor.y;
                } else {
                    x = RootTransform.Size.x*Anchor.x;
                    y = RootTransform.Size.y*Anchor.y;
                }
                var p = new Vector3(x, y, 0.0f);
                return Pos+p;
            }
        }
    }
}
