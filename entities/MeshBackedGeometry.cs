using System.Collections.Generic;

namespace AnimLib {
    public abstract class MeshBackedGeometry : EntityState3D/*, ICloneable*/, RendererResource {
        //public Transform2 Transform;
        //public Color Outline = Color.BLACK;
        //public float OutlineWidth = 0.0f;
        public RenderState.BuiltinShader Shader = RenderState.BuiltinShader.LineShader;

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
