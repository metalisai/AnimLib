using System;
using System.Collections.Generic;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(TexRect))]
internal class TexRectState : NewMeshBackedGeometry
{
    [Dyn]
    public Texture2D texture;

    public TexRectState(Texture2D texture, string uid) : base(uid)
    {
        this.texture = texture;
        this.Shader = BuiltinShader.TexturedQuadShader;
    }

    public TexRectState(string uid, TexRectState trs) : base(uid)
    {
        this.texture = trs.texture;
        this.Shader = BuiltinShader.TexturedQuadShader;
    }

    public override void GenerateMesh(ColoredTriangleMeshGeometry mesh)
    {
        mesh.vertexMode = MeshVertexMode.Triangles;
        mesh.vertices = [
            new Vector3(-1.0f, -1.0f, 0.0f),
            new Vector3( 1.0f, -1.0f, 0.0f),
            new Vector3( 1.0f,  1.0f, 0.0f),
            new Vector3(-1.0f,  1.0f, 0.0f),
        ];
        mesh.colors = [Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE];
        mesh.indices = [0, 1, 2, 2, 3, 0];
        mesh.edgeCoordinates = [
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f)
        ];
    }

    public override List<(string, object)> GetShaderProperties()
    {
        return [("_MainTex", this.texture)];
    }
}

public partial class TexRect : MeshEntity3D
{
    public TexRect(Texture2D texture)
    {
        this._textureP.Value = texture;
    }
    
    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        Debug.Assert(this.Id >= 0);
        var state = new TexRectState(this.Texture, NewMeshBackedGeometry.GenerateEntityName(this.Id));
        GetState(state, evaluator);
        return state;
    }
}
