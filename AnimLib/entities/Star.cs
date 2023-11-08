namespace AnimLib;

/// <summary>
/// Internal state of a rectangle.
/// </summary>
internal class StarState : ShapeState {
    public int points;
    public float innerRadius, outerRadius;

    public StarState(ShapePath path) : base(path) {
    }

    public StarState(StarState rs) : base(rs) {
        this.points = rs.points;
        this.innerRadius = rs.innerRadius;
        this.outerRadius = rs.outerRadius;
    }

    public override Vector2 AABB {
        get {
            throw new System.NotImplementedException();
        }
    }

    public override object Clone()
    {
        return new StarState(this);
    }
}

/// <summary>
/// A rectangle shaped entity.
/// </summary>
public class Star : Shape, IColored {

    private static ShapePath CreateStarPath(float outerR, float innerR, int points = 5) {
        var pb = new PathBuilder();
        pb.Star(outerR, innerR, points);
        return pb;
    }

    /// <summary>
    /// Creates a new rectangle with the given width and height.
    /// </summary>
    public Star(float outerR, float innerR, int points = 5) : base(new StarState(CreateStarPath(outerR, innerR, points))) {
        var s = this.state as StarState;
        s.points = points;
        s.innerRadius = innerR;
        s.outerRadius = outerR;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Star(Star r) : base(r) {
    }

    /// <summary>
    /// Clone this rectangle.
    /// </summary>
    public override object Clone() {
        return new Star(this);
    }
}
