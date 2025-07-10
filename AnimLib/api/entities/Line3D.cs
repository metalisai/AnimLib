using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Line3D))]
internal class Line3DState : NewMeshBackedGeometry
{
    [Dyn(onSet: ["MeshDirty"])]
    internal MeshVertexMode vertexMode = MeshVertexMode.Strip;
    [Dyn]
    public float width;
    [Dyn(onSet: ["MeshDirty"])]
    public Vector3[] vertices = [];
    [Dyn]
    public Color color = Color.BLACK;
    [Dyn(onSet: ["MeshDirty"])]
    public Color[] colors = [];

    public Line3DState(string uid) : base(uid)
    {
    }

    public Line3DState(string uid, Line3DState sls) : this(uid)
    {
        this.vertexMode = sls.vertexMode;
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.vertexMode = vertexMode;
        mesh.vertices = vertices;
        if (colors.Length > 0 && colors.Length == vertices.Length)
        {
            mesh.colors = colors;
        }
        else
        {
            if (colors.Length > 0)
            {
                Debug.Error("Line3DState: colors.Length != vertices.Length");
            }
            mesh.colors = vertices.Select(x => color).ToArray();
        }
    }

    public override List<(string, object)> GetShaderProperties()
    {
        return [("Width", this.width)];
    }
}

/// <summary>
/// A 3D triangle mesh.
/// </summary>
public partial class Line3D : MeshEntity3D, IColored
{
    /// <summary>
    /// Creates a new Line3D.
    /// </summary>
    public Line3D(float width = 1.0f, MeshVertexMode mode = MeshVertexMode.Segments)
    {
        VertexMode = mode;
        Width = width;
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Created); // Id is only valid if the entity is created
        var state = new Line3DState(NewMeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
