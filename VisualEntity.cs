using System;

namespace AnimLib {

    public class EntityStateResolver {
        public Func<int, EntityState> GetEntityState;
    }

    public abstract class EntityState : ICloneable {
        // TODO: find way to reference state without VisualEntity
        public int parentId;
        public Vector3 position;
        public Vector2 anchor;
        public Quaternion rotation = Quaternion.IDENTITY;
        public Vector3 scale = Vector3.ONE;
        public bool active = true;
        public bool selectable = true;
        public int entityId = -1;
        public RendererAnimation anim = null;
        public abstract object Clone();

        public EntityState() {
        }

        public EntityState(EntityState ent) {
            this.parentId = ent.parentId;
            this.position = ent.position;
            this.anchor = ent.anchor;
            this.rotation = ent.rotation;
            this.scale = ent.scale;
            this.active = ent.active;
            this.selectable = ent.selectable;
            this.entityId = ent.entityId;
        }

        // TODO: cache
        public M4x4 ModelToWorld(EntityStateResolver resolver) {
            if(parentId == 0) {
                return M4x4.TRS(position, rotation, scale);
            } else { 
                var parent = resolver.GetEntityState(parentId);
                return parent.ModelToWorld(resolver) * M4x4.TRS(position, rotation, scale);
            }
        }
    }

    public abstract class VisualEntity : ICloneable {
        public Transform Transform;
        // NOTE: this only contains valid data during animation baking
        // (we need to store values for getters in user code)
        public EntityState state;
        public bool created = false;

        public VisualEntity(VisualEntity ent) : this() {
            this.state = (EntityState)ent.state.Clone();
            this.state.entityId = -1;
        }

        public VisualEntity() {
            Transform = new Transform(this);
        }

        public bool Active {
            get {
                return state.active;
            }
            set {
                World.current.SetProperty(this, "Active", value, state.active);
                state.active = value;
            }
        }
        public int EntityId {
            get {
                return state.entityId;
            } set {
                state.entityId = value;
            }
        }

        public void EntityCreated() {
            OnCreated();
        }

        protected virtual void OnCreated() {
        }

        public abstract object Clone();
    }

    public abstract class EntityState2D : EntityState {
        public Vector2 sizeRect;
        public float rot;

        public EntityState2D() {}

        public EntityState2D(EntityState2D e2d) : base(e2d) {
            this.sizeRect = e2d.sizeRect;
            this.rot = e2d.rot;
        }
    }

    public abstract class Visual2DEntity : VisualEntity {
        public Visual2DEntity() {}
        public Visual2DEntity(Visual2DEntity e) : base(e) {}
        public Vector2 Anchor
        {
            get {
                return ((EntityState2D)state).anchor;
            }
            set {
                World.current.SetProperty(this, "Anchor", value, ((EntityState2D)state).anchor);
                ((EntityState2D)state).anchor = value;
            }
        }
        public Vector2 SizeRect
        {
            get {
                return ((EntityState2D)state).sizeRect;
            }
            set {
                World.current.SetProperty(this, "SizeRect", value, ((EntityState2D)state).sizeRect);
                ((EntityState2D)state).sizeRect= value;
            }
        }
        public float Rot
        {
            get {
                return ((EntityState2D)state).rot;
            }
            set {
                World.current.SetProperty(this, "Rot", value, ((EntityState2D)state).rot);
                ((EntityState2D)state).rot = value;
            }
        }
    }
}
