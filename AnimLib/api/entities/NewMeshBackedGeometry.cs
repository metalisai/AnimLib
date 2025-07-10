using System;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// The mode of a line.
/// </summary>
public enum MeshVertexMode {
    /// <summary>
    /// Each pair of vertices are a single line segment.
    /// </summary>
    Segments,
    /// <summary>
    /// The vertices are a continuous line strip.
    /// </summary>
    Strip,
    /// <summary>
    /// The vertices are trialngle list.
    /// </summary>
    Triangles,
};

internal abstract class NewMeshBackedGeometry : EntityState3D
{
    public BuiltinShader Shader = BuiltinShader.LineShader;
    public string UID = "";
    public abstract void GenerateMesh(ColoredTriangleMeshGeometry mesh);
    public int MeshVersion = 0;

    public static string GenerateEntityName(int entityId)
    {
        return "entity" + entityId.ToString();
    }

    public NewMeshBackedGeometry(string uid) : base()
    {

    }

    public virtual List<(string, object)> GetShaderProperties()
    {
        return [];
    }
}