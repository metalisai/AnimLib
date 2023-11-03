using System.Linq;

namespace AnimLib {
    internal class MeshState : MeshBackedGeometry
    {
        bool dirty = true;
        Vector3[] _vertices;
        public Vector3[] vertices {
            get {
                return _vertices;
            }
            set {
                _vertices = value;
                dirty = true;
            }
        }
        uint[] _indices;
        public uint[] indices {
            get {
                return _indices;
            }
            set {
                _indices = value;
                dirty = true;
            }
        }
        Color _color;
        public Color color {
            get {
                return _color;
            } 
            set {
                _color = value;
                dirty = true;
            }
        }

        public MeshState(string owner) : base(owner) {
            this.Shader = BuiltinShader.MeshShader;
        }

        public MeshState(MeshState ms) : base(ms) {
            this.vertices = ms.vertices.ToArray();
            this.indices = ms.indices.ToArray();
            this.color = ms.color;
        }

        public override object Clone() {
            return new MeshState(this);
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh) {
            mesh.Dirty = dirty;
            if(dirty) {
                mesh.vertices = vertices;
                mesh.indices = indices;
                mesh.colors = vertices.Select(x => color).ToArray();
                dirty = false;
            }
        }
    }
    public class Mesh : VisualEntity3D
    {
        public Color Color {
            get {
                return ((MeshState)state).color;
            }
            set {
                World.current.SetProperty(this, "StartPoint", value, ((MeshState)state).color);
                ((MeshState)state).color = value;
            }
        }

        public Vector3[] Vertices {
            get {
                return ((MeshState)state).vertices;
            }
            set {
                World.current.SetProperty(this, "Vertices", value, ((MeshState)state).vertices);
                ((MeshState)state).vertices = value;
            }
        }

        public uint[] Indices {
            get {
                return ((MeshState)state).indices;
            }
            set {
                World.current.SetProperty(this, "Indices", value, ((MeshState)state).indices);
                ((MeshState)state).indices = value;
            }
        }

        public Mesh(string owner) : base(new MeshState(owner)) {
        }

        public Mesh() : this(World.current.Resources.GetGuid()) {
        }

        public Mesh(Mesh mesh) : base(mesh) {
        }

        public override object Clone() {
            return new Mesh(this);
        }
    }
}
