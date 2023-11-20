using System;

namespace AnimLib;

/// <summary>
/// Internal state of a <see cref="Shape"/>.
/// </summary>
internal class ShapeState : EntityState2D {
    public ShapePath path;
    public Color color = Color.RED;
    public Color contourColor = Color.BLACK;
    public float contourSize = 0.0f;
    public (float, float) trim = (0.0f, 1.0f);
    public ShapeMode mode = ShapeMode.FilledContour;

    public ShapeState(ShapePath path) {
        this.path = path;
    }

    public ShapeState(ShapeState ss) : base(ss) {
        this.path = ss.path.Clone();
        this.color = ss.color;
        this.contourColor = ss.contourColor;
        this.contourSize = ss.contourSize;
        this.mode = ss.mode;
        this.trim = ss.trim;
    }

    public override Vector2 AABB {
        get {
            throw new NotImplementedException();
        }
    }

    public override object Clone() {
        return new ShapeState(this);
    }
}

/// <summary>
/// A 2D shape made up of a contour and a fill.
/// </summary>
public class Shape : VisualEntity2D, IColored {
    internal Shape(ShapeState state) : base(state) {
    }

    /// <summary>
    /// Create a new shape from a path.
    /// </summary>
    public Shape(ShapePath path) : base(new ShapeState(path)) {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public Shape(Shape s) : base(s) {
    }

    /// <summary>
    /// The path of the shape.
    /// </summary>
    public ShapePath Path {
        get {
            return ((ShapeState)state).path;
        }
        set {
            World.current.SetProperty(this, "Path", value, ((ShapeState)state).path);
            ((ShapeState)state).path = value;
        }
    }

    /// <summary>
    /// The color of the shape. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Filled"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public Color Color {
        get {
            return ((ShapeState)state).color;
        }
        set {
            World.current.SetProperty(this, "Color", value, ((ShapeState)state).color);
            ((ShapeState)state).color = value;
        }
    }

    /// <summary>
    /// The color of the contour. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Contour"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public Color ContourColor {
        get {
            return ((ShapeState)state).contourColor;
        }
        set {
            World.current.SetProperty(this, "ContourColor", value, ((ShapeState)state).contourColor);
            ((ShapeState)state).contourColor = value;
        }
    }

    /// <summary>
    /// The size of the contour. Visible if <see cref="Mode"/> is <see cref="ShapeMode.Contour"/> or <see cref="ShapeMode.FilledContour"/>.
    /// </summary>
    public float ContourSize {
        get {
            return ((ShapeState)state).contourSize;
        }
        set {
            World.current.SetProperty(this, "ContourSize", value, ((ShapeState)state).contourSize);
            ((ShapeState)state).contourSize = value;
        }
    }

    /// <summary>
    /// The drawing mode of the shape.
    /// </summary>
    public ShapeMode Mode {
        get {
            return ((ShapeState)state).mode;
        }
        set {
            World.current.SetProperty(this, "Mode", value, ((ShapeState)state).mode);
            ((ShapeState)state).mode = value;
        }
    }

    /// <summary>
    /// Trim of the underlying path in range [0, 1].
    /// </summary>
    public (float, float) Trim {
        get {
            return ((ShapeState)state).trim;
        }
        set {
            World.current.SetProperty(this, "Trim", value, ((ShapeState)state).trim);
            ((ShapeState)state).trim = value;
        }
    }

    /// <summary>
    /// Clone the shape.
    /// </summary>
    public override object Clone() {
        return new Shape(this);
    }
}
