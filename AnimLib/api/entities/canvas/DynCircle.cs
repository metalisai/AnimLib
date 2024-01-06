using System;

namespace AnimLib;

public abstract class DynVisualEntity {
    public int Id { get; internal set; }
    public DynProperty<int> Parent;
    public DynProperty<bool> Active;
    public DynProperty<int> SortKey;
    public DynProperty<bool> Created;

    abstract public object GetState(Func<DynPropertyId, object?> evaluator);
}

public abstract class DynVisualEntity2D : DynVisualEntity {
    public DynProperty<Vector2> Position;
    public DynProperty<float> Rotation;
    public DynProperty<Vector2> Scale;
    public DynProperty<Vector2> Anchor;
    public DynProperty<Vector2> Pivot;
    public DynProperty<M3x3> Homography;
}

/// <summary>
/// A shape defined by path.
/// </summary>
public class DynShape : DynVisualEntity2D {
    protected DynProperty<ShapePath> Path { get; set; }

    public DynShape(ShapePath path) {
        Path = new DynProperty<ShapePath>("path", path);
    }

    public override object GetState(Func<DynPropertyId, object?> evaluator) {
        return new ShapeState(Path.Value) {
        };
    }
}

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
        radiusP = new DynProperty<float>("radius", radius);
    }

    internal DynProperty<float> radiusP;

    public DynProperty<float> Radius {
        get {
            return radiusP;
        }
        set {
            radiusP.Value = value.Value;
        }
    }

    public override object GetState(Func<DynPropertyId, object?> evaluator) {
        float val = evaluator(radiusP.Id) as float? ?? default(float);
        return new CircleState(CreateCirclePath(val)) {
            radius = val as float? ?? default(float),
        };
    }
}
