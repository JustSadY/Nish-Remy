using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class RoadMeshBuilder : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float roadWidth = 2f;
    [SerializeField, Min(0f)] private float heightOffset = 0.02f;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float terrainRaycastDistance = 20f;

    // Spline noktaları arasına ek örnekleme — rampalarda yüzey takibini düzeltir
    [SerializeField, Min(1)] private int terrainResampleCount = 8;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
    }

    public void Build(List<Vector3> splinePoints)
    {
        if (splinePoints == null || splinePoints.Count < 2) return;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Spline noktaları arasına yoğun örnekleme ekle, sonra zemine yapıştır
        List<Vector3> densified = DensifyAndSnap(splinePoints);

        if (densified.Count < 2) return;

        Mesh mesh = GenerateMesh(densified);
        _meshFilter.mesh = mesh;

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = mesh;
        }
    }

    // Her iki spline noktası arasına ek ara noktalar ekle
    // Sonra hepsini zemine snap'le — rampayı düzgün takip eder
    private List<Vector3> DensifyAndSnap(List<Vector3> points)
    {
        var result = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int s = 0; s < terrainResampleCount; s++)
            {
                float t = s / (float)terrainResampleCount;
                Vector3 interpolated = Vector3.Lerp(points[i], points[i + 1], t);
                Vector3 snapped = SnapToTerrain(interpolated);
                result.Add(snapped);
            }
        }

        result.Add(SnapToTerrain(points[^1]));
        return result;
    }

    // Zemin yüzeyini bulmak için hem yukarıdan hem aşağıdan raycast atar
    // Rampanın altında veya üstünde kalan noktaları da doğru snap'ler
    private Vector3 SnapToTerrain(Vector3 worldPoint)
    {
        int mask = GetLayerMask();
        float half = terrainRaycastDistance * 0.5f;

        // Önce yukarıdan aşağı
        Vector3 originAbove = worldPoint + Vector3.up * half;
        if (Physics.Raycast(originAbove, Vector3.down, out RaycastHit hitDown, terrainRaycastDistance, mask))
        {
            // Çarpılan nokta worldPoint'e daha yakın mı kontrol et
            // (yanlış bir üst yüzeye snap'lememek için)
            if (hitDown.point.y <= worldPoint.y + half)
                return hitDown.point + Vector3.up * heightOffset;
        }

        // Aşağıdan yukarı — rampa altına girmiş noktalar için
        Vector3 originBelow = worldPoint - Vector3.up * half;
        if (Physics.Raycast(originBelow, Vector3.up, out RaycastHit hitUp, terrainRaycastDistance, mask))
        {
            return hitUp.point + Vector3.up * heightOffset;
        }

        // İkisi de çarpmadıysa orijinal noktayı kullan
        return worldPoint + Vector3.up * heightOffset;
    }

    private Mesh GenerateMesh(List<Vector3> worldPoints)
    {
        int count = worldPoints.Count;
        var vertices = new Vector3[count * 2];
        var uvs     = new Vector2[count * 2];
        var normals = new Vector3[count * 2];
        var triangles = new int[(count - 1) * 6];

        float totalLength = 0f;
        var cumulative = new float[count];
        for (int i = 1; i < count; i++)
        {
            totalLength += Vector3.Distance(worldPoints[i - 1], worldPoints[i]);
            cumulative[i] = totalLength;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 forward       = GetForwardAt(worldPoints, i);
            Vector3 terrainNormal = GetTerrainNormalAt(worldPoints[i]);
            Vector3 right         = Vector3.Cross(forward, terrainNormal).normalized;

            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(forward, Vector3.up).normalized;

            float half = roadWidth * 0.5f;

            vertices[i * 2]     = transform.InverseTransformPoint(worldPoints[i] - right * half);
            vertices[i * 2 + 1] = transform.InverseTransformPoint(worldPoints[i] + right * half);

            float uvX = totalLength > 0f ? cumulative[i] / totalLength : 0f;
            uvs[i * 2]     = new Vector2(uvX, 0f);
            uvs[i * 2 + 1] = new Vector2(uvX, 1f);

            normals[i * 2]     = transform.InverseTransformDirection(terrainNormal);
            normals[i * 2 + 1] = transform.InverseTransformDirection(terrainNormal);
        }

        int t = 0;
        for (int i = 0; i < count - 1; i++)
        {
            int bl = i * 2, br = i * 2 + 1;
            int tl = (i + 1) * 2, tr = (i + 1) * 2 + 1;
            triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = br;
            triangles[t++] = br; triangles[t++] = tl; triangles[t++] = tr;
        }

        var mesh = new Mesh { name = "Road" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Vector3 GetForwardAt(List<Vector3> pts, int i)
    {
        if (i == 0)             return (pts[1] - pts[0]).normalized;
        if (i == pts.Count - 1) return (pts[^1] - pts[^2]).normalized;
        Vector3 a = (pts[i] - pts[i - 1]).normalized;
        Vector3 b = (pts[i + 1] - pts[i]).normalized;
        return ((a + b) * 0.5f).normalized;
    }

    private Vector3 GetTerrainNormalAt(Vector3 worldPoint)
    {
        int mask = GetLayerMask();
        float half = terrainRaycastDistance * 0.5f;

        Vector3 originAbove = worldPoint + Vector3.up * half;
        if (Physics.Raycast(originAbove, Vector3.down, out RaycastHit hit, terrainRaycastDistance, mask))
            return hit.normal;

        Vector3 originBelow = worldPoint - Vector3.up * half;
        if (Physics.Raycast(originBelow, Vector3.up, out RaycastHit hitUp, terrainRaycastDistance, mask))
            return hitUp.normal;

        return Vector3.up;
    }

    private int GetLayerMask() => terrainLayer == 0 ? ~0 : (int)terrainLayer;
}