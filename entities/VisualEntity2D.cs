namespace AnimLib {

    public enum Entity2DCoordinateSystem {
        CanvasOrientedWorld,
        CanvasNormalized,
    };

    public abstract class EntityState2D : EntityState {
        public int canvasId = -1; // entity Id of canvas
        public Vector2 position = Vector2.ZERO;
        public float rot = 0.0f;
        public Vector2 anchor = Vector2.ZERO;
        public Vector2 pivot = Vector2.ZERO;
        public Vector2 scale = Vector2.ONE;
        // NOTE: pivot and anchor always use CanvasNormalized coordinates
        public Entity2DCoordinateSystem csystem = Entity2DCoordinateSystem.CanvasOrientedWorld;

        internal CanvasState canvas; // resolved by WorldMachine before redering

        public EntityState2D() {}

        public EntityState2D(EntityState2D e2d) : base(e2d) {
            this.canvasId = e2d.canvasId;
            this.position = e2d.position;
            this.rot = e2d.rot;
            this.anchor = e2d.anchor;
            this.pivot = e2d.pivot;
            this.scale = e2d.scale;
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

        // TODO: this doesn't belong here
        public M4x4 ModelToWorld(EntityStateResolver resolver) {
            if(parentId == 0) {
                return M4x4.TRS(position, Quaternion.IDENTITY, scale);
            } else { 
                var parent = (EntityState2D)resolver.GetEntityState(parentId);
                if(parent == null) {
                    Debug.Error($"Entity {this} did not find parent {parentId}");
                    return M4x4.IDENTITY;
                }
                return parent.ModelToWorld(resolver) * M4x4.TRS(position, Quaternion.IDENTITY, scale);
            }
        }

        // axis aligned bounding box required for normalized coordinates
        public abstract Vector2 AABB { get; }
    }

    public abstract class VisualEntity2D : VisualEntity {
        private Canvas _canvas;
        public Transform2D Transform;

        public VisualEntity2D(EntityState2D state) : base(state) {
            Transform = new Transform2D(this);
            Canvas = Canvas.Default;
        }
        public VisualEntity2D(VisualEntity2D e) : base(e) {
            Transform = new Transform2D(this);
            Canvas = e.Canvas;
        }

        internal int CanvasId
        {
            get {
                return _canvas.EntityId;
            }
            set {
                World.current.SetProperty(this, "canvasId", value, Canvas.EntityId);
                ((EntityState2D)state).canvasId = value;
            }
        }

        public new EntityState2D state {
            get {
                return base.state as EntityState2D;
            }
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
