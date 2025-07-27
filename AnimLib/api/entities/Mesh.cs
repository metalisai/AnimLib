using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Mesh))]
internal class MeshState : MeshBackedGeometry
{
    [Dyn]
    public Vector3[] vertices = [];
    [Dyn]
    public uint[] indices = [];
    [Dyn]
    public Color color;
    [Dyn]
    public Color outline;

    internal MeshState(string uid) : base(uid)
    {
        this.Shader = BuiltinShader.MeshShader;
    }

    public MeshState(string uid, MeshState ms) : this(uid)
    {
        this.vertices = ms.vertices.ToArray();
        this.indices = ms.indices.ToArray();
        this.color = ms.color;
        this.outline = ms.outline;
    }

    public override string? GenerateCacheKey()
    {
        return null; // not reusable
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.vertices = vertices;
        mesh.indices = indices;
        mesh.colors = vertices.Select(x => color).ToArray();
    }

    public override List<(string, object)> GetShaderProperties()
    {
        return [("_Outline", this.outline.ToVector4())];
    }
}

/// <summary>
/// A 3D triangle mesh.
/// </summary>
public partial class Mesh : MeshEntity3D, IColored
{
    public Mesh() : base()
    {
    }
    
    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Id >= 0);
        var state = new MeshState(MeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
