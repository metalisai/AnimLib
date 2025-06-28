using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A meta-entity that contains other entities.
/// </summary>
public abstract class EntityCollection2D : DynVisualEntity2D {
    List<DynVisualEntity> entities = new ();

    public EntityCollection2D() : base()
    {
        
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public EntityCollection2D(EntityCollection2D e) : base(e)
    {
    }

    /// <summary> Attach an entity to this collection </summary>
    public void Attach(DynVisualEntity2D e) {
        if (!e.Created && this.Created) {
            World.current.CreateDynInstantly(e);
        }
        else if (e.Created && !this.Created) {
            Debug.Error("Cannot attach a created entity to a non-created collection");
            return;
        }
        entities.Add(e);
        if (this.Created) {
            World.current.AttachDynChild(this, e);
        }
        e.Parent = this;
    }

    /// <summary> Detach collection entity from this collection. Note that managing the detached entity's lifetime is not managed by this collection after detaching.</summary>
    /// <param name="e">The entity to detach.</param>
    public void Detach(DynVisualEntity2D e)
    {
        entities.Remove(e);

        if (e.Created)
        {
            World.current.DetachDynChild(this, e);
        }
        // TODO: should remove parent and assign absolute coordinates
        //e.Parent = null;
    }


    /// <summary> Destroy a child entity. </summary>
    public void DestroyChild(DynVisualEntity2D e)
    {
        Detach(e);
        if (e.Created) {
            World.current.DestroyDyn(e);
        }
        entities.Remove(e);
    }

    /// <summary> Disconnect all entities from this collection (but don't destroy them). Destroys the collection entity.</summary>
    public void Disband()
    {
        foreach(var e in entities) {
            if (e.Created) {
                World.current.DetachDynChild(this, e);
            }
        }
        entities.Clear();
        World.current.DestroyDyn(this);
    }

    internal override void OnCreated() {
        base.OnCreated();
        foreach (var e in entities)
        {
            World.current.CreateDynInstantly(e);
            World.current.AttachDynChild(this, e);
        }
    }
}
