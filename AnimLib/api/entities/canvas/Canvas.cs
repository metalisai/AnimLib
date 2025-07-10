using System;

namespace AnimLib;

[GenerateDynProperties(forType: typeof(Canvas))]
internal class CanvasState : EntityState3D
{

    public static string DEFAULTNAME = "_defaultCanvas";

    [Dyn]
    public Vector3 center;
    [Dyn]
    public Vector3 normal;
    [Dyn]
    public Vector3 up;
    [Dyn]
    public float width;
    [Dyn]
    public float height;
    [Dyn]
    public bool is2d = false;
    [Dyn]
    public Vector2 anchor;
    [Dyn]
    public string name;
    [Dyn]
    public CanvasEffect[] effects = [];

    public CanvasState(string name)
    {
        this.name = name;
    }

    public CanvasState(CanvasState cs) : base(cs)
    {
        this.center = cs.center;
        this.normal = cs.normal;
        this.up = cs.up;
        this.width = cs.width;
        this.height = cs.height;
        this.is2d = cs.is2d;
        this.anchor = cs.anchor;
        this.name = cs.name;
    }

    public override object Clone()
    {
        return new CanvasState(this);
    }

    public M4x4 WorldToNormalizedCanvas
    {
        get
        {
            var mat = NormalizedCanvasToWorld.InvertedHomogenous;
            return mat;
        }
    }

    public Plane Surface
    {
        get
        {
            return new Plane(normal, center);
        }
    }

    // normalized coordinates -0.5..0.5
    public M4x4 NormalizedCanvasToWorld
    {
        get
        {
            // TODO: cache
            if (!is2d)
            {
                // right vector
                var c1 = new Vector4(width * Vector3.Cross(this.normal, this.up), 0.0f);
                // up vector
                var c2 = new Vector4(height * this.up, 0.0f);
                // forward vector
                var c3 = new Vector4(-this.normal, 0.0f);
                // translation
                var c4 = new Vector4(this.center, 1.0f);
                return M4x4.FromColumns(c1, c2, c3, c4);
            }
            else
            {
                var c1 = new Vector4(width * Vector3.Cross(this.normal, this.up), 0.0f);
                var c2 = new Vector4(height * this.up, 0.0f);
                var c3 = new Vector4(-this.normal, 0.0f);
                var c4 = new Vector4(this.center, 1.0f);
                var mat = M4x4.FromColumns(c1, c2, c3, c4);
                return mat;
            }
        }
    }

    // oriented world coordinates (x - left, y - up, z - forward)
    public M4x4 CanvasToWorld
    {
        get
        {
            // TODO: cache
            var anchorWorld = NormalizedCanvasToWorld * new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
            var c1 = new Vector4(Vector3.Cross(this.normal, this.up), 0.0f);
            var c2 = new Vector4(this.up, 0.0f);
            var c3 = new Vector4(-this.normal, 0.0f);
            return M4x4.FromColumns(c1, c2, c3, anchorWorld);
        }
    }
}

/// <summary>
/// A canvas is a 2D plane in 3D space. It is defined by a center, a normal, an up vector, and a size. 2D objects are always created on a canvas.
/// </summary>
public partial class Canvas : DynVisualEntity3D {
    [ThreadStatic]
    static Canvas? _default;

    /// <summary>
    /// The default canvas. Uses screen coordinates and is rendered directly on screen.
    /// </summary>
    public static Canvas? Default {
        get {
            return _default;
        }

        internal set {
            _default = value;
        }
    }

    /// <summary>
    /// Add a post effect to this canvas.
    /// </summary>
    public void AddEffect(CanvasEffect effect) {
        var newArr = new CanvasEffect[Effects.Length+1];
        Array.Copy(Effects, newArr, Effects.Length);
        newArr[Effects.Length] = effect;
        Effects = newArr;
    }

    /// <summary>
    /// Remove all post effects from this canvas.
    /// </summary>
    public void ClearEffects() {
        Effects = [];
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator)
    {
        var state = new CanvasState(this.Name);
        GetState(state, evaluator);
        return state;
    }

    /// <summary>
    /// Create a canvas with the given name given a ortho camera.
    /// </summary>
    public Canvas(string name, OrthoCamera cam) : base() {
        Width = cam.Width;
        Height = cam.Height;
        Center = Vector3.ZERO;
        Up = Vector3.UP;
        Normal = -Vector3.FORWARD;
        Is2d = true;
    }

    /// <summary>
    /// Create a canvas.
    /// identity - x is width, y is height, z is flat/depth
    /// </summary>
    public Canvas(string name, Vector3 center, Vector3 up, Vector3 normal, Vector2 size) : base(){
        Center = center;
        Up = up;
        Normal = normal;
        Width = size.x;
        Height = size.y;
    }

    /// <summary>
    /// Create a new 2D canvas.
    /// </summary>
    public Canvas(string name, Vector2 center, Vector2 size) : base() {
        Center = new Vector3(center.x, center.y, 0.0f);
        Up = Vector3.UP;
        Normal = -Vector3.FORWARD;
        Width = size.x;
        Height = size.y;
        Is2d = true;
    }

    /// <summary>
    /// A matrix that transforms from normalized canvas coordinates to world coordinates.
    /// </summary>
    /*public M4x4 CanvasToWorld {
        get {
            return ((CanvasState)state).NormalizedCanvasToWorld;
        }
    }*/
}
