using System;

namespace AnimLib;

internal record EntityStateResolver(Func<int, EntityState?> GetEntityState);

internal abstract class EntityState
{
    // TODO: find way to reference state without VisualEntity
    public int parentId = 0;
    // who created this entity inside the world, i.e. AnimationBehaviour or SceneObject
    public object? creator;
    public bool active = true;
    public bool selectable = true;
    public int entityId = -1;
    public int sortKey = 0; // used to sort 2D or transparent 3D entities

    public EntityState()
    {
    }

    public EntityState(EntityState ent)
    {
        this.parentId = ent.parentId;
        this.active = ent.active;
        this.selectable = ent.selectable;
        this.entityId = ent.entityId;
        this.sortKey = ent.sortKey;
    }
}

/// <summary>
/// A visual entity that can be animated. Base for both 2D and 3D entities.
/// </summary>
public abstract class VisualEntity
{
    /// <summary>
    /// The ID of this entity.
    /// </summary>
    public int Id { get; internal set; }
    public bool ManagedLifetime { get; internal set; }
    /// <summary>
    /// The parent entity ID.
    /// </summary>
    public DynProperty<int> ParentId = DynProperty<int>.CreateEmpty(-1);
    /// <summary>
    /// Whether this entity is active. Inactive entities will not be rendered.
    /// </summary>
    public DynProperty<bool> Active = DynProperty<bool>.CreateEmpty(true);
    /// <summary>
    /// The sort key of this entity. Entities with lower sort keys will be rendered first. Used to resolve draw order issues.
    /// </summary>
    public DynProperty<int> SortKey = DynProperty<int>.CreateEmpty(0);
    /// <summary>
    /// Whether this entity has been created in the world.
    /// </summary>
    public DynProperty<bool> Created = DynProperty<bool>.CreateEmpty(false);

    /// <summary>
    /// Creates a new visual entity.
    /// </summary>
    internal VisualEntity(VisualEntity other)
    {
        this.ParentId = other.ParentId;
        this.Active = other.Active;
        this.SortKey = other.SortKey;
    }

    internal VisualEntity()
    {
    }

    abstract internal object GetState(Func<DynPropertyId, object?> evaluator);

    private protected void GetState(EntityState dest, Func<DynPropertyId, object?> evaluator)
    {
        dest.entityId = Id;
        dest.active = evaluator(Active.Id) as bool? ?? default(bool);
        dest.sortKey = evaluator(SortKey.Id) as int? ?? default(int);
        dest.parentId = evaluator(ParentId.Id) as int? ?? default(int);
    }

    internal virtual void OnCreated()
    {
        ParentId = new DynProperty<int>("parent", ParentId.Value);
        Active = new DynProperty<bool>("active", Active.Value);
        SortKey = new DynProperty<int>("sortKey", SortKey.Value);
        Created = new DynProperty<bool>("created", Created.Value);
        this.Created.Value = true;
    }

    abstract internal object Clone();
}


