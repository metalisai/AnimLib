namespace AnimLib;

/// <summary>
/// A 3D transform, consisting of a position, rotation, and scale.
/// </summary>
public class Transform {
    /// <summary>
    /// The entity this transform is attached to.
    /// </summary>
    protected VisualEntity3D entity;
    internal Transform? _parent;

    /// <summary>
    /// The parent transform.
    /// </summary>
    public Transform? parent {
        get {
            if (entity.state.parentId == 0) {
                return null;
            }
            var e = World.current.EntityResolver.GetEntity(entity.state.parentId);
            return ((VisualEntity3D)e)?.Transform;
        } set {
            _parent = value;
            if (value != null) {
                World.current.SetProperty(entity, "parentId", value.entity.state.entityId, entity.state.parentId);
                entity.state.parentId = value.entity.state.entityId;
            }
        }
    }

    /// <summary>
    /// The world position of this transform.
    /// </summary>
    public virtual Vector3 WorldPos {
        get {
            // TODO
            Debug.Warning("Warning: WorldPos not implemented for this type of transform.");
            return Pos;
        }
    }

    /// <summary>
    /// The local position of this transform.
    /// </summary>
    public Vector3 Pos {
        get {
            return entity.state.position;
        } set {
            World.current.SetProperty(entity, "position", value, entity.state.position);
            entity.state.position = value;
        }
    }

    /// <summary>
    /// The local rotation of this transform.
    /// </summary>
    public Quaternion Rot {
        get {
            return entity.state.rotation;
        }
        set {
            World.current.SetProperty(entity, "rotation", value, entity.state.rotation);
            entity.state.rotation = value;
        }
    }

    /// <summary>
    /// The local scale of this transform.
    /// </summary>
    public Vector3 Scale {
        get {
            return entity.state.scale;
        }
        set {
            World.current.SetProperty(entity, "scale", value, entity.state.scale);
            entity.state.scale = value;
        }
    }

    /// <summary>
    /// Creates a new transform.
    /// </summary>
    public Transform(VisualEntity3D entity, Vector3 pos, Quaternion rot) {
        this.entity = entity;
        this.Pos = pos;
        this.Rot = rot;
        this.Scale = Vector3.ONE;
    }

    /// <summary>
    /// Creates a new transform.
    /// </summary>
    public Transform(VisualEntity3D entity, Vector3 pos, Quaternion rot, Vector3 scale) {
        this.entity = entity;
        this.Pos = pos;
        this.Rot = rot;
        this.Scale = scale;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Transform(Transform t) {
        this.entity = t.entity;
        this.Pos = t.Pos;
        this.Rot = t.Rot;
        this.Scale = t.Scale;
    }

    /// <summary>
    /// Creates a new transform.
    /// </summary>
    public Transform(VisualEntity3D entity) {
        this.entity = entity;
    }
}
