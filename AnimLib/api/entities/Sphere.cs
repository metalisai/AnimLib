using System.Collections.Generic;
using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Sphere))]
internal class SphereState : NewMeshBackedGeometry
{
    [Dyn]
    public Color color = Color.YELLOW;
    [Dyn(onSet: ["MeshDirty"])]
    public float radius = 0.5f;

    public SphereState(string uid) : base(uid)
    {
        this.Shader = BuiltinShader.SolidColorShader;
    }
    
    public SphereState(string uid, SphereState sls) : this(uid)
    {
        this.color = sls.color;
        this.radius = sls.radius;
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        // uvSphere

        int nSlices = 16;
        int nStacks = 16;

        List<Vector3> vertices = new List<Vector3>();
        List<uint> indices = new List<uint>();

        uint v0 = (uint)vertices.Count;
        vertices.Add(new Vector3(0, radius, 0));

        for (int i = 1; i < nStacks; i++)
        {
            float phi = MathF.PI * i / nStacks;
            float y = radius * MathF.Cos(phi);
            float r = radius * MathF.Sin(phi);
            for (int j = 0; j < nSlices; j++)
            {
                float theta = MathF.PI * 2 * j / nSlices;
                float x = r * MathF.Cos(theta);
                float z = r * MathF.Sin(theta);
                vertices.Add(new Vector3(x, y, z));
            }
        }

        uint v1 = (uint)vertices.Count;
        vertices.Add(new Vector3(0, -radius, 0));

        for (int i = 0; i < nSlices; i++)
        {
            uint i0 = (uint)(i + 1);
            uint i1 = (uint)((i + 1) % nSlices + 1);
            indices.AddRange([v0, i1, i0]);
            i0 = (uint)(i + nSlices * (nStacks - 2) + 1);
            i1 = (uint)((i + 1) % nSlices + nSlices * (nStacks - 2) + 1);
            indices.AddRange([v1, i0, i1]);
        }

        for (int j = 0; j < nStacks - 2; j++)
        {
            for (int i = 0; i < nSlices; i++)
            {
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
        for (int i = 0; i < vertices.Count; i++)
        {
            mesh.colors[i] = color;
        }
    }
}

/// <summary>
/// A 3D sphere entity.
/// </summary>
public partial class Sphere : MeshEntity3D, IColored
{
    /// <summary>
    /// Creates a new sphere with the given radius.
    /// </summary>
    public Sphere(float radius) : base()
    {
        _radiusP.Value = radius;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Created); // Id is only valid if the entity is created
        var state = new SphereState(NewMeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
