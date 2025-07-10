using System;

namespace AnimLib;

/// <summary>
/// Internal state of a circle.
/// </summary>
[GenerateDynProperties(forType: typeof(Circle))]
internal class CircleState : ShapeState
{
    [Dyn]
    public float radius;

    public CircleState(ShapePath path) : base(path)
    {
    }

    public CircleState(CircleState cs) : base(cs)
    {
        this.radius = cs.radius;
    }
}

/// <summary>
/// A circle shaped entity.
/// </summary>
public partial class Circle : DynShape
{
    private static ShapePath CreateCirclePath(float radius)
    {
        var pb = new PathBuilder();
        pb.Circle(Vector2.ZERO, radius);
        return pb;
    }

    /// <summary>
    /// Creates a new circle with the given radius.
    /// </summary>
    public Circle(float radius) : base(CreateCirclePath(radius))
    {
        _radiusP.Value = radius;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new CircleState(new ShapePath());
        GetState(state, evaluator);
        state.path = CreateCirclePath(state.radius);
        return state;
    }
}