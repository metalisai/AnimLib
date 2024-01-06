using System;

namespace AnimLib;

/// <summary>
/// A visual entity that can be animated. Base for both 2D and 3D entities.
/// </summary>
public abstract class DynVisualEntity {
    /// <summary>
    /// The ID of this entity.
    /// </summary>
    public int Id { get; internal set; }
    /// <summary>
    /// The parent entity ID.
    /// </summary>
    public DynProperty<int> Parent = DynProperty<int>.CreateEmpty(-1);
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

    internal DynVisualEntity() {
    }

    abstract internal object GetState(Func<DynPropertyId, object?> evaluator);

    private protected void GetState(EntityState dest, Func<DynPropertyId, object?> evaluator) {
        dest.active = evaluator(Active.Id) as bool? ?? default(bool);
        dest.sortKey = evaluator(SortKey.Id) as int? ?? default(int);
        dest.parentId = evaluator(Parent.Id) as int? ?? default(int);
    }

    internal virtual void OnCreated() {
        Parent = new DynProperty<int>("parent", -1);
        Active = new DynProperty<bool>("active", true);
        SortKey = new DynProperty<int>("sortKey", 0);
        Created = new DynProperty<bool>("created", false);
        this.Created = true;
    }
}


