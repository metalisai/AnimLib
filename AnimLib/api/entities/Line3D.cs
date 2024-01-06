using System;
using System.Linq;

namespace AnimLib;

internal class Line3DState : MeshBackedGeometry
{
    bool dirty = true;
    Vector3[] _vertices;
    public Vector3[] vertices {
        get {
            return _vertices;
        }
        set {
            _vertices = value;
            dirty = true;
        }
    }

    Color _color;
    public Color color {
        get {
            return _color;
        } 
        set {
            _color = value;
            dirty = true;
        }
    }

    Color[] _colors;
    public Color[] colors {
        get {
            return _colors;
        }
        set {
            _colors = value;
            dirty = true;
        }
    }

    internal Line3DState(string owner) : base(owner) {
        this.Shader = BuiltinShader.LineShader;
        this._vertices = Array.Empty<Vector3>();
        this._colors = Array.Empty<Color>();
        dirty = true;
    }

    public Line3DState(Line3DState ms) : base(ms) {
        this._vertices = ms.vertices.ToArray();
        this.color = ms.color;
        this._colors = ms.colors.ToArray();
        this.outline = ms.outline;
        dirty = true;
    }

    public override object Clone() {
        return new Line3DState(this);
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh) {
        mesh.Dirty = dirty;
        if(dirty) {
            mesh.vertices = vertices;
            if (_colors.Length > 0 && colors.Length == vertices.Length) {
                mesh.colors = _colors;
            } else {
                if (_colors.Length > 0) {
                    Debug.Error("Line3DState: colors.Length != vertices.Length");
                }
                mesh.colors = vertices.Select(x => color).ToArray();
            }
            dirty = false;
        }
    }
}

/// <summary>
/// A 3D triangle mesh.
/// </summary>
public class Line3D : VisualEntity3D
{
    /// <summary>
    /// The color of the mesh.
    /// </summary>
    public Color Color {
        get {
            return ((Line3DState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((Line3DState)state).color);
            ((Line3DState)state).color = value;
        }
    }

    /// <summary>
    /// The colors of line vertices.
    /// </summary>
    public Color[] Colors {
        get {
            return ((Line3DState)state).colors;
        }
        set {
            World.current.SetProperty(this, "Colors", value, ((Line3DState)state).colors);
            ((Line3DState)state).colors = value;
        }
    }

    /// <summary>
    /// The vertices of the mesh.
    /// </summary>
    public Vector3[] Vertices {
        get {
            return ((Line3DState)state).vertices;
        }
        set {
            World.current.SetProperty(this, "Vertices", value, ((Line3DState)state).vertices);
            ((Line3DState)state).vertices = value;
        }
    }

    /// <summary>
    /// The width of the line in pixels.
    /// </summary>
    public float Width {
        get {
            return (float)((Line3DState)state).properties["Width"].Value;
        }
        set {
            ((Line3DState)state).properties["Width"].Value = value;
        }
    }

    internal Line3D(string owner) : base(new Line3DState(owner)) {
    }

    /// <summary>
    /// Creates a new Mesh.
    /// </summary>
    public Line3D(float width = 1.0f) : this(World.current.Resources.GetGuid()) {
        var state = (Line3DState)this.state;
        state.properties.Add("Width", new DynProperty<float>("Width", width));
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Line3D(Line3D mesh) : base(mesh) {
        var state = (Line3DState)this.state;
        var ostate = (Line3DState)mesh.state;
        state.properties.Add("Width", new DynProperty<float>("Width", ostate.properties["Width"].Value as float? ?? default(float)));
    }

    /// <summary>
    /// Clones this mesh.
    /// </summary>
    public override object Clone() {
        return new Line3D(this);
    }
}
