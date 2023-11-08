using System;
using System.Linq;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A single arc of a cubic bezier curve.
/// </summary>
public  struct CubicBezier
{
    /// <summary>
    /// Control point.
    /// </summary>
    public Vector2 p0, p1, p2, p3;
}

/// <summary>
/// A spline made up of cubic bezier curves.
/// </summary>
public class CubicSpline {
    internal class BezierNode {
        public BezierNode Left, Right;
        public CubicBezier Bezier;

        BezierNode(CubicBezier bezier)
        {
            this.Bezier = bezier;
        }

        internal static BezierNode MakeTree(CubicSpline cubics, CubicBezier dst)
        {
            var current = cubics.Arcs.Select(x => new BezierNode(x)).ToList();
            if (current.Count < 2)
            {
                throw new Exception("current.Count < 2");
            }
            while (true)
            {
                if (current.Count == 2)
                {
                    break;
                }

                var next = new List<BezierNode>();
                while (true)
                {
                    if (current.Count == 1)
                    {
                        next.Add(current[0]);
                        break;
                    }
                    else if (current.Count >= 2)
                    {
                        var left = current[0];
                        var right = current[1];
                        current.RemoveRange(0, 2);
                        var simplified = new CubicBezier();
                        simplified.p0 = left.Bezier.p0;
                        simplified.p1 = left.Bezier.p2;
                        simplified.p2 = right.Bezier.p1;
                        simplified.p3 = right.Bezier.p3;
                        var newN = new BezierNode(simplified);
                        newN.Left = left;
                        newN.Right = right;
                        next.Add(newN);
                    }
                }
                current = next;
            }
            var ret = new BezierNode(dst);
            ret.Left = current[0];
            ret.Right = current[1];
            return ret;
        }

        internal static void Evaluate(BezierNode root, float t, List<CubicBezier> ret)
        {
            var (p1, p2) = CollapsePair((root.Left.Bezier, root.Right.Bezier), root.Bezier, t);
            if (root.Left.Left == null && root.Left.Right == null)
            {
                ret.Add(p1);
            }
            else
            {
                root.Left.Bezier = p1;
                Evaluate(root.Left, t, ret);
            }
            if (root.Right.Left == null && root.Right.Right == null)
            {
                ret.Add(p2);
            }
            else
            {
                root.Right.Bezier = p2;
                Evaluate(root.Right, t, ret);
            }
        }
    }

    /// <summary>
    /// The cubic bezier curves that make up this spline.
    /// </summary>
    public List<CubicBezier> Arcs = new();
    /// <summary>
    /// The starting point of this spline.
    /// </summary>
    public Vector2 Start;
    /// <summary>
    /// Whether this spline is closed.
    /// </summary>
    public bool Closed;

    /// <summary>
    /// Empty constructor.
    /// </summary>
    public CubicSpline() {}

