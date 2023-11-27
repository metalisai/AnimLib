namespace AnimLib;

internal abstract class EntityState3D : EntityState {
    public Vector3 position;
    public Quaternion rotation = Quaternion.IDENTITY;
    public Vector3 scale = Vector3.ONE;

    public EntityState3D() : base() {
    }

    public EntityState3D(EntityState3D ent) : base(ent) {
        this.position = ent.position;
        this.rotation = ent.rotation;
        this.scale = ent.scale;
    }

    // TODO: cache
    public M4x4 ModelToWorld(EntityStateResolver resolver) {
        if(parentId == 0) {
            return M4x4.TRS(position, rotation, scale);
        } else { 
            var parent = (EntityState3D)resolver.GetEntityState(parentId);
            return parent.ModelToWorld(resolver) * M4x4.TRS(position, rotation, scale);
        }
    }
}

/// <summary>
/// A 3D visual entity. Unlike <see cref="VisualEntity2D"/>, this entity does not require a canvas (and can't be placed on one).
/// </summary>
public abstract class VisualEntity3D : VisualEntity {
    /// <summary>
    /// The 3D transform of the entity.
    /// </summary>
    public Transform Transform;

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public VisualEntity3D(VisualEntity3D ent) : base(ent) {
        Transform = new Transform(this);
    }
    
    /// <summary>
    /// Creates a new 3D visual entity from a state.
    /// </summary>
    internal VisualEntity3D(EntityState state) : base(state) {
        Transform = new Transform(this);
    }

    internal new EntityState3D state {
        get {
            return (EntityState3D)base.state;
        }
    }
}
