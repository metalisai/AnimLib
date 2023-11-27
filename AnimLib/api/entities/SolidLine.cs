using System;
using System.Linq;

namespace AnimLib;

internal class SolidLineState : MeshBackedGeometry {
    public Vector3[] points;
    public Color color;
    public float width;
    // this is used to animate lines (from 0 to 1, how much of the line is visible)
    public float progression = 1.0f;

    public override object Clone()
    {
        return new SolidLineState(this);
    }

    public SolidLineState(string owner) : base(owner) {

    }

    public SolidLineState(RendererHandle h, string owner) : base(h, owner) {

    }

    public SolidLineState(SolidLineState sls) : base(sls) {
        this.color = sls.color;
        this.points = sls.points.ToArray();
        this.width = sls.width;
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
    {
        if(points.Length == 2) {
            var ps = points.ToArray();
            ps[1] = points[0] + progression*(points[1]-points[0]);
            LineRenderer.UpdateLineMesh(mesh, ps, width, color, ownerGuid);
        } else {
            LineRenderer.UpdateLineMesh(mesh, points, width, color, ownerGuid);
        }
    }
}

public class SolidLine : VisualEntity3D {
    public Vector3[] Points { 
        get {
            return ((SolidLineState)state).points;
        }
        set {
            World.current.SetProperty(this, "Points", value, ((SolidLineState)state).points);
            ((SolidLineState)state).points = value;
        }
    }
    public Color Color { 
        get {
            return ((SolidLineState)state).color;
        } set {
            World.current.SetProperty(this, "Color", value, ((SolidLineState)state).color);
            ((SolidLineState)state).color = value;
        }
    }
    public float Width {
        get {
            return ((SolidLineState)state).width;
        } set {
            World.current.SetProperty(this, "Width", value, ((SolidLineState)state).width);
            ((SolidLineState)state).width = value;
        }
    }

    public float Progression {
        get {
            return ((SolidLineState)state).progression;
        } set {
            World.current.SetProperty(this, "Progression", value, ((SolidLineState)state).progression);
            ((SolidLineState)state).progression = value;
        }
    }

    public SolidLine(string owner) : base(new SolidLineState(owner)) {
    }

    public SolidLine() : this(World.current.Resources.GetGuid()) {
    }

    public SolidLine(SolidLine sl) : base(sl) {
    }

    public override object Clone() {
        return new SolidLine(this);
    }
}
