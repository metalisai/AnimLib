using System.Collections.Generic;
using System;

namespace AnimLib;

internal class SphereState : MeshBackedGeometry {
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
    protected float _radius;
    public float radius {
        get {
            return _radius;
        }
        set {
            _radius = value;
            dirty = true;
        }
    }
    internal bool dirty = true;
    // this is used to animate lines (from 0 to 1, how much of the line is visible)
    public override object Clone()
    {
        return new SphereState(this);
    }

    public SphereState(string owner) : base(owner) {
        this.Shader = BuiltinShader.SolidColorShader;
    }

    public SphereState(RendererHandle h, string owner) : base(h, owner) {

    }

    public SphereState(SphereState sls) : base(sls) {
        this.color = sls.color;
        this.radius = sls.radius;
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.Dirty = dirty;
        if (dirty) {
            // uvSphere

            int nSlices = 16;
            int nStacks = 16;

            List<Vector3> vertices = new List<Vector3>();
            List<uint> indices = new List<uint>();

            uint v0 = (uint)vertices.Count;
            vertices.Add(new Vector3(0, radius, 0));

            for (int i = 1; i < nStacks; i++) {
                float phi = MathF.PI * i / nStacks;
                float y = radius*MathF.Cos(phi);
                float r = radius*MathF.Sin(phi);
                for (int j = 0; j < nSlices; j++) {
                    float theta = MathF.PI * 2 * j / nSlices;
                    float x = r * MathF.Cos(theta);
                    float z = r * MathF.Sin(theta);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            uint v1 = (uint)vertices.Count;
            vertices.Add(new Vector3(0, -radius, 0));

            for (int i = 0; i < nSlices; i++) {
                uint i0 = (uint)(i + 1);
                uint i1 = (uint)((i + 1) % nSlices + 1);
                indices.AddRange([v0, i1, i0]);
                i0 = (uint)(i + nSlices * (nStacks - 2) + 1);
                i1 = (uint)((i + 1) % nSlices + nSlices * (nStacks - 2) + 1);
                indices.AddRange([v1, i0, i1]);
            }

            for (int j = 0; j < nStacks-2; j++) {
                for (int i = 0; i < nSlices; i++) {
                    uint i0 = (uint)(i + j * nSlices + 1);
                    uint i1 = (uint)((i + 1) % nSlices + j * nSlices + 1);
                    uint i2 = (uint)(i + (j + 1) * nSlices + 1);
                    uint i3 = (uint)((i + 1) % nSlices + (j + 1) * nSlices + 1);
                    indices.AddRange([i0, i1, i2, i1, i3, i2]);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.indices = indices.ToArray();
            mesh.colors = new Color[vertices.Count];
            for (int i = 0; i < vertices.Count; i++) {
                mesh.colors[i] = color;
            }

            dirty = false;
        }
    }
}

/// <summary>
/// A 3D sphere
/// </summary>
public class Sphere : VisualEntity3D, IColored {
    /// <summary>
    /// Radius of the sphere
    /// </summary>
    public float Radius {
        get {
            return ((SphereState)state).radius;
        }
        set {
            World.current.SetProperty(this, "Radius", value, ((SphereState)state).radius);
            ((SphereState)state).radius = value;
        }
    }

    /// <summary>
    /// Color of the sphere
    /// </summary>
    public Color Color {
        get {
            return ((SphereState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((SphereState)state).color);
            ((SphereState)state).color = value;
        }
    }

    /// <summary>
    /// Create a new sphere
    /// </summary>
    public Sphere(string owner) : base(new SphereState(owner)) {
    }

    /// <summary>
    /// Create a new sphere
    /// </summary>
    public Sphere() : this(World.current.Resources.GetGuid()) {
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    public Sphere(Sphere s) : base (s) {
    }

    /// <summary>
    /// Clone this sphere
    /// </summary>
    public override object Clone() {
        return new Sphere(this);
    }
}
