using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A meta-entity that contains other entities.
/// </summary>
public abstract class EntityCollection2D : VisualEntity2D {
    List<VisualEntity> entities = new List<VisualEntity>();

    internal EntityCollection2D(EntityState2D state) : base(state) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public EntityCollection2D(EntityCollection2D e) : base(e) {
    }

    /// <summary> Attach an entity to this collection </summary>
    public void Attach(VisualEntity2D e) {
        if (!e.created && this.created) {
            World.current.CreateInstantly(e);
        }
        else if (e.created && !this.created) {
            Debug.Error("Cannot attach a created entity to a non-created collection");
            return;
        }
        entities.Add(e);
        if (this.created) {
            World.current.AttachChild(this, e);
        }
        e.Transform.parent = Transform;
    }

    /// <summary> Detach collection entity from this collection. Note that managing the detached entity's lifetime is not managed by this collection after detaching.</summary>
    /// <param name="e">The entity to detach.</param>
    public void Detach(VisualEntity2D e) {
        entities.Remove(e);

        if (e.created) {
            World.current.DetachChild(this, e);
        }
    }


    /// <summary> Destroy a child entity. </summary>
    public void DestroyChild(VisualEntity2D e)
    {
        Detach(e);
        if (e.created) {
            World.current.Destroy(e);
        }
        entities.Remove(e);
    }

    /// <summary> Disconnect all entities from this collection (but don't destroy them). </summary>
    public void Disband()
    {
        foreach(var e in entities) {
            if (e.created) {
                World.current.DetachChild(this, e);
            }
        }
        entities.Clear();
        World.current.Destroy(this);
    }

    private protected override void OnCreated() {
        foreach (var e in entities) {
            World.current.CreateInstantly(e);
            World.current.AttachChild(this, e);
        }
        base.OnCreated();
    }
}
