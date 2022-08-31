namespace AnimLib
{
    public class Transform2D
    {
        protected VisualEntity2D entity;

        public Transform2D parent {
            get {
                return entity.state.parentId == 0 ? null : ((VisualEntity2D)World.current.EntityResolver.GetEntity(entity.state.parentId)).Transform;
            } set {
                World.current.SetProperty(entity, "parentId", value.entity.state.entityId, entity.state.parentId);
                entity.state.parentId = value.entity.state.entityId;
            }
        }
        public Vector2 Pos {
            get {
                return entity.state.position;
            } set {
                World.current.SetProperty(entity, "position", value, entity.state.position);
                entity.state.position = value;
            }
        }
        public float Rot {
            get {
                return entity.state.rot;
            }
            set {
                World.current.SetProperty(entity, "rot", value, entity.state.rot);
                entity.state.rot = value;
            }
        }
        public Vector2 Scale {
            get {
                return entity.state.scale;
            }
            set {
                World.current.SetProperty(entity, "scale", value, entity.state.scale);
                entity.state.scale = value;
            }
        }
        public Transform2D(VisualEntity2D entity, Vector2 pos, float rot) {
            this.entity = entity;
            this.Pos = pos;
            this.Rot = rot;
            this.Scale = Vector2.ONE;
        }
        public Transform2D(VisualEntity2D entity, Vector2 pos, float rot, Vector2 scale) {
            this.entity = entity;
            this.Pos = pos;
            this.Rot = rot;
            this.Scale = scale;
        }
        public Transform2D(Transform2D t) {
            this.entity = t.entity;
            this.Pos = t.Pos;
            this.Rot = t.Rot;
            this.Scale = t.Scale;
        }
        public Transform2D(VisualEntity2D entity) {
            this.entity = entity;
        }
    }
}
