using System.Linq;

namespace AnimLib;
internal class BezierState : EntityState3D {
    public float width = 1.0f;
    public Color color = Color.BLACK;
    public Vector3[] points;

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

public class BezierSpline : VisualEntity3D {
    public float Width {
        get {
            return ((BezierState)state).width;
        }
        set {
            World.current.SetProperty(this, "width", value, ((BezierState)state).width);
            ((BezierState)state).width = value;
        }
    }

    public Color Color {
        get {
            return ((BezierState)state).color;
        }
        set {
            World.current.SetProperty(this, "color", value, ((BezierState)state).color);
            ((BezierState)state).color = value;
        }
    }

    public Vector3[] Points {
        get {
            return ((BezierState)state).points;
        }
        set {
            if((value?.Length ?? 0) < 3) {
                Debug.Error("Bezier spline must have at least 3 points");
                return;
            }
            World.current.SetProperty(this, "points", value, ((BezierState)state).points);
            ((BezierState)state).points = value;
        }
    }

    public static void MakeContinuous(Vector3[] ps) {
        int count = (ps.Length - 1)/2 - 1;
        for(int i = 0; i < count; i++) {
            ps[3+i*2] = 2.0f*ps[2+i*2] - ps[1+i*2];
        }
    }

    public void MakeContinuous() {
        var ps = Points.ToArray();
        MakeContinuous(ps);
        Points = ps; // force update command
    }

    public BezierSpline() : base(new BezierState()) {
    }

    public BezierSpline(BezierSpline bs) : base(bs) {
    }

    public override object Clone() {
        return new BezierSpline(this);
    }
}
