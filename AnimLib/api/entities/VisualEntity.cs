using System;

namespace AnimLib;

[Flags]
internal enum VisualEntityFlags {
    None = 0,
    Created = 1,
    ManagedLifetime = 2,
}

internal record EntityStateResolver(Func<int, EntityState?> GetEntityState);

internal abstract class EntityState : ICloneable {
    // TODO: find way to reference state without VisualEntity
    public int parentId = 0;
    // who created this entity inside the world, i.e. AnimationBehaviour or SceneObject
    public object? creator; 
    public bool active = true;
    public bool selectable = true;
    public int entityId = -1;
    public int sortKey = 0; // used to sort 2D or transparent 3D entities
    public abstract object Clone();

    public EntityState() {
    }

    public EntityState(EntityState ent) {
        this.parentId = ent.parentId;
        this.active = ent.active;
        this.selectable = ent.selectable;
        this.entityId = ent.entityId;
        this.sortKey = ent.sortKey;
    }
}

/// <summary>
/// A visual entity. Base class for all visual entities.
/// </summary>
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

    /// <summary>
    /// Whether the entity has been created.
    /// </summary>
    public bool created {
        get {
            return GetFlag(VisualEntityFlags.Created);
        }
        internal set {
            SetFlag(VisualEntityFlags.Created, value);
        }
    }

    /// <summary>
    /// Whether the entity's lifetime is managed by some other entity. For example glyph shapes can be managed by text entities.
    /// </summary>
    public bool managedLifetime {
        get {
            return GetFlag(VisualEntityFlags.ManagedLifetime);
        }
        internal set {
            SetFlag(VisualEntityFlags.ManagedLifetime, value);
        }
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public VisualEntity(VisualEntity ent){
        this.state = (EntityState)ent.state.Clone();
        this.state.entityId = -1;
    }

    internal VisualEntity(EntityState state) {
        this.state = state;
    }

    /// <summary>
    /// Whether the entity is active. Inactive entities are not rendered, but still exist in the world.
    /// </summary>
    public bool Active {
        get {
            return state.active;
        }
        set {
            World.current.SetProperty(this, "Active", value, state.active);
            state.active = value;
        }
    }

    /// <summary>
    /// The id of the entity.
    /// </summary>
    public int EntityId {
        get {
            return state.entityId;
        } set {
            state.entityId = value;
        }
    }

    /// <summary>
    /// Sort key used for sorting entities on the same canvas.
    /// </summary>
    public int SortKey {
        get {
            return ((EntityState)state).sortKey;
        }
        set {
            World.current.SetProperty(this, "SortKey", value, ((EntityState)state).sortKey);
            ((EntityState)state).sortKey = value;
        }
    }

    internal void EntityCreated() {
        OnCreated();
    }

    private protected virtual void OnCreated() {
    }

    /// <summary>
    /// Clone this entity.
    /// </summary>
    public abstract object Clone();
}
