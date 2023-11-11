using SkiaSharp;

namespace AnimLib;

/// <summary>
/// Extension methods to convert between AnimLib and SkiaSharp types.
/// </summary>
public static class SkiaExtensions
{
    public static SKPoint ToSKPoint(this Vector2 v) {
        return new SKPoint(v.x, v.y);
    }

    public static Vector2 ToV2(this SKPoint v) {
        return new Vector2(v.X, v.Y);
    }

    public static SKColor ToSKColor(this Color c) {
        return new SKColor(c.r, c.g, c.b, c.a);
    }

    public static SKMatrix ToSKMatrix(this M3x3 m) {
        return new SKMatrix(m.m11, m.m12, m.m13, m.m21, m.m22, m.m23, m.m31, m.m32, m.m33);
    }

    public static ShapePath ToShapePath(this SKPath p) {
        var pathBuilder = new PathBuilder();
        var points = new SKPoint[4];
        var itr = p.CreateIterator(false); 
        SKPathVerb verb;
        while((verb = itr.Next(points)) != SKPathVerb.Done) {
            switch(verb) {
                case SKPathVerb.Move:
                    pathBuilder.MoveTo(points[0].ToV2());
                    break;
                case SKPathVerb.Line:
                    pathBuilder.LineTo(points[1].ToV2());
                    break;
                case SKPathVerb.Cubic:
                    pathBuilder.CubicTo(points[1].ToV2(), points[2].ToV2(), points[3].ToV2());
                    break;
                case SKPathVerb.Quad:
                    pathBuilder.QuadTo(points[1].ToV2(), points[2].ToV2());
                    break;
                case SKPathVerb.Conic:
                    pathBuilder.ConicTo(points[1].ToV2(), points[2].ToV2(), itr.ConicWeight());
                    break;
                case SKPathVerb.Close:
                    pathBuilder.Close();
                    break;
            }
        }
        return pathBuilder.Build();
    }

    public static SKPath ToSKPath(this ShapePath p) {
        var path = new SKPath();
        foreach(var verb in p.path) {
            switch(verb.Item1) {
                case PathVerb.Move:
                    path.MoveTo(verb.Item2.points[0].ToSKPoint());
                    break;
                case PathVerb.Line:
                    path.LineTo(verb.Item2.points[1].ToSKPoint());
                    break;
                case PathVerb.Quad:
                    path.QuadTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint());
                    break;
                case PathVerb.Cubic:
                    path.CubicTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint(), verb.Item2.points[3].ToSKPoint());
                    break;
                case PathVerb.Conic:
                    path.ConicTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint(), verb.Item2.conicWeight);
                    break;
                case PathVerb.Close:
                    path.Close();
                    break;
                default:
                    break;
            }
        }
        return path;
    }
}

