namespace AnimLib;

internal class DummyState : EntityState3D {
    public DummyState() : base() {
    }

    public DummyState(DummyState d) : base(d) {
    }

    public override object Clone() {
        return new DummyState(this);
    }
}

/// <summary>
/// A dummy object. Useful for debugging or parenting (the transform kind, not the human kind).
/// </summary>
public class Dummy : VisualEntity3D {
    /// <summary> Create a new dummy object. </summary>
    public Dummy() : base(new DummyState()) {}

    /// <summary> Copy constructor. </summary>
    public Dummy(Dummy dummy) : base(dummy) {}

    /// <summary> Clone this dummy object. </summary>
    public override object Clone() {
        return new Dummy(this);
    }
}
