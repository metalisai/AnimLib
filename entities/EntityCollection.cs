using System.Collections.Generic;

namespace AnimLib;

public abstract class EntityCollection2D : VisualEntity2D {
    List<VisualEntity> entities = new List<VisualEntity>();

    public EntityCollection2D(EntityState2D state) : base(state) {
    }

    public EntityCollection2D(EntityCollection2D e) : base(e) {
    }

    // attach an entity to this collection
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

    // detach collection entity from parent
    public void Detach(VisualEntity2D e) {
        entities.Remove(e);

        if (e.created) {
            World.current.DetachChild(this, e);
        }
    }


    public void DestroyChild(VisualEntity2D e)
    {
        Detach(e);
        if (e.created) {
            World.current.Destroy(e);
        }
        entities.Remove(e);
    }

    // disconnect all entities from this collection (but don't destroy them)
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

    protected override void OnCreated() {
        foreach (var e in entities) {
            World.current.CreateInstantly(e);
            World.current.AttachChild(this, e);
        }
        base.OnCreated();
    }
}
