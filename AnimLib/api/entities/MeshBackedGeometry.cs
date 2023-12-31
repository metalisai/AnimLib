using System.Collections.Generic;

namespace AnimLib {
    internal abstract class MeshBackedGeometry : EntityState3D, IRendererResource {
        public Color Outline = Color.BLACK;
        public BuiltinShader Shader = BuiltinShader.LineShader;

        public Dictionary<string, DynProperty> properties = new ();

        public readonly RendererHandle RendererHandle = new RendererHandle();
        /*public abstract object Clone();*/
        public abstract void UpdateMesh(ColoredTriangleMeshGeometry mesh);
        public List<(string, object)> shaderProperties = new List<(string, object)>();
        public string ownerGuid;

        public MeshBackedGeometry(string ownerGuid) {
            this.ownerGuid = ownerGuid;
        }

        public MeshBackedGeometry(MeshBackedGeometry mbg) : base(mbg) {
            this.ownerGuid = mbg.ownerGuid;
            this.Shader = mbg.Shader;
            this.shaderProperties = mbg.shaderProperties;
            this.properties = mbg.properties;
        }

        public string GetOwnerGuid() {
            return ownerGuid;
        }

        protected MeshBackedGeometry(RendererHandle handle, string ownerGuid) {
            RendererHandle = handle;
            this.ownerGuid = ownerGuid;
        }
    }

}
