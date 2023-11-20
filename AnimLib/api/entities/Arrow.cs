using System;

namespace AnimLib;

internal class ArrowState : MeshBackedGeometry
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public Color startColor = Color.BLACK;
    public Color endColor = Color.BLACK;
    public float width = 1.0f;
    public float outlineWidth = 1.0f;
    public Color outline = Color.BLACK;

    public override object Clone()
    {
        return new ArrowState(this);
    }

    public ArrowState(ArrowState ars) : base(ars) {
        this.startPoint = ars.startPoint;
        this.endPoint = ars.endPoint;
        this.startColor = ars.startColor;
        this.endColor = ars.endColor;
        this.width = ars.width;
        this.outlineWidth = ars.outlineWidth;
        this.outline = ars.outline;
    }

    public ArrowState(string owner) : base(owner) {
        this.Shader = BuiltinShader.ArrowShader;
        Func<float> len = () => {
            return (startPoint-endPoint).Length;
        };
        this.shaderProperties.Add(("Length", len));
        Func<float> width = () => {
            return this.width;
        };
        Func<Vector4> outlineColor = () => {
            Vector4 ret = new Vector4(outline.r/255.0f, outline.g/255.0f, outline.b/255.0f, outlineWidth);
            return ret;
        };
        this.shaderProperties.Add(("Width", width));
        this.shaderProperties.Add(("_Outline", outlineColor));
    }

    public ArrowState(RendererHandle h, string owner) : base(h, owner) {
        this.Shader = BuiltinShader.ArrowShader;
        Func<float> len = () => {
            return (startPoint-endPoint).Length;
        };
        this.shaderProperties.Add(("Length", len));
        Func<float> width = () => {
            return this.width;
        };
        this.shaderProperties.Add(("Width", width));
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
    {
        Vector2 dir = (endPoint - startPoint).Normalized;
        // We render 2d distance field (union of triangle and rectangle) within a rectangle
        float z = endPoint.z;
        var v1 = (Vector2)endPoint + dir.PerpCcw*width*1f;
        var v2 = (Vector2)startPoint + dir.PerpCcw*width*1f;
        var v3 = (Vector2)startPoint + dir.PerpCw*width*1f;
        var v4 = (Vector2)endPoint + dir.PerpCw*width*1f;
        mesh.vertices = new Vector3[] {
            new Vector3(v1, z),
            new Vector3(v2, z),
            new Vector3(v3, z),
            new Vector3(v4, z),
        };
        mesh.colors = new Color[] {endColor, startColor, startColor, endColor};
        mesh.indices = new uint[]{0, 3, 2, 0, 1, 2};
        mesh.edgeCoordinates = new Vector2[] {
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
        };
        mesh.Dirty = true;
    }
}

public class Arrow2D : VisualEntity3D
{
    public Vector3 StartPoint {
        get {
            return ((ArrowState)state).startPoint;
        }
        set {
            World.current.SetProperty(this, "StartPoint", value, ((ArrowState)state).startPoint);
            ((ArrowState)state).startPoint = value;
        }
    }
    public Vector3 EndPoint {
        get {
            return ((ArrowState)state).endPoint;
        }
        set {
            World.current.SetProperty(this, "EndPoint", value, ((ArrowState)state).endPoint);
            ((ArrowState)state).endPoint = value;
        }
    }
    public Color StartColor {
        get {
            return ((ArrowState)state).startColor;
        }
        set {
            World.current.SetProperty(this, "StartColor", value, ((ArrowState)state).startColor);
            ((ArrowState)state).startColor = value;
        }
    }
    public Color EndColor {
        get {
            return ((ArrowState)state).endColor;
        }
        set {
            World.current.SetProperty(this, "EndColor", value, ((ArrowState)state).endColor);
            ((ArrowState)state).endColor = value;
        }
    }
    public Color Color {
        set {
            StartColor = value;
            EndColor = value;
        }
        get {
            return StartColor;
        }
    }
    public float Width {
        get {
            return ((ArrowState)state).width;
        }
        set {
            World.current.SetProperty(this, "Width", value, ((ArrowState)state).width);
            ((ArrowState)state).width = value;
        }
    }

    public Arrow2D(string owner) : base(new ArrowState(owner)) {
    }

    public Arrow2D() : this(World.current.Resources.GetGuid()) {
    }

    public Arrow2D(Arrow2D arrow) : base(arrow) {
    }

    public override object Clone() {
        return new Arrow2D(this);
    }
}
