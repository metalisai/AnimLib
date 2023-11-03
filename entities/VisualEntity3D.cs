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

public abstract class VisualEntity3D : VisualEntity {
    public Transform Transform;

    public VisualEntity3D(VisualEntity3D ent) : base(ent) {
        Transform = new Transform(this);
    }
    
    internal VisualEntity3D(EntityState state) : base(state) {
        Transform = new Transform(this);
    }

    internal new EntityState3D state {
        get {
            return base.state as EntityState3D;
        }
    }
}