    /// <summary>
    /// Constructs a <c>CubicSpline</c> from a <c>ShapePath</c>. All verbs are converted to cubic bezier curves.
    /// </summary>
    public static CubicSpline[] FromShape(ShapePath src)
    {
        Vector2 pos = new(0.0f, 0.0f);
        List<CubicSpline> ret = new();
        CubicSpline current = new();
        bool started = false;

        foreach (var v in src.path) {
            switch (v.verb) {
                case PathVerb.Move:
                    if (started)
                    {
                        throw new Exception("Move after start");
                    }
                    current.Start = v.data.points[0];
                    pos = v.data.points[0];
                    break;
                case PathVerb.Line:
                    var dest = v.data.points[1];
                    var cp1 = Vector2.Lerp(pos, dest, 1.0f/3.0f);
                    var cp2 = Vector2.Lerp(pos, dest, 2.0f/3.0f);
                    var bezier = new CubicBezier();
                    bezier.p0 = pos;
                    bezier.p1 = cp1;
                    bezier.p2 = cp2;
                    bezier.p3 = dest;
                    current.Arcs.Add(bezier);
                    pos = dest;
                    break;
                case PathVerb.Quad:
                    var cp = v.data.points[1];
                    dest = v.data.points[2];
                    cp1 = Vector2.Lerp(pos, cp, 2.0f/3.0f);
                    cp2 = Vector2.Lerp(cp, dest, 1.0f/3.0f);
                    bezier = new CubicBezier();
                    bezier.p0 = pos;
                    bezier.p1 = cp1;
                    bezier.p2 = cp2;
                    bezier.p3 = dest;
                    current.Arcs.Add(bezier);
                    pos = dest;
                    break;
                case PathVerb.Cubic:
                    bezier = new CubicBezier();
                    bezier.p0 = pos;
                    bezier.p1 = v.data.points[1];
                    bezier.p2 = v.data.points[2];
                    bezier.p3 = v.data.points[3];
                    current.Arcs.Add(bezier);
                    pos = v.data.points[3];
                    break;
                case PathVerb.Conic:
                    cp = v.data.points[1];
                    dest = v.data.points[2];
                    cp1 = Vector2.Lerp(pos, cp, 2.0f/3.0f);
                    cp2 = Vector2.Lerp(cp, dest, 1.0f/3.0f);
                    bezier = new CubicBezier();
                    bezier.p0 = pos;
                    bezier.p1 = cp1;
                    bezier.p2 = cp2;
                    bezier.p3 = dest;
                    current.Arcs.Add(bezier);
                    pos = dest;
                    break;
                case PathVerb.Close:
                    current.Closed = true;
                    break;
                case PathVerb.Noop:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        return new CubicSpline[] { current };
    }

    internal static (CubicBezier c1, CubicBezier c2) CollapsePair((CubicBezier c1, CubicBezier c2) src, CubicBezier dst, float t)
    {
        var dst_c1p0 = dst.p0;
        var dst_c1p1 = (dst.p0 + dst.p1) * 0.5f;
        var dst_c1p2 = (dst.p0 + 2.0f*dst.p1 + dst.p2) * 0.25f;
        var dst_c1p3 = (dst.p0 + 3.0f*dst.p1 + 3.0f*dst.p2 + dst.p3) * 0.125f;
        var dst_c2p0 = dst_c1p3;
        var dst_c2p1 = (dst.p1 + 2.0f*dst.p2 + dst.p3) * 0.25f;
        var dst_c2p2 = (dst.p2 + dst.p3) * 0.5f;
        var dst_c2p3 = dst.p3;

        CubicBezier c1 = new CubicBezier();
        CubicBezier c2 = new CubicBezier();

        c1.p0 = Vector2.Lerp(src.c1.p0, dst_c1p0, t);
        c1.p1 = Vector2.Lerp(src.c1.p1, dst_c1p1, t);
        c1.p2 = Vector2.Lerp(src.c1.p2, dst_c1p2, t);
        c1.p3 = Vector2.Lerp(src.c1.p3, dst_c1p3, t);

        c2.p0 = Vector2.Lerp(src.c2.p0, dst_c2p0, t);
        c2.p1 = Vector2.Lerp(src.c2.p1, dst_c2p1, t);
        c2.p2 = Vector2.Lerp(src.c2.p2, dst_c2p2, t);
        c2.p3 = Vector2.Lerp(src.c2.p3, dst_c2p3, t);

        return (c1, c2);
    }

    /// <summary>
    /// Converts this <c>CubicSpline</c> into a <c>ShapePath</c>.
    /// </summary>
    public ShapePath ToShapePath()
    {
        var pb = new PathBuilder();
        pb.MoveTo(Start);
        foreach (var arc in Arcs)
        {
            pb.CubicTo(arc.p1, arc.p2, arc.p3);
        }
        if (Closed)
        {
            pb.Close();
        }
        return pb;
    }

    /// <summary>
    /// Morphs this <c>CubicSpline</c> into another <c>CubicSpline</c> given progress.
    /// </summary>
    public CubicSpline MorphTo(CubicSpline b, float t)
    {
        var a = this;
        var longer = a.Arcs.Count > b.Arcs.Count ? a : b;
        var shorter = a.Arcs.Count > b.Arcs.Count ? b : a;
        if (longer != a)
        {
            t = 1.0f - t;
        }
        var ret = new CubicSpline();
        ret.Arcs = new List<CubicBezier>();
        ret.Start = Vector2.Lerp(longer.Start, shorter.Start, t);
        ret.Closed = a.Closed && b.Closed;

        var arrA = a.Arcs;
        var arrB = b.Arcs;
        var lenA = a.Arcs.Count;
        var lenB = b.Arcs.Count;

        if (lenA == lenB)
        {
            for (int mi = 0; mi < lenA; mi++)
            {
                var interpolated = new CubicBezier();
                interpolated.p0 = Vector2.Lerp(arrA[mi].p0, arrB[mi].p0, t);
                interpolated.p1 = Vector2.Lerp(arrA[mi].p1, arrB[mi].p1, t);
                interpolated.p2 = Vector2.Lerp(arrA[mi].p2, arrB[mi].p2, t);
                interpolated.p3 = Vector2.Lerp(arrA[mi].p3, arrB[mi].p3, t);
                ret.Arcs.Add(interpolated);
            }
            return ret;
        }

        var longerLen = longer.Arcs.Count;
        var shorterLen = shorter.Arcs.Count;
        var longerArr = longer.Arcs;
        var shorterArr = shorter.Arcs;

        int wholeRatio = longerLen / shorterLen;
        int remainder = longerLen % shorterLen;

        int i = 0;
        int shorterIndex = 0;
        while (i < longerLen)
        {
            int count = wholeRatio + (remainder-- > 0 ? 1 : 0);
            List<int> longerIndices = new List<int>();
            for (int j = 0; j < count; j++)
            {
                longerIndices.Add(i++);
            }

            // no subdivision/collapsing
            if (longerIndices.Count == 1)
            {
                var interpolated = new CubicBezier();
                var arc1 = longerArr[longerIndices[0]];
                var arc2 = shorterArr[shorterIndex];
                interpolated.p0 = Vector2.Lerp(arc1.p0, arc2.p0, t);
                interpolated.p1 = Vector2.Lerp(arc1.p1, arc2.p1, t);
                interpolated.p2 = Vector2.Lerp(arc1.p2, arc2.p2, t);
                interpolated.p3 = Vector2.Lerp(arc1.p3, arc2.p3, t);
                ret.Arcs.Add(interpolated);
                shorterIndex++;
                continue;
            }

            var interpolatedList = new List<CubicBezier>();
            var subSpline = new CubicSpline() { Arcs = longerIndices.Select(x => longerArr[x]).ToList() };
            var indicesStr = longerIndices.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y);
            var tree = BezierNode.MakeTree(subSpline, shorterArr[shorterIndex]);
            BezierNode.Evaluate(tree, t, interpolatedList);
            foreach (var interpolated in interpolatedList)
            {
                ret.Arcs.Add(interpolated);
            }

            shorterIndex++;
        }
        if (ret.Arcs.Count != longer.Arcs.Count)
        {
            throw new Exception("retIndex != ret.Length");
        }
        return ret;
    }
}
