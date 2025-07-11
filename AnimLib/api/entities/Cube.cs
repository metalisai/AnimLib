using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Cube))]
internal class CubeState : NewMeshBackedGeometry
{
    [Dyn]
    public Color color = Color.YELLOW;
    [Dyn]
    public Color outline = Color.BLACK;

    public CubeState(string uid) : base(uid)
    {
        this.Shader = BuiltinShader.CubeShader;
    }

    public CubeState(string uid, CubeState sls) : this(uid)
    {
        this.color = sls.color;
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
        };
        mesh.indices = new uint[] {
            0,1,2, 1,3,2, 0,4,1, 1,4,5, 2,7,6, 2,3,7, 1,7,3, 1,5,7, 4,2,6, 4,0,2, 5,6,7, 5,4,6
        };
        mesh.colors = Enumerable.Repeat(color, mesh.vertices.Length).ToArray();
        mesh.Dirty = true;
        mesh.edgeCoordinates = new Vector2[] {
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
            Vector2.ZERO,
        };
    }

    public override List<(string, object)> GetShaderProperties()
    {
        return [("_Outline", this.outline.ToVector4())];
    }
}

/// <summary>
/// A 3D cube.
/// </summary>
public partial class Cube : MeshEntity3D, IColored
{
    /// <summary>
    /// Creates a new cube object.
    /// </summary>
    public Cube() : base()
    {
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Created); // Id is only valid if the entity is created
        var state = new CubeState(NewMeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
