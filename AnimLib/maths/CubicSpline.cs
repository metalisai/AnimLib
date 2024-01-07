using System;
using System.Linq;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// A spline made up of cubic bezier curves.
/// </summary>
public class CubicSpline {
    internal class BezierNode {
        public BezierNode? Left, Right;
        public CubicBezier<Vector2, float> Bezier;

        BezierNode(CubicBezier<Vector2, float> bezier)
        {
            this.Bezier = bezier;
        }

        internal static BezierNode MakeTree(CubicSpline cubics, CubicBezier<Vector2, float> dst)
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
                    if (current.Count <= 0)
                    {
                        break;
                    }
                    else if (current.Count == 1)
                    {
                        next.Add(current[0]);
                        break;
                    }
                    else if (current.Count >= 2)
                    {
                        var left = current[0];
                        var right = current[1];
                        current.RemoveRange(0, 2);
                        var simplified = new CubicBezier<Vector2, float>(left.Bezier.p0, left.Bezier.p2, right.Bezier.p1, right.Bezier.p3);
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

        internal static void Evaluate(BezierNode root, float t, List<CubicBezier<Vector2, float>> ret)
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
    public List<CubicBezier<Vector2, float>> Arcs = new();
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
    /// Clone this <c>CubicSpline</c>.
    /// </summary>
    public CubicSpline Clone()
    {
        var ret = new CubicSpline();
        ret.Arcs = Arcs.Select(x => new CubicBezier<Vector2, float>(x.p0, x.p1, x.p2, x.p3)).ToList();
        ret.Start = Start;
        ret.Closed = Closed;
        return ret;
    }

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
                        ret.Add(current);
                        current = new CubicSpline();
                    }
                    current.Start = v.data.points[0];
                    pos = v.data.points[0];
                    started = true;
                    break;
                case PathVerb.Line:
                    var dest = v.data.points[1];
                    var cp1 = Vector2.Lerp(pos, dest, 1.0f/3.0f);
                    var cp2 = Vector2.Lerp(pos, dest, 2.0f/3.0f);
                    var bezier = new CubicBezier<Vector2, float>(pos, cp1, cp2, dest);
                    current.Arcs.Add(bezier);
                    pos = dest;
                    break;
                case PathVerb.Quad:
                    var cp = v.data.points[1];
                    dest = v.data.points[2];
                    cp1 = Vector2.Lerp(pos, cp, 2.0f/3.0f);
                    cp2 = Vector2.Lerp(cp, dest, 1.0f/3.0f);
                    bezier = new CubicBezier<Vector2, float>(pos, cp1, cp2, dest);
                    current.Arcs.Add(bezier);
                    pos = dest;
                    break;
                case PathVerb.Cubic:
                    bezier = new CubicBezier<Vector2, float>(pos, v.data.points[1], v.data.points[2], v.data.points[3]);
                    current.Arcs.Add(bezier);
                    pos = v.data.points[3];
                    break;
                case PathVerb.Conic:
                    cp = v.data.points[1];
                    dest = v.data.points[2];
                    // conver rational quadratic to cubic
                    // use the weight to determine the control point
                    float w = v.data.conicWeight;
                    cp1 = v.data.points[0] + w*(cp - v.data.points[0]);
                    cp2 = dest + (1.0f-w)*(cp - dest);
                    bezier = new CubicBezier<Vector2, float>(pos, cp1, cp2, dest);
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
        if (current.Arcs.Count > 0)
        {
            ret.Add(current);
        }
        return ret.ToArray();
    }

