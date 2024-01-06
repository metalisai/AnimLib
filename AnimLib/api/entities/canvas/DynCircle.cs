using System;

namespace AnimLib;

public abstract class DynVisualEntity {
    public int Id { get; internal set; }
    public DynProperty<int> Parent = DynProperty<int>.CreateEmpty(-1);
    public DynProperty<bool> Active = DynProperty<bool>.CreateEmpty(true);
    public DynProperty<int> SortKey = DynProperty<int>.CreateEmpty(0);
    public DynProperty<bool> Created = DynProperty<bool>.CreateEmpty(false);

    public DynVisualEntity() {
    }

    abstract public object GetState(Func<DynPropertyId, object?> evaluator);

    internal virtual void OnCreated() {
        Parent = new DynProperty<int>("parent", -1);
        Active = new DynProperty<bool>("active", true);
        SortKey = new DynProperty<int>("sortKey", 0);
        Created = new DynProperty<bool>("created", false);
        this.Created = true;
    }
}

public abstract class DynVisualEntity2D : DynVisualEntity {
    public DynProperty<Vector2> Position = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    public DynProperty<float> Rotation = DynProperty<float>.CreateEmpty(0.0f);
    public DynProperty<Vector2> Scale = DynProperty<Vector2>.CreateEmpty(Vector2.ONE);
    public DynProperty<Vector2> Anchor = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    public DynProperty<Vector2> Pivot = DynProperty<Vector2>.CreateEmpty(Vector2.ZERO);
    public DynProperty<M3x3?> Homography = DynProperty<M3x3?>.CreateEmpty(null);

    internal override void OnCreated() {
        base.OnCreated();
        Position = new DynProperty<Vector2>("position", Vector2.ZERO);
        Rotation = new DynProperty<float>("rotation", 0.0f);
        Scale = new DynProperty<Vector2>("scale", Vector2.ONE);
        Anchor = new DynProperty<Vector2>("anchor", Vector2.ZERO);
        Pivot = new DynProperty<Vector2>("pivot", Vector2.ZERO);
        Homography = new DynProperty<M3x3?>("homography", null);
    }
}

/// <summary>
/// A shape defined by path.
/// </summary>
public class DynShape : DynVisualEntity2D {
    protected ShapePath path;
    public DynProperty<ShapePath> Path;

    public DynShape(ShapePath path) {
        this.path = path;
    }

    public override object GetState(Func<DynPropertyId, object?> evaluator) {
        return new ShapeState(Path.Value) {
        };
    }

    internal override void OnCreated() {
        base.OnCreated();
        Path = new DynProperty<ShapePath>("path", path);
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
        radiusP = DynProperty<float>.CreateEmpty(radius);
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
        var pos = evaluator(Position.Id) as Vector2? ?? default(Vector2);
        var scale = evaluator(Scale.Id) as Vector2? ?? default(Vector2);
        return new CircleState(CreateCirclePath(val)) {
            radius = val as float? ?? default(float),
            position = pos,
            scale = scale,
        };
    }

    internal override void OnCreated() {
        base.OnCreated();
        radiusP = new DynProperty<float>("radius", radiusP.Value);
    }
}
