using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class CharacterPathDrawer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer invalidLineRenderer;
    [SerializeField, Min(0f)] private float pointSpacing = 0.3f;

    [Header("Catmull-Rom Spline Settings")]
    [SerializeField, Range(4, 32)] private int samplesPerSegment = 12;
    [SerializeField, Range(0f, 1f)] private float simplifyTolerance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("NavMesh Settings")]
    [SerializeField, Min(0f)] private float navMeshSampleRadius = 2f;
    [SerializeField, Range(0f, 1f)] private float minValidRatio = 0.5f;

    [Header("Slope Settings")]
    // Çizim sırasında iki nokta arasındaki maksimum eğim açısı
    // 45° = her 1 birim yatayda en fazla 1 birim dikey iniş/çıkış
    [SerializeField, Range(10f, 80f)] private float maxSlopeAngle = 45f;

    private readonly List<Vector3> _drawnPoints = new();
    private readonly List<bool> _pointValidity = new();
    private int _lastPreviewCount;
    private const int PreviewUpdateInterval = 4;
    private Vector3[] _splineBuffer = new Vector3[256];
    private Vector3[] _rawBuffer = new Vector3[256];
    private InputSystem_Actions _actions;

    public System.Action<List<Vector3>> OnPathCommitted;
    public bool IsEditorMode { get; set; }

    private void Awake() => _actions = new InputSystem_Actions();

    private void Start()
    {
        if (lineRenderer != null) lineRenderer = Instantiate(lineRenderer);
        if (invalidLineRenderer != null) invalidLineRenderer = Instantiate(invalidLineRenderer);
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        if (invalidLineRenderer != null) invalidLineRenderer.positionCount = 0;
    }

    private void OnEnable() => _actions?.Enable();
    private void OnDisable() => _actions?.Disable();

    public void HandleDrawing()
    {
        if (_actions.Player.Left.WasPressedThisFrame())
        {
            ClearDrawing();
            return;
        }

        if (_actions.Player.Left.IsPressed())
        {
            TrySamplePoint();
            return;
        }

        if (_actions.Player.Left.WasReleasedThisFrame() && _drawnPoints.Count > 1)
            CommitPath();
    }

    public void TrySamplePoint()
    {
        if (!TryGetMouseWorldPosition(out Vector3 worldPos)) return;
        worldPos.y += 0.05f;

        if (_drawnPoints.Count > 0 && Vector3.Distance(_drawnPoints[^1], worldPos) <= pointSpacing) return;

        // Yeni noktayı önce eğim limitine göre düzelt, sonra NavMesh'e snap'le
        worldPos = ApplySlopeLimit(worldPos);

        bool isValid;
        if (IsEditorMode)
        {
            isValid = true;
        }
        else
        {
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                worldPos = navHit.position + Vector3.up * 0.05f;
                isValid = true;
            }
            else
            {
                isValid = false;
            }
        }

        _drawnPoints.Add(worldPos);
        _pointValidity.Add(isValid);
        RefreshPreview();
    }

    // Önceki noktaya göre maxSlopeAngle'ı aşarsa Y'yi klamplar
    // Bu sayede uçurum kenarında dikey düşmek yerine 45° eğimle iner
    private Vector3 ApplySlopeLimit(Vector3 newPoint)
    {
        if (_drawnPoints.Count == 0) return newPoint;

        Vector3 prev = _drawnPoints[^1];
        float hDist = Vector2.Distance(
            new Vector2(prev.x, prev.z),
            new Vector2(newPoint.x, newPoint.z));

        if (hDist < 0.001f) return newPoint;

        float vDiff = newPoint.y - prev.y;
        float maxVChange = hDist * Mathf.Tan(maxSlopeAngle * Mathf.Deg2Rad);

        if (Mathf.Abs(vDiff) > maxVChange)
            newPoint.y = prev.y + Mathf.Sign(vDiff) * maxVChange;

        return newPoint;
    }

    public void ClearDrawing()
    {
        _drawnPoints.Clear();
        _pointValidity.Clear();
        _lastPreviewCount = 0;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        if (invalidLineRenderer != null) invalidLineRenderer.positionCount = 0;
    }

    public List<Vector3> GetDrawnPoints() => _drawnPoints;

    private void CommitPath()
    {
        List<Vector3> validPoints = GetValidPoints();
        float validRatio = _drawnPoints.Count > 0 ? (float)validPoints.Count / _drawnPoints.Count : 0f;

        List<Vector3> pointsToCommit = validPoints.Count >= 2 && validRatio >= minValidRatio
            ? validPoints
            : _drawnPoints;

        if (pointsToCommit.Count >= 2)
        {
            List<Vector3> splinePoints = CatmullRomSpline.Build(pointsToCommit, samplesPerSegment, simplifyTolerance);
            OnPathCommitted?.Invoke(splinePoints);
        }

        ClearDrawing();
    }

    private void RefreshPreview()
    {
        if (_drawnPoints.Count - _lastPreviewCount < PreviewUpdateInterval && _drawnPoints.Count > PreviewUpdateInterval) return;
        _lastPreviewCount = _drawnPoints.Count;
        UpdateRawTrail();
        UpdateSplinePreview();
    }

    private void UpdateRawTrail()
    {
        if (invalidLineRenderer == null) return;
        if (_rawBuffer.Length < _drawnPoints.Count)
            _rawBuffer = new Vector3[_drawnPoints.Count * 2];
        for (int i = 0; i < _drawnPoints.Count; i++)
            _rawBuffer[i] = _drawnPoints[i];
        invalidLineRenderer.positionCount = _drawnPoints.Count;
        invalidLineRenderer.SetPositions(_rawBuffer);
    }

    private void UpdateSplinePreview()
    {
        if (lineRenderer == null) return;
        List<Vector3> validPoints = GetValidPoints();
        if (validPoints.Count < 2) { lineRenderer.positionCount = 0; return; }

        List<Vector3> preview = CatmullRomSpline.BuildRaw(validPoints, samplesPerSegment);
        if (_splineBuffer.Length < preview.Count)
            _splineBuffer = new Vector3[preview.Count * 2];
        for (int i = 0; i < preview.Count; i++)
            _splineBuffer[i] = preview[i];
        lineRenderer.positionCount = preview.Count;
        lineRenderer.SetPositions(_splineBuffer);
    }

    private List<Vector3> GetValidPoints()
    {
        var valid = new List<Vector3>(_drawnPoints.Count);
        for (int i = 0; i < _drawnPoints.Count; i++)
            if (i < _pointValidity.Count && _pointValidity[i])
                valid.Add(_drawnPoints[i]);
        return valid;
    }

    private bool TryGetMouseWorldPosition(out Vector3 result)
    {
        result = transform.position;
        if (Camera.main == null) return false;

        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            result = hit.point;
            return true;
        }

        if (groundLayer == 0 || IsEditorMode)
        {
            if (Physics.Raycast(ray, out RaycastHit fallbackHit, 500f))
            {
                result = fallbackHit.point;
                return true;
            }
        }

        if (IsEditorMode)
        {
            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float distance))
            {
                result = ray.GetPoint(distance);
                return true;
            }
        }

        return false;
    }
}