    internal static (CubicBezier<Vector2,float> c1, CubicBezier<Vector2, float> c2) CollapsePair((CubicBezier<Vector2, float> c1, CubicBezier<Vector2, float> c2) src, CubicBezier<Vector2, float> dst, float t)
    {
        var dst_c1p0 = dst.p0;
        var dst_c1p1 = (dst.p0 + dst.p1) * 0.5f;
        var dst_c1p2 = (dst.p0 + 2.0f*dst.p1 + dst.p2) * 0.25f;
        var dst_c1p3 = (dst.p0 + 3.0f*dst.p1 + 3.0f*dst.p2 + dst.p3) * 0.125f;
        var dst_c2p0 = dst_c1p3;
        var dst_c2p1 = (dst.p1 + 2.0f*dst.p2 + dst.p3) * 0.25f;
        var dst_c2p2 = (dst.p2 + dst.p3) * 0.5f;
        var dst_c2p3 = dst.p3;

        CubicBezier<Vector2, float> c1 = new (
            Vector2.Lerp(src.c1.p0, dst_c1p0, t),
            Vector2.Lerp(src.c1.p1, dst_c1p1, t),
            Vector2.Lerp(src.c1.p2, dst_c1p2, t),
            Vector2.Lerp(src.c1.p3, dst_c1p3, t)
        );
        CubicBezier<Vector2, float> c2 = new (
            Vector2.Lerp(src.c2.p0, dst_c2p0, t),
            Vector2.Lerp(src.c2.p1, dst_c2p1, t),
            Vector2.Lerp(src.c2.p2, dst_c2p2, t),
            Vector2.Lerp(src.c2.p3, dst_c2p3, t)
        );
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

    /// Convert a collection of <c>CubicSpline</c> into a <c>ShapePath</c>.
    public static ShapePath CollectionToShapePath(CubicSpline[] splines)
    {
        var pb = new PathBuilder();
        foreach (var spline in splines)
        {
            pb.MoveTo(spline.Start);
            foreach (var arc in spline.Arcs)
            {
                pb.CubicTo(arc.p1, arc.p2, arc.p3);
            }
            if (spline.Closed)
            {
                pb.Close();
            }
        }
        return pb;
    }

    internal Vector2 ClosestControlPoint(Vector2 point, CubicSpline[] spline)
    {
        var ret = new Vector2();
        var bestDistance = double.MaxValue;
        foreach (var arc in Arcs)
        {
            var points = arc.Points;
            foreach (var p in points)
            {
                var dif = p - point;
                var distance = Vector2.Dot(dif, dif);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    ret = p;
                }
            }
        }
        return ret;
    }

    internal static float MinControlPointDistance(CubicSpline a, CubicSpline b)
    {
        float ret = 0.0f;
        var aPoints = a.Arcs.SelectMany(x => x.Points).ToList();
        foreach (var acp in aPoints)
        {
            var bcp = b.ClosestControlPoint(acp, new CubicSpline[] { });
            var dif = acp - bcp; 
            var distance = Vector2.Dot(dif, dif);
            ret += distance;
        }
        return ret;
    }

