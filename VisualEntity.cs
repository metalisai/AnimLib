using System;

namespace AnimLib {

    public class EntityStateResolver {
        public Func<int, EntityState> GetEntityState;
    }

    public abstract class EntityState : ICloneable {
        // TODO: find way to reference state without VisualEntity
        public int parentId;
        public object creator; // AnimationBehaviour or SceneObject
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

    public enum Entity2DCoordinateSystem {
        CanvasOrientedWorld,
        CanvasNormalized,
    };

    public abstract class EntityState2D : EntityState {
        public int canvasId = -1; // entity Id of canvas
        public Vector2 pivot;
        public float rot;
        // NOTE: pivot and anchor always use CanvasNormalized coordinates
        public Entity2DCoordinateSystem csystem = Entity2DCoordinateSystem.CanvasOrientedWorld;

        internal CanvasState canvas; // resolved by WorldMachine before redering

        public EntityState2D() {}

        public EntityState2D(EntityState2D e2d) : base(e2d) {
            this.rot = e2d.rot;
            this.pivot = e2d.pivot;
            this.canvasId = e2d.canvasId;
            this.csystem = e2d.csystem;
        }

        // normalized coordinates -0.5..0.5
        internal M4x4 NormalizedCanvasToWorld {
            get {
                // TODO: cache
                var anchorWorld = canvas.NormalizedCanvasToWorld*new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
                var c1 = new Vector4(canvas.width*Vector3.Cross(canvas.normal, canvas.up), 0.0f);
                var c2 = new Vector4(canvas.height*canvas.up, 0.0f);
                var c3 = new Vector4(-canvas.normal, 0.0f);
                var mat = M4x4.FromColumns(c1, c2, c3, anchorWorld);
                return mat;
            }
        }

        // oriented world coordinates (x - left, y - up, z - forward)
        internal M4x4 CanvasToWorld {
            get {
                // TODO: cache
                var anchorWorld = canvas.NormalizedCanvasToWorld*new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
                var c1 = new Vector4(Vector3.Cross(canvas.normal, canvas.up), 0.0f);
                var c2 = new Vector4(canvas.up, 0.0f);
                var c3 = new Vector4(-canvas.normal, 0.0f);
                return M4x4.FromColumns(c1, c2, c3, anchorWorld);
            }
        }

        // axis aligned bounding box required for normalized coordinates
        public abstract Vector2 AABB { get; }
    }

    public abstract class Visual2DEntity : VisualEntity {
        private Canvas _canvas;
        public Visual2DEntity(EntityState2D state) {
            this.state = state;
            Canvas = Canvas.Default;
        }
        public Visual2DEntity(Visual2DEntity e) : base(e) {
            Canvas = e.Canvas;
        }

        public Canvas Canvas 
        {
            get {
                return _canvas ?? Canvas.Default;
            }
            set {
                World.current.SetProperty(this, "canvasId", value.EntityId, Canvas.EntityId);
                ((EntityState2D)state).canvasId = value.EntityId;
                _canvas = value;
            }
        }
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
        public Vector2 Pivot
        {
            get {
                return ((EntityState2D)state).pivot;
            }
            set {
                World.current.SetProperty(this, "Pivot", value, ((EntityState2D)state).pivot);
                ((EntityState2D)state).pivot = value;
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

        public Entity2DCoordinateSystem CSystem {
            get {
                return ((EntityState2D)state).csystem;
            }
            set {
                World.current.SetProperty(this, "CSystem", value, ((EntityState2D)state).csystem);
                ((EntityState2D)state).csystem = value;
            }
        }
    }
}
