namespace AnimLib;

internal class MorphShapeState : EntityState2D {
    public float progress = 0.0f;
    public ShapePath shape1;
    public ShapePath shape2;
    public Color color1 = Color.RED;
    public Color color2 = Color.RED;
    public Color contourColor1 = Color.BLACK;
    public Color contourColor2 = Color.BLACK;
    public float contourSize1 = 0.0f;
    public float contourSize2 = 0.0f;
    public ShapeMode mode1 = ShapeMode.FilledContour;
    public ShapeMode mode2 = ShapeMode.FilledContour;

    public MorphShapeState(ShapePath shape1, ShapePath shape2,
        Color color1, Color color2,
        Color contourColor1, Color contourColor2,
        float contourSize1, float contourSize2,
        ShapeMode mode1, ShapeMode mode2
    ) : base() {
        this.shape1 = shape1;
        this.shape2 = shape2;
        this.color1 = color1;
        this.color2 = color2;
        this.contourColor1 = contourColor1;
        this.contourColor2 = contourColor2;
        this.contourSize1 = contourSize1;
        this.contourSize2 = contourSize2;
        this.mode1 = mode1;
        this.mode2 = mode2;
    }

    public MorphShapeState(MorphShapeState rs) : base(rs) {
        this.progress = rs.progress;
        this.shape1 = rs.shape1;
        this.shape2 = rs.shape2;
        this.color1 = rs.color1;
        this.color2 = rs.color2;
        this.contourColor1 = rs.contourColor1;
        this.contourColor2 = rs.contourColor2;
        this.contourSize1 = rs.contourSize1;
        this.contourSize2 = rs.contourSize2;
        this.mode1 = rs.mode1;
        this.mode2 = rs.mode2;
    }

    public override Vector2 AABB {
        get {
            throw new System.NotImplementedException();
        }
    }

    public override object Clone()
    {
        return new MorphShapeState(this);
    }

    private static Vector2 AveragePoint(Vector2[] points) {
        var sum = new Vector2(0.0f, 0.0f);
        foreach(var p in points) {
            sum += p;
        }
        return (1.0f / points.Length) * sum;
    }

    private static int MinimalMoveOffset(Vector2[] a, Vector2[] b) {
        if (a.Length != b.Length) {
            throw new System.ArgumentException("Arrays must be of equal length");
        }
        var massPoint1 = AveragePoint(a);
        var massPoint2 = AveragePoint(b);
        var offset = massPoint2 - massPoint1; // shape a -> shape b
        int bestOffset = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < a.Length; i++) {
            float sum = 0.0f;
            for (int j = 0; j < a.Length; j++) {
                int idx1 = j;
                int idx2 = (j + i) % a.Length;
                var v1 = a[idx1];
                var v2 = b[idx2] + offset;
                var dif = v1 - v2;
                sum += Vector2.Dot(dif, dif); // square distance
            }
            if (sum < bestDistance) {
                bestDistance = sum;
                bestOffset = i;
            }
        }
        return bestOffset;
    }

    /// <summary>
    /// Morphs a <c>ShapePath</c> into another <c>ShapePath</c> given progress.
    /// </summary>
    public ShapePath MorphLinear()
    {
        var startPath = shape1;
        var endPath = shape2;
        var t = progress;

        int segments = 100;
        var startLinear = startPath.Linearize(segments)[0];
        var endLinear = endPath.Linearize(segments)[0];

        // not allowing transitions from open to closed shapes for now
        if (startLinear.closed != endLinear.closed) {
            throw new System.ArgumentException("Both shapes must have the same number of close verbs");
        }

        var ignoreVerb = (PathVerb x) => x == PathVerb.Close || x == PathVerb.Move;
        var minOffset = MinimalMoveOffset(startLinear.points, endLinear.points);

        var pathBuilder = new PathBuilder();
        var start0 = startLinear.points[0];
        var end0 = endLinear.points[minOffset];
        var p0 = Vector2.Lerp(start0, end0, t);
        pathBuilder.MoveTo(p0);
        for(int i = 1; i < startLinear.points.Length; i++) {
            var p1 = startLinear.points[i];
            var p2 = endLinear.points[(i+minOffset)%endLinear.points.Length];
            var p = Vector2.Lerp(p1, p2, t);
            pathBuilder.LineTo(p);
        }
        if (endLinear.closed) {
            pathBuilder.Close();
        }
        return pathBuilder.GetPath();
    }

    internal bool HasBody(ShapeMode mode) {
        return mode == ShapeMode.Filled || mode == ShapeMode.FilledContour;
    }

    internal bool HasContour(ShapeMode mode) {
        return mode == ShapeMode.Contour || mode == ShapeMode.FilledContour;
    }

    public Color CurrentColor {
        get {
            var a1 = color1.ToVector4().w;
            var a2 = color2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasBody(mode1) && !HasBody(mode2)) {
                alpha *= 1.0f - progress;
            } else if (!HasBody(mode1) && HasBody(mode2)) {
                alpha *= progress;
            } 
            var col = Color.LerpHSV(color1, color2, progress);
            var rgb = col.WithA((byte)(alpha*255));
            return rgb;
        }
    }

    public Color CurrentContourColor {
        get {
            var a1 = contourColor1.ToVector4().w;
            var a2 = contourColor2.ToVector4().w;
            float alpha = a1 + (a2 - a1) * progress;
            if (HasContour(mode1) && !HasContour(mode2)) {
                alpha *= 1.0f - progress;
            } else if (!HasContour(mode1) && HasContour(mode2)) {
                alpha *= progress;
            } 
            var col = Color.LerpHSV(contourColor1, contourColor2, progress);
            var rgb = col.WithA((byte)(alpha*255));
            return rgb;
        }
    }

    public ShapeMode CurrentMode {
        get {
            if (progress <= 0.0f) {
                return mode1;
            } else if (progress >= 1.0f) {
                return mode2;
            } else {
                bool needBody = HasBody(mode1) || HasBody(mode2);
                bool needContour = HasContour(mode1) || HasContour(mode2);
                if (needBody && needContour) {
                    return ShapeMode.FilledContour;
                } else if (needBody) {
                    return ShapeMode.Filled;
                } else if (needContour) {
                    return ShapeMode.Contour;
                } else {
                    return ShapeMode.None;
                }
            }
        }
    }

    public float CurrentContourSize {
        get {
            return contourSize1 + (contourSize2 - contourSize1) * progress;
        }
    }
}

/// <summary>
/// A morph shape between two shapes.
/// </summary>
public class MorphShape : VisualEntity2D {

    public MorphShape(Shape a, Shape b) : base(new MorphShapeState(a.Path, b.Path, a.Color, b.Color, a.ContourColor, b.ContourColor, a.ContourSize, b.ContourSize, a.Mode, b.Mode)) {
    }

    public MorphShape(MorphShape ms) : base(ms) {
    }

    public float Progress {
        get {
            return ((MorphShapeState)state).progress;
        }
        set {
            World.current.SetProperty(this, "Progress", value, ((MorphShapeState)state).progress);
            ((MorphShapeState)state).progress = value;
        }
    }

    public override object Clone() {
        return new MorphShape(this);
    }
}