    internal static int[] BestMatches(CubicSpline[] a, CubicSpline[] b)
    {
        var ret = new int[a.Length];
        var pool = Enumerable.Range(0, b.Length).ToList();
        for (int i = 0; i < a.Length; i++)
        {
            int bestIndex = -1;
            double bestDistance = double.MaxValue;
            for (int j = 0; j < pool.Count; j++)
            {
                var idx = pool[j];
                var splineA = a[i];
                var splineB = b[idx];
                var distance = MinControlPointDistance(splineA, splineB);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = idx;
                }
            }
            if (bestIndex == -1)
            {
                ret[i] = -1;
            }
            else
            {
                ret[i] = bestIndex;
                pool.Remove(bestIndex);
            }
        }
        return ret;
    }

    /// <summary>
    /// Morphs a multi-part <c>CubicSpline</c> into another multi-part <c>CubicSpline</c> given progress.
    /// </summary>
    public static CubicSpline[] MorphCollection(CubicSpline[] a, CubicSpline[] b, float t)
    {
        var matches = BestMatches(a, b);
        var ret = new CubicSpline[Math.Max(a.Length, b.Length)];
        List<int> unMatchedB = Enumerable.Range(0, b.Length).ToList();
        int retIdx = 0;

        void scaleSpline(CubicSpline spline, float scale) {
            var massPoint = spline.Arcs.SelectMany(x => x.Points).Aggregate((x, y) => x + y) / (spline.Arcs.Count*4);
            spline.Start = massPoint + (scale * (spline.Start - massPoint));
            for (int k = 0; k < spline.Arcs.Count; k++)
            {
                var arc = spline.Arcs[k];
                arc.p0 = massPoint + scale * (arc.p0 - massPoint);
                arc.p1 = massPoint + scale * (arc.p1 - massPoint);
                arc.p2 = massPoint + scale * (arc.p2 - massPoint);
                arc.p3 = massPoint + scale * (arc.p3 - massPoint);
                spline.Arcs[k] = arc;
            }
        };

        for (int i = 0; i < matches.Length; i++)
        {
            var idx = matches[i];
            if (idx == -1)
            {
                // shrink into nothing
                var spline = a[i].Clone();
                float scale = Math.Clamp(1.0f - t, 0.0f, 1.0f);
                scaleSpline(spline, scale);
                ret[retIdx] = spline;
            }
            else
            {
                ret[retIdx] = a[i].MorphTo(b[idx], t);
                unMatchedB.Remove(idx);
            }
            retIdx++;
        }
        foreach (var idx in unMatchedB)
        {
            // grow from nothing
            var spline = b[idx].Clone();
            float scale = Math.Clamp(t, 0.0f, 1.0f);
            scaleSpline(spline, scale);
            ret[retIdx] = spline;
            retIdx++;
        }
        Debug.Assert(retIdx == ret.Length);
        return ret;
    }

    /// <summary>
    /// Morphs this <c>CubicSpline</c> into another <c>CubicSpline</c> given progress.
    /// </summary>
    public CubicSpline MorphTo(CubicSpline b, float t)
    {
        var a = this;
        var longer = a.Arcs.Count > b.Arcs.Count ? a : b;
        var shorter = a.Arcs.Count > b.Arcs.Count ? b : a;
        var lenA = a.Arcs.Count;
        var lenB = b.Arcs.Count;

        float origT = t;
        if (longer != a && lenA != lenB)
        {
            t = 1.0f - t;
        }
        var ret = new CubicSpline();
        ret.Arcs = new List<CubicBezier<Vector2, float>>();
        ret.Start = Vector2.Lerp(a.Start, b.Start, origT);
        ret.Closed = a.Closed && b.Closed;

        var arrA = a.Arcs;
        var arrB = b.Arcs;

        if (lenA == lenB)
        {
            for (int mi = 0; mi < lenA; mi++)
            {
                var interpolated = new CubicBezier<Vector2, float>(
                    Vector2.Lerp(arrA[mi].p0, arrB[mi].p0, origT),
                    Vector2.Lerp(arrA[mi].p1, arrB[mi].p1, origT),
                    Vector2.Lerp(arrA[mi].p2, arrB[mi].p2, origT),
                    Vector2.Lerp(arrA[mi].p3, arrB[mi].p3, origT)
                );
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
                var arc1 = longerArr[longerIndices[0]];
                var arc2 = shorterArr[shorterIndex];
                var interpolated = new CubicBezier<Vector2, float>(
                    Vector2.Lerp(arc1.p0, arc2.p0, t),
                    Vector2.Lerp(arc1.p1, arc2.p1, t),
                    Vector2.Lerp(arc1.p2, arc2.p2, t),
                    Vector2.Lerp(arc1.p3, arc2.p3, t)
                );
                ret.Arcs.Add(interpolated);
                shorterIndex++;
                continue;
            }

            var interpolatedList = new List<CubicBezier<Vector2, float>>();
            var subSpline = new CubicSpline() { Arcs = longerIndices.Select(x => longerArr[x]).ToList() };
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
