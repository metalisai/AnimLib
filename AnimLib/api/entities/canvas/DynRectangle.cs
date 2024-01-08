using System;

namespace AnimLib;

/// <summary>
/// A circle shaped entity.
/// </summary>
public class DynRectangle : DynShape {
    private static ShapePath CreateRectanglePath(float w, float h) {
        var pb = new PathBuilder();
        pb.Rectangle(new Vector2(-0.5f*w, -0.5f*h), new Vector2(0.5f*w, 0.5f*h));
        return pb;
    }

    internal DynRectangle(DynRectangle other) : base(other) {
        this.widthP.Value = other.widthP.Value;
        this.heightP.Value = other.heightP.Value;
    }

    /// <summary>
    /// Creates a new circle with the given radius.
    /// </summary>
    public DynRectangle(float w, float h) : base(CreateRectanglePath(w, h)) {
        widthP.Value = w;
        heightP.Value = h;
    }

    private protected DynProperty<float> widthP = DynProperty<float>.CreateEmpty(0.0f);
    /// <summary>
    /// The radius of this circle.
    /// </summary>
    public DynProperty<float> Width {
        get {
            return widthP;
        }
        set {
            widthP.Value = value.Value;
        }
    }

    private protected DynProperty<float> heightP = DynProperty<float>.CreateEmpty(0.0f);
    /// <summary>
    /// The radius of this circle.
    /// </summary>
    public DynProperty<float> Height {
        get {
            return heightP;
        }
        set {
            heightP.Value = value.Value;
        }
    }

    private protected void GetState(RectangleState state, Func<DynPropertyId, object?> evaluator) {
        base.GetState(state, evaluator);
        state.width = evaluator(widthP.Id) as float? ?? default(float);
        state.height = evaluator(heightP.Id) as float? ?? default(float);
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator) {
        var state = new RectangleState(new ShapePath());
        this.GetState(state, evaluator);
        state.path = CreateRectanglePath(state.width, state.height);
        return state;
    }

    internal override void OnCreated() {
        base.OnCreated();
        widthP = new DynProperty<float>("width", widthP.Value);
        heightP = new DynProperty<float>("height", heightP.Value);
    }

    internal override object Clone() {
        return new DynRectangle(this);
    }
}
