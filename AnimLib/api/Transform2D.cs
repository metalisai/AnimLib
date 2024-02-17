namespace AnimLib;

/// <summary>
/// A 2D transform for 2D entities on a canvas.
/// </summary>
public class Transform2D {
    /// <summary>
    /// The entity this transform belongs to.
    /// </summary>
    protected VisualEntity2D entity;
    internal Transform2D? _parent;

    /// <summary>
    /// The parent transform.
    /// </summary>
    public Transform2D? parent {
        get {
            if (entity.state.parentId == 0) {
                return null;
            }
            var e = World.current.EntityResolver.GetEntity(entity.state.parentId);
            return ((VisualEntity2D)e)?.Transform;
        } set {
            _parent = value;
            if (value != null) {
                World.current.SetProperty(entity, "parentId", value.entity.state.entityId, entity.state.parentId);
                entity.state.parentId = value.entity.state.entityId;
            }
        }
    }

    /// <summary>
    /// The position of the entity relative to the anchor point.
    /// </summary>
    public Vector2 Pos {
        get {
            return entity.state.position;
        } set {
            World.current.SetProperty(entity, "position", value, entity.state.position);
            entity.state.position = value;
        }
    }

    /// <summary>
    /// The rotation of the entity in radians.
    /// </summary>
    public float Rot {
        get {
            return entity.state.rot;
        }
        set {
            World.current.SetProperty(entity, "rot", value, entity.state.rot);
            entity.state.rot = value;
        }
    }

    /// <summary>
    /// The homography/perspective transform relative to the parent.
    /// </summary>
    public M3x3? Homography {
        get {
            return entity.state.homography;
        }
        set {
            World.current.SetProperty(entity, "homography", value, entity.state.homography);
            entity.state.homography = value;
        }
    }

    /// <summary>
    /// The scale of the entity.
    /// </summary>
    public Vector2 Scale {
        get {
            return entity.state.scale;
        }
        set {
            World.current.SetProperty(entity, "scale", value, entity.state.scale);
            entity.state.scale = value;
        }
    }

    /// <summary>
    /// The anchor on the parent entity.
    /// </summary>
    public Vector2 Anchor
    {
        get {
            return entity.state.anchor;
        }
        set {
            World.current.SetProperty(entity, "Anchor", value, entity.state.anchor);
            entity.state.anchor = value;
        }
    }

    /// <summary>
    /// Create a new transform for the given entity.
    /// </summary>
    public Transform2D(VisualEntity2D entity, Vector2 pos, float rot) {
        this.entity = entity;
        this.Pos = pos;
        this.Rot = rot;
        this.Scale = Vector2.ONE;
    }

    /// <summary>
    /// Create a new transform for the given entity.
    /// </summary>
    public Transform2D(VisualEntity2D entity, Vector2 pos, float rot, Vector2 scale) {
        this.entity = entity;
        this.Pos = pos;
        this.Rot = rot;
        this.Scale = scale;
    }

    /// <summary>
    /// Copy constructor for a different entity.
    /// </summary>
    public Transform2D(Transform2D t, VisualEntity2D entity) {
        this.entity = entity;
        this.Pos = t.Pos;
        this.Rot = t.Rot;
        this.Scale = t.Scale;
        this.Anchor = t.Anchor;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Transform2D(Transform2D t) {
        this.entity = t.entity;
        this.Pos = t.Pos;
        this.Rot = t.Rot;
        this.Scale = t.Scale;
        this.Anchor = t.Anchor;
    }

    /// <summary>
    /// Create a new transform for the given entity.
    /// </summary>
    public Transform2D(VisualEntity2D entity) {
        this.entity = entity;
    }
}
