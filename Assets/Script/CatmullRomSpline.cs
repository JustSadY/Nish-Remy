using System.Collections.Generic;
using UnityEngine;

public static class CatmullRomSpline
{
    public static List<Vector3> Build(
        IReadOnlyList<Vector3> rawPoints,
        int samplesPerSegment = 12,
        float simplifyTolerance = 0.15f)
    {
        if (rawPoints.Count < 2)
            return new List<Vector3>(rawPoints);

        List<Vector3> ctrl = DouglasPeucker(rawPoints, simplifyTolerance);

        ctrl.Insert(0, ctrl[0] + (ctrl[0] - ctrl[1]));
        ctrl.Add(ctrl[^1] + (ctrl[^1] - ctrl[^2]));

        var result = new List<Vector3>(ctrl.Count * samplesPerSegment);

        for (int i = 1; i < ctrl.Count - 2; i++)
        {
            Vector3 p0 = ctrl[i - 1];
            Vector3 p1 = ctrl[i];
            Vector3 p2 = ctrl[i + 1];
            Vector3 p3 = ctrl[i + 2];

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                result.Add(Evaluate(p0, p1, p2, p3, t));
            }
        }

        result.Add(ctrl[^2]);
        return result;
    }

    public static List<Vector3> BuildRaw(
        IReadOnlyList<Vector3> rawPoints,
        int samplesPerSegment = 8)
    {
        if (rawPoints.Count < 2)
            return new List<Vector3>(rawPoints);

        var ctrl = new List<Vector3>(rawPoints);
        ctrl.Insert(0, ctrl[0] + (ctrl[0] - ctrl[1]));
        ctrl.Add(ctrl[^1] + (ctrl[^1] - ctrl[^2]));

        var result = new List<Vector3>(ctrl.Count * samplesPerSegment);

        for (int i = 1; i < ctrl.Count - 2; i++)
        {
            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                result.Add(Evaluate(ctrl[i - 1], ctrl[i], ctrl[i + 1], ctrl[i + 2], t));
            }
        }

        result.Add(ctrl[^2]);
        return result;
    }

    private static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static List<Vector3> DouglasPeucker(IReadOnlyList<Vector3> points, float tolerance)
    {
        if (points.Count <= 2)
            return new List<Vector3>(points);

        int maxIndex = 0;
        float maxDist = 0f;

        for (int i = 1; i < points.Count - 1; i++)
        {
            float d = PerpendicularDistance(points[i], points[0], points[^1]);
            if (d > maxDist)
            {
                maxDist = d;
                maxIndex = i;
            }
        }

        if (maxDist <= tolerance)
            return new List<Vector3> { points[0], points[^1] };

        var left = DouglasPeucker(Slice(points, 0, maxIndex + 1), tolerance);
        var right = DouglasPeucker(Slice(points, maxIndex, points.Count), tolerance);

        left.RemoveAt(left.Count - 1);
        left.AddRange(right);
        return left;
    }

    private static float PerpendicularDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        float len = line.magnitude;

        if (len < Mathf.Epsilon)
            return Vector3.Distance(point, lineStart);

        return Vector3.Cross(line, lineStart - point).magnitude / len;
    }

    private static List<Vector3> Slice(IReadOnlyList<Vector3> source, int start, int end)
    {
        var slice = new List<Vector3>(end - start);
        for (int i = start; i < end; i++) slice.Add(source[i]);
        return slice;
    }
}