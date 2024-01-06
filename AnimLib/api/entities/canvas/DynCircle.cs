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

    /// <summary>
    /// Creates a new circle with the given radius.
    /// </summary>
    public DynCircle(float radius) : base(CreateCirclePath(radius)) {
        radiusP = DynProperty<float>.CreateEmpty(radius);
    }

    private protected DynProperty<float> radiusP;

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
        radiusP = new DynProperty<float>("radius", radiusP.Value);
    }
}
