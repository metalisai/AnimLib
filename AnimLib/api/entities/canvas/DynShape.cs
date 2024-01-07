using System;

namespace AnimLib;

/// <summary>
/// A shape defined by path.
/// </summary>
public class DynShape : DynVisualEntity2D {
    protected ShapePath path;

    private protected DynProperty<ShapePath> PathP = DynProperty<ShapePath>.CreateEmpty(new ShapePath());
    public DynProperty<ShapePath> Path {
        get {
            return PathP;
        }

        set {
            PathP.Value = value.Value;
        }
    }

    private protected DynProperty<Color> FillColorP = DynProperty<Color>.CreateEmpty(Color.RED);
    public DynProperty<Color> FillColor {
        get {
            return FillColorP;
        }

        set {
            FillColor.Value = value.Value;
        }
    }

    private protected DynProperty<Color> ContourColorP = DynProperty<Color>.CreateEmpty(Color.BLACK);
    public DynProperty<Color> ContourColor {
        get {
            return ContourColorP;
        }

        set {
            ContourColor.Value = value.Value;
        }
    }

    private protected DynProperty<float> ContourSizeP = DynProperty<float>.CreateEmpty(1.0f);
    public DynProperty<float> ContourSize {
        get {
            return ContourSizeP;
        }

        set {
            ContourSize.Value = value.Value;
        }
    }

    private protected DynProperty<ShapeMode> ModeP = DynProperty<ShapeMode>.CreateEmpty(ShapeMode.FilledContour);
    public DynProperty<ShapeMode> Mode {
        get {
            return ModeP;
        }

        set {
            Mode.Value = value.Value;
        }
    }

    private protected DynProperty<(float, float)> TrimP = DynProperty<(float, float)>.CreateEmpty((0.0f, 1.0f));
    public DynProperty<(float, float)> Trim {
        get {
            return TrimP;
        }

        set {
            Trim.Value = value.Value;
        }
    }

    internal DynShape(DynShape other) : base(other) {
        this.path = other.path;
        this.PathP.Value = other.PathP.Value;
        this.FillColorP.Value = other.FillColorP.Value;
        this.ContourColorP.Value = other.ContourColorP.Value;
        this.ContourSizeP.Value = other.ContourSizeP.Value;
        this.ModeP.Value = other.ModeP.Value;
        this.TrimP.Value = other.TrimP.Value;
    }

    public DynShape(ShapePath path) {
        this.path = path;
    }

    private protected void GetState(ShapeState shape, Func<DynPropertyId, object?> evaluator) {
        base.GetState(shape, evaluator);
        shape.path = evaluator(PathP.Id) as ShapePath ?? throw new Exception("Path is null");
        shape.color = evaluator(FillColorP.Id) as Color? ?? default(Color);
        shape.contourColor = evaluator(ContourColorP.Id) as Color? ?? default(Color);
        shape.contourSize = evaluator(ContourSizeP.Id) as float? ?? default(float);
        shape.mode = evaluator(ModeP.Id) as ShapeMode? ?? default(ShapeMode);
        shape.trim = evaluator(TrimP.Id) as (float, float)? ?? (0.0f, 1.0f);
    }

    internal override object GetState(Func<DynPropertyId, object?> evaluator) {
        var shape = new ShapeState(Path.Value ?? new ShapePath());
        base.GetState(shape, evaluator);
        return shape;
    }

    internal override void OnCreated() {
        base.OnCreated();
        PathP = new DynProperty<ShapePath>("path", path);
        FillColorP = new DynProperty<Color>("fillColor", this.FillColorP.Value);
        ContourColorP = new DynProperty<Color>("contourColor", this.ContourColorP.Value);
        ContourSizeP = new DynProperty<float>("contourSize", this.ContourSizeP.Value);
        ModeP = new DynProperty<ShapeMode>("mode", this.ModeP.Value);
        TrimP = new DynProperty<(float, float)>("trim", this.TrimP.Value);
    }

    internal override object Clone() {
        return new DynShape(this);
    }
}


