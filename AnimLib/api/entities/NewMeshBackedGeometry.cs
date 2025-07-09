namespace AnimLib;

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
}