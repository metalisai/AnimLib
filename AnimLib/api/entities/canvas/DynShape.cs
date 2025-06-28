using System;

namespace AnimLib;

/// <summary>
/// A shape defined by path.
/// </summary>
public partial class DynShape : DynVisualEntity2D, IColored {

    public DynShape(ShapePath path) {
        this.Path = path;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator) {
        var shape = new ShapeState(Path ?? new ShapePath());
        GetState(shape, evaluator);
        return shape;
    }
}