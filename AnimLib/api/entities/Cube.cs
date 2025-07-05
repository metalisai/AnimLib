using System.Linq;

namespace AnimLib;

internal class CubeState : MeshBackedGeometry
{
    public Color color = Color.WHITE;
    internal bool dirty = true;

    public CubeState(string owner) : base(owner)
    {
        this.Shader = BuiltinShader.CubeShader;
    }

    public CubeState(RendererHandle h, string owner) : base(h, owner)
    {

    }

    public CubeState(CubeState c) : base(c)
    {
        this.color = c.color;
    }

    public override object Clone()
    {
        return new CubeState(this);
    }

    public override void UpdateMesh(ColoredTriangleMeshGeometry cubeGeometry)
    {
        cubeGeometry.Dirty = dirty;
        cubeGeometry.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
        };
        cubeGeometry.indices = new uint[] {
            0,1,2, 1,3,2, 0,4,1, 1,4,5, 2,7,6, 2,3,7, 1,7,3, 1,5,7, 4,2,6, 4,0,2, 5,6,7, 5,4,6
        };
        cubeGeometry.colors = Enumerable.Repeat(color, cubeGeometry.vertices.Length).ToArray();
        cubeGeometry.Dirty = true;
        cubeGeometry.edgeCoordinates = new Vector2[] {
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
}

/// <summary>
/// A 3D cube.
/// </summary>
public class Cube : VisualEntity3D, IColored {
    /// <summary> Color of the cube </summary>
    public Color Color { 
        get {
            return ((CubeState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((CubeState)state).color);
            ((CubeState)state).color = value;
        }
    }

    /// <summary> Outline color of the cube </summary>
    public Color Outline { 
        get {
            return ((CubeState)state).outline;
        }
        set {
            World.current.SetProperty(this, "Outline", value, ((CubeState)state).outline);
            ((CubeState)state).outline = value;
        }
    }
    
    public Cube(string owner) : base(new CubeState(owner)) {
    }

    /// <summary>
    /// Create a new sphere
    /// </summary>
    public Cube() : this(World.current.Resources.GetGuid()) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Cube(Cube cube) : base(cube) {
    }

    /// <summary>
    /// Clone this cube.
    /// </summary>
    public override object Clone() {
        return new Cube(this);
    }
}
