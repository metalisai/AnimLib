using System;

namespace AnimLib;

/// <summary>
/// Internal state of a rectangle.
/// </summary>
[GenerateDynProperties(forType: typeof(Rectangle))]
internal class RectangleState : ShapeState
{
    [Dyn]
    public float width;
    [Dyn]
    public float height;

    public RectangleState(ShapePath path) : base(path)
    {
    }

    public RectangleState(RectangleState rs) : base(rs)
    {
        this.width = rs.width;
        this.height = rs.height;
    }

    public override Vector2 AABB
    {
        get
        {
            return new Vector2(width, height);
        }
    }

    public override object Clone()
    {
        return new RectangleState(this);
    }
}

/// <summary>
/// A rectangle shaped entity.
/// </summary>
/*public class Rectangle : Shape, IColored
{

    private static ShapePath CreateRectanglePath(float w, float h)
    {
        var pb = new PathBuilder();
        pb.Rectangle(new Vector2(-0.5f * w, -0.5f * h), new Vector2(0.5f * w, 0.5f * h));
        return pb;
    }

    /// <summary>
    /// Creates a new rectangle with the given width and height.
    /// </summary>
    public Rectangle(float w, float h) : base(new RectangleState(CreateRectanglePath(w, h)))
    {
        var s = (RectangleState)this.state;
        s.width = w;
        s.height = h;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Rectangle(Rectangle r) : base(r)
    {
    }

    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public float Width
    {
        get
        {
            return ((RectangleState)state).width;
        }
        set
        {
            var rs = (RectangleState)state;
            rs.width = value;
            var pb = CreateRectanglePath(rs.width, rs.height);
            this.Path = pb;
        }
    }

    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public float Height
    {
        get
        {
            return ((RectangleState)state).height;
        }
        set
        {
            var rs = (RectangleState)state;
            rs.height = value;
            var pb = CreateRectanglePath(rs.width, rs.height);
            this.Path = pb;
        }
    }

    /// <summary>
    /// Clone this rectangle.
    /// </summary>
    public override object Clone()
    {
        return new Rectangle(this);
    }
}*/

/// <summary>
/// A rectangle shaped entity.
/// </summary>
public partial class Rectangle : DynShape
{
    private static ShapePath CreateRectanglePath(float w, float h)
    {
        var pb = new PathBuilder();
        pb.Rectangle(new Vector2(-0.5f * w, -0.5f * h), new Vector2(0.5f * w, 0.5f * h));
        return pb;
    }

    /// <summary>
    /// Creates a new rectangle with given width and height.
    /// </summary>
    public Rectangle(float w, float h) : base(CreateRectanglePath(w, h))
    {
        _widthP.Value = w;
        _heightP.Value = h;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new RectangleState(new ShapePath());
        this.GetState(state, evaluator);
        state.path = CreateRectanglePath(state.width, state.height);
        return state;
    }
}
