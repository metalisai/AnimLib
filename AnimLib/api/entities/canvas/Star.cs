using System;

namespace AnimLib;

/// <summary>
/// Internal state of a star.
/// </summary>
[GenerateDynProperties(forType: typeof(Star))]
internal class StarState : ShapeState
{
    [Dyn]
    public int numPoints;
    [Dyn]
    public float innerRadius;
    [Dyn]
    public float outerRadius;

    public StarState(ShapePath path) : base(path)
    {
    }

    public StarState(StarState rs) : base(rs)
    {
        this.numPoints = rs.numPoints;
        this.innerRadius = rs.innerRadius;
        this.outerRadius = rs.outerRadius;
    }

    public override Vector2 AABB
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public override object Clone()
    {
        return new StarState(this);
    }
}

/// <summary>
/// A star shaped 2D entity.
/// </summary>
public partial class Star : DynShape
{
    private static ShapePath CreateStarPath(float outerR, float innerR, int points = 5)
    {
        var pb = new PathBuilder();
        pb.Star(outerR, innerR, points);
        return pb;
    }

    /// <summary>
    /// Creates a new 2D star shape with given radiuses and number of corners.
    /// </summary>
    public Star(float outerR, float innerR, int points = 5) : base(CreateStarPath(outerR, innerR, points))
    {
        _innerRadiusP.Value = innerR;
        _outerRadiusP.Value = outerR;
        _numPointsP.Value = points;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new StarState(new ShapePath());
        this.GetState(state, evaluator);
        state.path = CreateStarPath(state.innerRadius, state.outerRadius, state.numPoints);
        return state;
    }
}
