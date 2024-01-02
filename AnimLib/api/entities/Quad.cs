namespace AnimLib;

internal class QuadState : MeshBackedGeometry
{
    bool dirty = true;

    Vector3[] _vertices = new Vector3[4];
    public Vector3[] vertices {
        get {
            return _vertices;
        }
        set {
            _vertices = value;
            dirty = true;
        }
    }

    public Color _color;
    public Color color {
        get {
            return _color;
        } 
        set {
            _color = value;
            dirty = true;
        }
    }

    internal QuadState(string owner) : base(owner) {
        this.Shader = BuiltinShader.QuadShader;
        dirty = true;
    }

    public QuadState(QuadState qs) : base(qs) {
        for(int i = 0; i < 4; i++) {
            this._vertices[i] = qs.vertices[i];
        }
        this.color = qs.color;
        this.outline = qs.outline;
        dirty = true;
    }

    public override object Clone() {
        return new QuadState(this);
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh) {
        mesh.Dirty = dirty;
        if(dirty) {
            mesh.vertices = vertices;
            mesh.colors = new Color[4] { color, color, color, color };
            mesh.indices = new uint[6] { 0, 1, 2, 2, 3, 0 };
            dirty = false;
        }
    }
}

/// <summary>
/// A 3D quad. For when you don't want that ugly edge in the middle.
/// </summary>
public class Quad : VisualEntity3D {
    /// <summary>
    /// The color of the mesh.
    /// </summary>
    public Color Color {
        get {
            return ((QuadState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((QuadState)state).color);
            ((QuadState)state).color = value;
        }
    }

    /// <summary>
    /// The outline color of the mesh.
    /// </summary>
    public Color Outline {
        get {
            return ((QuadState)state).outline;
        }
        set {
            World.current.SetProperty(this, "Outline", value, ((QuadState)state).outline);
            ((QuadState)state).outline = value;
        }
    }

    /// <summary>
    /// The three vertices of the quad. Fourth is calculated.
    /// </summary>
    public (Vector3, Vector3, Vector3, Vector3) Vertices {
        set {
            Vector3[] v = new Vector3[4];
            v[0] = value.Item1;
            v[1] = value.Item2;
            v[2] = value.Item3;
            v[3] = value.Item4;
            World.current.SetProperty(this, "Vertices", v, ((QuadState)state).vertices);
            ((QuadState)state).vertices = v;
        }
    }

    internal Quad(string owner) : base(new QuadState(owner)) {
    }

    /// <summary>
    /// Create a new quad.
    /// </summary>
    public Quad() : this(World.current.Resources.GetGuid()) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Quad(Quad q) : base(q) {
    }

    /// <summary>
    /// Clone this quad.
    /// </summary>
    public override object Clone() {
        return new Quad(this);
    }
}
