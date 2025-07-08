using System;

namespace AnimLib;

public abstract class MeshEntity3D : DynVisualEntity3D
{
    internal MeshEntity3D() : base()
    {

    }

    internal MeshEntity3D(MeshEntity3D other) : base(other)
    {

    }

    private protected void GetState(NewMeshBackedGeometry state, Func<DynPropertyId, object?> evaluator)
    {
        base.GetState(state, evaluator);
        Debug.Assert(this.Created);
        state.UID = NewMeshBackedGeometry.GenerateEntityName(this.Id);
    }
}