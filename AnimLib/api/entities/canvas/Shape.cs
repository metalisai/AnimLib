using System;

namespace AnimLib;

/// <summary>
/// Internal state of a <see cref="Shape"/>.
/// </summary>
[GenerateDynProperties(forType: typeof(Shape))]
internal class ShapeState : EntityState2D {
    [Dyn]
    public ShapePath path;
    [Dyn]
    public Color color = Color.RED;
    [Dyn]
    public Color contourColor = Color.BLACK;
    [Dyn]
    public float contourSize = 0.0f;
    [Dyn]
    public (float, float) trim = (0.0f, 1.0f);
    [Dyn]
    public ShapeMode mode = ShapeMode.FilledContour;

    public ShapeState(ShapePath path) {
        this.path = path;
    }

    public ShapeState(ShapeState ss) : base(ss) {
        this.path = ss.path.Clone();
        this.color = ss.color;
        this.contourColor = ss.contourColor;
        this.contourSize = ss.contourSize;
        this.mode = ss.mode;
        this.trim = ss.trim;
    }

    public override Vector2 AABB {
        get {
            throw new NotImplementedException();
        }
    }
}

/// <summary>
/// A shape defined by path.
/// </summary>
public partial class Shape : VisualEntity2D, IColored
{

    public Shape(ShapePath path)
    {
        this.Path = path;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var shape = new ShapeState(Path ?? new ShapePath());
        GetState(shape, evaluator);
        return shape;
    }
}