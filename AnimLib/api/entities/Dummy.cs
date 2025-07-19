using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Dummy))]
internal class DummyState : EntityState3D
{
    public DummyState() : base()
    {
    }

    public DummyState(DummyState d) : base(d)
    {
    }
}

/// <summary>
/// A dummy object. Useful for debugging or parenting (the transform kind, not the human kind).
/// </summary>
public partial class Dummy : VisualEntity3D
{
    /// <summary> Create a new dummy object. </summary>
    public Dummy() : base() { }


    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Id >= 0);
        var state = new CubeState(NewMeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
