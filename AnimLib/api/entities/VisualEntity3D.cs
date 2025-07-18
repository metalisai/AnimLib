namespace AnimLib;

[GenerateDynProperties(forType: typeof(VisualEntity3D))]
internal abstract class EntityState3D : EntityState
{
    [Dyn]
    public Vector3 position;
    [Dyn]
    public Quaternion rotation = Quaternion.IDENTITY;
    [Dyn]
    public Vector3 scale = Vector3.ONE;

    public EntityState3D() : base()
    {
    }

    public EntityState3D(EntityState3D ent) : base(ent)
    {
        this.position = ent.position;
        this.rotation = ent.rotation;
        this.scale = ent.scale;
    }

    // TODO: cache
    public M4x4 ModelToWorld(EntityStateResolver resolver)
    {
        if (parentId <= 0)
        {
            return M4x4.TRS(position, rotation, scale);
        }
        else
        {
            var parent = (EntityState3D?)resolver.GetEntityState(parentId);
            if (parent == null)
            {
                Debug.Error($"Parent entity {parentId} not found");
                return M4x4.TRS(position, rotation, scale);
            }
            return parent.ModelToWorld(resolver) * M4x4.TRS(position, rotation, scale);
        }
    }
}

public abstract partial class VisualEntity3D : VisualEntity
{
    internal VisualEntity3D()
    {

    }
}