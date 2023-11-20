namespace AnimLib;

internal class LinearSpline {
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
    public static ShapePath MorphLinear(ShapePath.LinearPath startLinear, ShapePath.LinearPath endLinear, float t, int segments = 100)
    {
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
        return pathBuilder.Build();
    }

}
