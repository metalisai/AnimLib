namespace AnimLib;

public abstract class DynVisualEntity {
    public int Id { get; internal set; }
    DynProperty<int> Parent;
    DynProperty<bool> Active;
    DynProperty<int> SortKey;
}

public abstract class DynVisualEntity2D : DynVisualEntity {
    DynProperty<Vector2> Position;
    DynProperty<float> Rotation;
    DynProperty<Vector2> Scale;
    DynProperty<Vector2> Anchor;
    DynProperty<Vector2> Pivot;
    DynProperty<M3x3> Homography;
}

/// <summary>
/// A shape defined by path.
/// </summary>
public class DynShape : DynVisualEntity2D {
    DynProperty<ShapePath> Path { get; set; }

    public DynShape(ShapePath path) {
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
    }

    internal DynProperty<float> radiusP = new DynProperty<float>("radius", 0.0f);

    public DynProperty<float> Radius {
        get {
            return radiusP;
        }
        set {
            radiusP.Value = value.Value;
        }
    }
}
