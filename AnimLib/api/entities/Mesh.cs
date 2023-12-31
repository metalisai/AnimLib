using System;
using System.Linq;

namespace AnimLib;

internal class MeshState : MeshBackedGeometry
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
    uint[] _indices;
    public uint[] indices {
        get {
            return _indices;
        }
        set {
            _indices = value;
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

    internal MeshState(string owner) : base(owner) {
        this.Shader = BuiltinShader.MeshShader;
        this._vertices = Array.Empty<Vector3>();
        this._indices = Array.Empty<uint>();
        dirty = true;
    }

    public MeshState(MeshState ms) : base(ms) {
        this._vertices = ms.vertices.ToArray();
        this._indices = ms.indices.ToArray();
        this.color = ms.color;
        this.Outline = ms.Outline;
        dirty = true;
    }

    public override object Clone() {
        return new MeshState(this);
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh) {
        mesh.Dirty = dirty;
        if(dirty) {
            mesh.vertices = vertices;
            mesh.indices = indices;
            mesh.colors = vertices.Select(x => color).ToArray();
            dirty = false;
        }
    }
}

/// <summary>
/// A 3D triangle mesh.
/// </summary>
public class Mesh : VisualEntity3D
{
    /// <summary>
    /// The color of the mesh.
    /// </summary>
    public Color Color {
        get {
            return ((MeshState)state).color;
        }
        set {
            World.current.SetProperty(this, "StartPoint", value, ((MeshState)state).color);
            ((MeshState)state).color = value;
        }
    }

    /// <summary>
    /// The outline color of the mesh.
    /// </summary>
    public Color Outline {
        get {
            return ((MeshState)state).Outline;
        }
        set {
            World.current.SetProperty(this, "Outline", value, ((MeshState)state).Outline);
            ((MeshState)state).Outline = value;
        }
    }

    /// <summary>
    /// The vertices of the mesh.
    /// </summary>
    public Vector3[] Vertices {
        get {
            return ((MeshState)state).vertices;
        }
        set {
            World.current.SetProperty(this, "Vertices", value, ((MeshState)state).vertices);
            ((MeshState)state).vertices = value;
        }
    }

    /// <summary>
    /// The indices of the mesh.
    /// </summary>
    public uint[] Indices {
        get {
            return ((MeshState)state).indices;
        }
        set {
            World.current.SetProperty(this, "Indices", value, ((MeshState)state).indices);
            ((MeshState)state).indices = value;
        }
    }

    internal Mesh(string owner) : base(new MeshState(owner)) {
    }

    /// <summary>
    /// Creates a new Mesh.
    /// </summary>
    public Mesh() : this(World.current.Resources.GetGuid()) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Mesh(Mesh mesh) : base(mesh) {
    }

    /// <summary>
    /// Clones this mesh.
    /// </summary>
    public override object Clone() {
        return new Mesh(this);
    }
}
