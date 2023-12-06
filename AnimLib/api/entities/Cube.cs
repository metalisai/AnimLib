namespace AnimLib;

internal class CubeState : EntityState3D
{
    public Color color = Color.WHITE;

    public CubeState() {
    }

    public CubeState(CubeState c) : base(c) {
        this.color = c.color;
    }

    public override object Clone()
    {
        return new CubeState(this);
    }
}

/// <summary>
/// A 3D cube.
/// </summary>
public class Cube : VisualEntity3D/*, ICloneable*/ {
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

    /// <summary>
    /// Create a new cube.
    /// </summary>
    public Cube() : base(new CubeState()) {
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
