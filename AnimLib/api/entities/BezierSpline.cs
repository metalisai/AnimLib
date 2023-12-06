using System;
using System.Linq;

namespace AnimLib;

internal class BezierState : EntityState3D {
    public float width = 1.0f;
    public Color color = Color.BLACK;
    public Vector3[] points = Array.Empty<Vector3>();

    public BezierState() {}

    public BezierState(BezierState bs) : base(bs) {
        this.width = bs.width;
        this.color = bs.color;
        this.points = bs.points;
    }

    public override object Clone()
    {
        return new BezierState(this);
    }
}

/// <summary>
/// A bezier spline in 3D space. Not volumetric.
/// </summary>
public class BezierSpline : VisualEntity3D {
    /// <summary> Line width of the spline </summary>
    public float Width {
        get {
            return ((BezierState)state).width;
        }
        set {
            World.current.SetProperty(this, "width", value, ((BezierState)state).width);
            ((BezierState)state).width = value;
        }
    }

    /// <summary> Color of the spline </summary>
    public Color Color {
        get {
            return ((BezierState)state).color;
        }
        set {
            World.current.SetProperty(this, "color", value, ((BezierState)state).color);
            ((BezierState)state).color = value;
        }
    }

    /// <summary> The points of the spline </summary>
    public Vector3[] Points {
        get {
            return ((BezierState)state).points;
        }
        set {
            if((value.Length) < 3) {
                Debug.Error("Bezier spline must have at least 3 points");
                return;
            }
            World.current.SetProperty(this, "points", value, ((BezierState)state).points);
            ((BezierState)state).points = value;
        }
    }

    /// <summary> Make the spline c1 continuous </summary>
    /// <param name="ps"> The points of the spline </param>
    public static void MakeContinuous(Vector3[] ps) {
        int count = (ps.Length - 1)/2 - 1;
        for(int i = 0; i < count; i++) {
            ps[3+i*2] = 2.0f*ps[2+i*2] - ps[1+i*2];
        }
    }

    /// <summary> Make this spline c1 continuous </summary>
    public void MakeContinuous() {
        var ps = Points.ToArray();
        MakeContinuous(ps);
        Points = ps; // force update command
    }

    /// <summary> Create a new bezier spline </summary>
    public BezierSpline() : base(new BezierState()) {
    }

    /// <summary> Copy constructor </summary>
    public BezierSpline(BezierSpline bs) : base(bs) {
    }

    /// <summary> Clone this spline </summary>
    public override object Clone() {
        return new BezierSpline(this);
    }
}
