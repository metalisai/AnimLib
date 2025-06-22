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

    public override object Clone()
    {
        return new CircleState(this);
    }
}

/// <summary>
/// A circle shaped entity.
/// </summary>
/*public class Circle : Shape, IColored {
    private static ShapePath CreateCirclePath(float radius) {
        var pb = new PathBuilder();
        pb.Circle(Vector2.ZERO, radius);
        return pb;
    }

    /// <summary>
    /// Creates a new circle with the given radius.
    /// </summary>
    public Circle(float radius) : base(new CircleState(CreateCirclePath(radius))) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Circle(Circle c) : base(c) {
    }

    /// <summary>
    /// The radius of the circle.
    /// </summary>
    public float Radius { 
        get {
            return ((CircleState)state).radius;
        }
        set {
            World.current.SetProperty(this, "Radius", value, ((CircleState)state).radius);
            ((CircleState)state).radius = value;
        }
    }

    /// <summary>
    /// Clone this circle.
    /// </summary>
    public override object Clone() {
        return new Circle(this);
    }
}
*/

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
        this.GetState(state, evaluator);
        state.path = CreateCirclePath(state.radius);
        return state;
    }
}