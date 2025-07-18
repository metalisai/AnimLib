using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A meta-entity that contains other entities.
/// </summary>
public abstract class EntityCollection2D : VisualEntity2D {
    List<VisualEntity> entities = new ();

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
    public void Attach(VisualEntity2D e) {
        if (!e.Created && this.Created) {
            World.current.CreateInstantly(e);
        }
        else if (e.Created && !this.Created) {
            Debug.Error("Cannot attach a created entity to a non-created collection");
            return;
        }
        entities.Add(e);
        if (this.Created) {
            World.current.AttachChild(this, e);
        }
        e.Parent = this;
    }

    /// <summary> Detach collection entity from this collection. Note that managing the detached entity's lifetime is not managed by this collection after detaching.</summary>
    /// <param name="e">The entity to detach.</param>
    public void Detach(VisualEntity2D e)
    {
        entities.Remove(e);

        if (e.Created)
        {
            World.current.DetachChild(this, e);
        }
        // TODO: should remove parent and assign absolute coordinates
        //e.Parent = null;
    }


    /// <summary> Destroy a child entity. </summary>
    public void DestroyChild(VisualEntity2D e)
    {
        Detach(e);
        if (e.Created) {
            World.current.Destroy(e);
        }
        entities.Remove(e);
    }

    /// <summary> Disconnect all entities from this collection (but don't destroy them). Destroys the collection entity.</summary>
    public void Disband()
    {
        foreach(var e in entities) {
            if (e.Created) {
                World.current.DetachChild(this, e);
            }
        }
        entities.Clear();
        World.current.Destroy(this);
    }

    internal override void OnCreated() {
        base.OnCreated();
        foreach (var e in entities)
        {
            World.current.CreateInstantly(e);
            World.current.AttachChild(this, e);
        }
    }
}
