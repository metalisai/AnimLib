using System;

namespace AnimLib;

internal class CanvasState : EntityState3D {
    
    public static string DEFAULTNAME = "_defaultCanvas";

    public Vector3 center, normal, up;
    public float width, height;
    public bool is2d = false;
    public Vector2 anchor;
    public string name;
    public CanvasEffect[] effects = Array.Empty<CanvasEffect>();

    public CanvasState(string name) {
        this.name = name;
    }

    public CanvasState(CanvasState cs) : base(cs) {
        this.center = cs.center;
        this.normal = cs.normal;
        this.up = cs.up;
        this.width = cs.width;
        this.height = cs.height;
        this.is2d = cs.is2d;
        this.anchor = cs.anchor;
        this.name = cs.name;
    }

    public override object Clone() {
        return new CanvasState(this);
    }

    public M4x4 WorldToNormalizedCanvas {
        get {
            var mat = NormalizedCanvasToWorld.InvertedHomogenous;
            return mat;
        }
    }

    public Plane Surface {
        get {
            return new Plane(normal, center);
        }
    }

    // normalized coordinates -0.5..0.5
    public M4x4 NormalizedCanvasToWorld {
        get {
            // TODO: cache
            if(!is2d) {
                // right vector
                var c1 = new Vector4(width*Vector3.Cross(this.normal, this.up), 0.0f);
                // up vector
                var c2 = new Vector4(height*this.up, 0.0f);
                // forward vector
                var c3 = new Vector4(-this.normal, 0.0f);
                // translation
                var c4 = new Vector4(this.center, 1.0f);
                return M4x4.FromColumns(c1, c2, c3, c4);
            } else {
                var c1 = new Vector4(width*Vector3.Cross(this.normal, this.up), 0.0f);
                var c2 = new Vector4(height*this.up, 0.0f);
                var c3 = new Vector4(-this.normal, 0.0f);
                var c4 = new Vector4(this.center, 1.0f);
                var mat = M4x4.FromColumns(c1, c2, c3, c4);
                return mat;
            }
        }
    }

    // oriented world coordinates (x - left, y - up, z - forward)
    public M4x4 CanvasToWorld {
        get {
            // TODO: cache
            var anchorWorld = NormalizedCanvasToWorld*new Vector4(anchor.x, anchor.y, 0.0f, 1.0f);
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
public class Canvas : VisualEntity3D {
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
    /// The width of the canvas.
    /// </summary>
    public float Width {
        get {
            return ((CanvasState)state).width;
        }
        set {
            World.current.SetProperty(this, "Width", value, ((CanvasState)state).width);
            ((CanvasState)state).width = value;
        }
    }

    /// <summary>
    /// The height of the canvas.
    /// </summary>
    public float Height {
        get {
            return ((CanvasState)state).height;
        }
        set {
            World.current.SetProperty(this, "Height", value, ((CanvasState)state).height);
            ((CanvasState)state).height = value;
        }
    }
    
    /// <summary>
    /// The center of the canvas.
    /// </summary>
    public Vector3 Center {
        get {
            return ((CanvasState)state).center;
        }
    }

    /// <summary>
    /// The normal vector of the canvas.
    /// </summary>
    public Vector3 Normal {
        get {
            return ((CanvasState)state).normal;
        }
    }

    /// <summary>
    /// The effects currently applied to this canvas.
    /// </summary>
    public CanvasEffect[] Effects {
        get {
            return ((CanvasState)state).effects;
        }
        internal set {
            Debug.TLog($"Setting effects on canvas {((CanvasState)state).name} to {value.Length}");
            World.current.SetProperty(this, "Effects", value, ((CanvasState)state).effects);
            ((CanvasState)state).effects = value;
        }
    }

    /// <summary>
    /// Add a post effect to this canvas.
    /// </summary>
    public void AddEffect(CanvasEffect effect) {
        var cs = (CanvasState)state;
        var newArr = new CanvasEffect[cs.effects.Length+1];
        Array.Copy(cs.effects, newArr, cs.effects.Length);
        newArr[cs.effects.Length] = effect;
        Effects = newArr;
    }

    /// <summary>
    /// Remove all post effects from this canvas.
    /// </summary>
    public void ClearEffects() {
        Effects = new CanvasEffect[0];
    }

    /// <summary>
    /// Create a canvas with the given name given a ortho camera.
    /// </summary>
    public Canvas(string name, OrthoCamera cam) : base(new CanvasState(name)) {
        var cs = (CanvasState)state;
        cs.width = cam.Width;
        cs.height = cam.Height;
        cs.center = Vector3.ZERO;
        cs.up = Vector3.UP;
        cs.normal = -Vector3.FORWARD;
        cs.is2d = true;
    }

    /// <summary>
    /// Create a canvas.
    /// identity - x is width, y is height, z is flat/depth
    /// </summary>
    public Canvas(string name, Vector3 center, Vector3 up, Vector3 normal, Vector2 size) : base(new CanvasState(name)){
        var cs = (CanvasState)state;
        cs.center = center;
        cs.up = up;
        cs.normal = normal;
        cs.width = size.x;
        cs.height = size.y;
    }

    /// <summary>
    /// Create a new 2D canvas.
    /// </summary>
    public Canvas(string name, Vector2 center, Vector2 size) : base(new CanvasState(name)) {
        var cs = (CanvasState)state;
        cs.center = new Vector3(center.x, center.y, 0.0f);
        cs.up = Vector3.UP;
        cs.normal = -Vector3.FORWARD;
        cs.width = size.x;
        cs.height = size.y;
        cs.is2d = true;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Canvas(Canvas c) : base(c) {
    }

    /// <summary>
    /// A matrix that transforms from normalized canvas coordinates to world coordinates.
    /// </summary>
    public M4x4 CanvasToWorld {
        get {
            return ((CanvasState)state).NormalizedCanvasToWorld;
        }
    }

    /// <summary>
    /// Clone this canvas.
    /// </summary>
    public override object Clone() {
        return new Canvas(this);
    }
}
