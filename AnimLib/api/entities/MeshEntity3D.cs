using System;

namespace AnimLib;

public abstract class MeshEntity3D : VisualEntity3D
{
    /// <summary>
    /// Version of the mesh. Setting properties might require regenerating mesh,
    /// this enables renderer to know when it can use cached mesh.
    /// </summary>
    internal DynProperty<int> MeshVersion = DynProperty<int>.CreateEmpty(0);

    internal MeshEntity3D() : base()
    {

    }

    internal MeshEntity3D(MeshEntity3D other) : base(other)
    {

    }

    internal override void OnCreated()
    {
        base.OnCreated();
        MeshVersion = new DynProperty<int>("meshVersion", MeshVersion.Value, MeshVersion);
    }

    private protected void GetState(MeshBackedGeometry state, Func<DynPropertyId, object?> evaluator)
    {
        base.GetState(state, evaluator);
        Debug.Assert(this.Id >= 0);
        state.UID = MeshBackedGeometry.GenerateEntityName(this.Id);
        state.MeshVersion = this.MeshVersion;
    }

    private protected void MeshDirty()
    {
        MeshVersion.Value = MeshVersion.Value + 1;
    }
}