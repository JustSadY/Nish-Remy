using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class RoadMeshBuilder : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float width = 1.5f;
    [SerializeField] private Material roadMaterial;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();

        if (roadMaterial != null)
            _meshRenderer.sharedMaterial = roadMaterial;
    }

    public void Build(List<Vector3> points)
    {
        if (points == null || points.Count < 2) return;

        if (_mesh != null) Destroy(_mesh);

        _mesh = GenerateMesh(points);
        _meshFilter.mesh = _mesh;
        _meshCollider.sharedMesh = _mesh;
    }

    private Mesh GenerateMesh(List<Vector3> points)
    {
        int count = points.Count;

        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[count * 2];
        int[] triangles = new int[(count - 1) * 6];

        float totalLength = ComputeTotalLength(points);
        float accumulated = 0f;

        for (int i = 0; i < count; i++)
        {
            if (i > 0) accumulated += Vector3.Distance(points[i - 1], points[i]);

            Vector3 forward = GetForward(points, i);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float half = width * 0.5f;
            float v = totalLength > 0f ? accumulated / totalLength : 0f;

            vertices[i * 2] = points[i] - right * half;
            vertices[i * 2 + 1] = points[i] + right * half;
            uvs[i * 2] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);
        }

        for (int i = 0, t = 0; i < count - 1; i++, t += 6)
        {
            int b = i * 2;

            triangles[t] = b;
            triangles[t + 1] = b + 2;
            triangles[t + 2] = b + 1;
            triangles[t + 3] = b + 1;
            triangles[t + 4] = b + 2;
            triangles[t + 5] = b + 3;
        }

        Mesh mesh = new Mesh { name = "RoadMesh" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
        return mesh;
    }

    private static Vector3 GetForward(List<Vector3> points, int index)
    {
        if (index < points.Count - 1)
            return (points[index + 1] - points[index]).normalized;
        if (index > 0)
            return (points[index] - points[index - 1]).normalized;
        return Vector3.forward;
    }

    private static float ComputeTotalLength(List<Vector3> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
            length += Vector3.Distance(points[i - 1], points[i]);
        return length;
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}