using System;

namespace AnimLib;

/// <summary>
/// A circle shaped entity.
/// </summary>
public class DynCircle : DynShape {
    private static ShapePath CreateCirclePath(float radius) {
        var pb = new PathBuilder();
        pb.Circle(Vector2.ZERO, radius);
        return pb;
    }

    internal DynCircle(DynCircle other) : base(other) {
        this.radiusP.Value = other.radiusP.Value;
    }

    /// <summary>
    /// Creates a new circle with the given radius.
    /// </summary>
    public DynCircle(float radius) : base(CreateCirclePath(radius)) {
        Debug.Log("Create with radius2 " + radius);
        radiusP.Value = radius;
        Debug.Log("Create with radius3 " + radiusP.Value);
    }

    private protected DynProperty<float> radiusP = DynProperty<float>.CreateEmpty(0.0f);
    /// <summary>
    /// The radius of this circle.
    /// </summary>
    public DynProperty<float> Radius {
        get {
            return radiusP;
        }
        set {
            radiusP.Value = value.Value;
        }
    }

    private protected void GetState(CircleState state, Func<DynPropertyId, object?> evaluator) {
        base.GetState(state, evaluator);
        state.radius = evaluator(radiusP.Id) as float? ?? default(float);
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator) {
        var state = new CircleState(new ShapePath());
        this.GetState(state, evaluator);
        state.path = CreateCirclePath(state.radius);
        return state;
    }

    internal override void OnCreated() {
        base.OnCreated();
        Debug.Log("Create with radius " + radiusP.Value);
        radiusP = new DynProperty<float>("radius", radiusP.Value);
    }

    internal override object Clone() {
        return new DynCircle(this);
    }
}
