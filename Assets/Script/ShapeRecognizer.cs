using System.Collections.Generic;
using UnityEngine;

public static class ShapeRecognizer
{
    public enum ShapeType
    {
        Road,
        Circle
    }

    [System.Serializable]
    public struct RecognitionResult
    {
        public ShapeType shape;
        public Vector3 centroid;
        public float radius;
        public float confidence;
    }

    public static RecognitionResult Recognize(
        IReadOnlyList<Vector3> points,
        float closureRatio = 0.25f,
        float circularityLimit = 0.22f,
        int minPoints = 12)
    {
        var result = new RecognitionResult { shape = ShapeType.Road };

        if (points == null || points.Count < minPoints)
            return result;

        float perimeter = ComputeLength(points);
        if (perimeter < Mathf.Epsilon) return result;

        float closure = Vector3.Distance(points[0], points[^1]);
        if (closure / perimeter > closureRatio)
            return result;

        Vector3 centroid = ComputeCentroid(points);

        float meanR = 0f;
        foreach (var p in points)
            meanR += Vector3.Distance(p, centroid);
        meanR /= points.Count;

        if (meanR < Mathf.Epsilon) return result;

        float variance = 0f;
        foreach (var p in points)
        {
            float diff = Vector3.Distance(p, centroid) - meanR;
            variance += diff * diff;
        }

        float normalizedDev = Mathf.Sqrt(variance / points.Count) / meanR;

        if (normalizedDev < circularityLimit)
        {
            result.shape = ShapeType.Circle;
            result.centroid = centroid;
            result.radius = meanR;
            result.confidence = 1f - (normalizedDev / circularityLimit);
        }

        return result;
    }

    private static Vector3 ComputeCentroid(IReadOnlyList<Vector3> points)
    {
        Vector3 sum = Vector3.zero;
        foreach (var p in points) sum += p;
        return sum / points.Count;
    }

    private static float ComputeLength(IReadOnlyList<Vector3> points)
    {
        float len = 0f;
        for (int i = 1; i < points.Count; i++)
            len += Vector3.Distance(points[i - 1], points[i]);
        return len;
    }
}