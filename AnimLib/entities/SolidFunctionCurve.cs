using System;
using System.Linq;

namespace AnimLib;

internal class SolidFunctionCurveState : MeshBackedGeometry {
    public Vector2[] handles;
    public Color color;
    public float width;
    public float startX;
    public float endX;
    public Func<float,float> func;
    public int segments = 10;

    public SolidFunctionCurveState(Func<float,float> f, string owner) : base(owner) {
        func = f;
    }

    public SolidFunctionCurveState(Func<float,float> f, RendererHandle handle, string owner) : base(handle, owner) {
        func = f;
    }

    public SolidFunctionCurveState(RendererHandle handle, string owner) : base(handle, owner) { }

    public SolidFunctionCurveState(SolidFunctionCurveState sfcs) : base(sfcs) {
        this.handles = sfcs.handles.ToArray();
        this.color = sfcs.color;
        this.width = sfcs.width;
        this.startX = sfcs.startX;
        this.endX = sfcs.endX;
        this.func = sfcs.func;
        this.segments = sfcs.segments;
    }

    public override object Clone()
    {
        return new SolidFunctionCurveState(this);
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
    {
        Vector3[] segs = new Vector3[segments+1];
        float start = startX;
        float step  = (endX-startX)/(float)segments;
        for(int i = 0; i < segs.Length; i++) {
            float x = start + i*step;
            segs[i] = new Vector3(x, func(x), 0.0f);
        }
        LineRenderer.UpdateLineMesh(mesh, segs, this.width, this.color, ownerGuid);
    }
}

public class SolidFunctionCurve : VisualEntity3D {
    public Vector2[] Handles {
        get {
            return ((SolidFunctionCurveState)state).handles;
        } set {
            World.current.SetProperty(this, "Handles", value, ((SolidFunctionCurveState)state).handles);
            ((SolidFunctionCurveState)state).handles = value;
        }
    }
    public Color Color {
        get {
            return ((SolidFunctionCurveState)state).color;
        } set {
            World.current.SetProperty(this, "Color", value, ((SolidFunctionCurveState)state).color);
            ((SolidFunctionCurveState)state).color = value;
        }
    }
    public float Width {
        get {
            return ((SolidFunctionCurveState)state).width;
        } set {
            World.current.SetProperty(this, "Width", value, ((SolidFunctionCurveState)state).width);
            ((SolidFunctionCurveState)state).width = value;
        }
    }
    public float StartX {
        get {
            return ((SolidFunctionCurveState)state).startX;
        } set {
            World.current.SetProperty(this, "StartX", value, ((SolidFunctionCurveState)state).startX);
            ((SolidFunctionCurveState)state).startX = value;
        }
    }
    public float EndX {
        get {
            return ((SolidFunctionCurveState)state).endX;
        } set {
            World.current.SetProperty(this, "EndX", value, ((SolidFunctionCurveState)state).endX);
            ((SolidFunctionCurveState)state).endX = value;
        }
    }
    public Func<float,float> Func {
        get {
            return ((SolidFunctionCurveState)state).func;
        } set {
            World.current.SetProperty(this, "Func", value, ((SolidFunctionCurveState)state).func);
            ((SolidFunctionCurveState)state).func = value;
        }
    }
    public int Segments {
        get {
            return ((SolidFunctionCurveState)state).segments;
        } set {
            World.current.SetProperty(this, "Segments", value, ((SolidFunctionCurveState)state).segments);
            ((SolidFunctionCurveState)state).segments = value;
        }
    }
    
    public SolidFunctionCurve(Func<float,float> f, string owner) : base(new SolidFunctionCurveState(f, owner)){
    }

    public SolidFunctionCurve(SolidFunctionCurve sfc) : base(sfc) {
    }

    public override object Clone() {
        return new SolidFunctionCurve(this);
    }
}
