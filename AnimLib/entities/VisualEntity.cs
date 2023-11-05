using System;

namespace AnimLib {

    [Flags]
    internal enum VisualEntityFlags {
        None = 0,
        Created = 1,
        ManagedLifetime = 2,
    }

    internal class EntityStateResolver {
        public Func<int, EntityState> GetEntityState;
    }

    internal abstract class EntityState : ICloneable {
        // TODO: find way to reference state without VisualEntity
        public int parentId = 0;
        public object creator; // AnimationBehaviour or SceneObject
        public bool active = true;
        public bool selectable = true;
        public int entityId = -1;
        public RendererAnimation anim = null;
        public abstract object Clone();

        public EntityState() {
        }

        public EntityState(EntityState ent) {
            this.parentId = ent.parentId;
            this.active = ent.active;
            this.selectable = ent.selectable;
            this.entityId = ent.entityId;
        }
    }

    public abstract class VisualEntity : ICloneable {
        // NOTE: this only contains valid data during animation baking (user code)
        internal EntityState state;
        internal VisualEntityFlags flags = VisualEntityFlags.None;

        private protected bool GetFlag(VisualEntityFlags flag) {
            return (flags & flag) != 0;
        }

        private protected void SetFlag(VisualEntityFlags flag, bool value) {
            if(value) {
                flags |= flag;
            } else {
                flags &= ~flag;
            }
        }

        public bool created {
            get {
                return GetFlag(VisualEntityFlags.Created);
            }
            internal set {
                SetFlag(VisualEntityFlags.Created, value);
            }
        }

        public bool managedLifetime {
            get {
                return GetFlag(VisualEntityFlags.ManagedLifetime);
            }
            internal set {
                SetFlag(VisualEntityFlags.ManagedLifetime, value);
            }
        }

        public VisualEntity(VisualEntity ent){
            this.state = (EntityState)ent.state.Clone();
            this.state.entityId = -1;
        }

        internal VisualEntity(EntityState state) {
            this.state = state;
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

}
