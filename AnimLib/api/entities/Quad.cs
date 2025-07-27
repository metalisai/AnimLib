using System;
using System.Collections.Generic;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Quad))]
internal class QuadState : MeshBackedGeometry
{
    [Dyn(onSet: ["MeshDirty"])]
    public Vector3[] vertices = new Vector3[4];
    [Dyn]
    public Color color;
    [Dyn]
    public Color outline;

    internal QuadState(string uid) : base(uid)
    {
        this.Shader = BuiltinShader.QuadShader;
    }

    public QuadState(string uid, QuadState qs) : this(uid)
    {
        for (int i = 0; i < 4; i++)
        {
            this.vertices[i] = qs.vertices[i];
        }
        this.color = qs.color;
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.vertices = vertices;
        mesh.colors = [color, color, color, color];
        mesh.indices = [0, 1, 2, 2, 3, 0];
    }

    public override List<(string, object)> GetShaderProperties()
    {
        return [("_Outline", this.outline.ToVector4())];
    }
}

/// <summary>
/// A 3D quad. For when you don't want that ugly edge in the middle.
/// </summary>
public partial class Quad : MeshEntity3D, IColored
{
    public Quad() : base()
    {
        
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Id >= 0);
        var state = new QuadState(MeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
