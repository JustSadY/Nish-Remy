using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterPathDrawer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Min(0f)] private float pointSpacing = 0.3f;
    [Header("Catmull-Rom Spline Settings")]
    [SerializeField, Range(4, 32)] private int samplesPerSegment = 12;
    [SerializeField, Range(0f, 1f)] private float simplifyTolerance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private readonly List<Vector3> _drawnPoints = new();
    private int _lastPreviewCount = 0;
    private const int PreviewUpdateInterval = 4;
    private Vector3[] _lineBuffer = new Vector3[256];
    private InputSystem_Actions _actions;

    public System.Action<List<Vector3>> OnPathCommitted;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
    }

    private void Start()
    {
        if (lineRenderer == null) return;
        lineRenderer = Instantiate(lineRenderer);
        lineRenderer.positionCount = 0;
    }

    private void OnEnable() => _actions?.Enable();
    private void OnDisable() => _actions?.Disable();

    public void HandleDrawing(bool forceClear = false)
    {
        if (forceClear || _actions.Player.Left.WasPressedThisFrame())
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
        {
            CommitPath();
        }
    }

    public void ClearDrawing()
    {
        _drawnPoints.Clear();
        _lastPreviewCount = 0;
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    public List<Vector3> GetDrawnPoints() => _drawnPoints;

    private void TrySamplePoint()
    {
        if (!TryGetMouseWorldPosition(out Vector3 worldPos)) return;

        worldPos.y += 0.05f;

        bool isFarEnough = _drawnPoints.Count == 0 || Vector3.Distance(_drawnPoints[^1], worldPos) > pointSpacing;
        if (!isFarEnough) return;

        _drawnPoints.Add(worldPos);
        RefreshLineRendererPreview();
    }

    private void RefreshLineRendererPreview()
    {
        if (lineRenderer == null) return;
        if (_drawnPoints.Count - _lastPreviewCount < PreviewUpdateInterval && _drawnPoints.Count > PreviewUpdateInterval) return;

        _lastPreviewCount = _drawnPoints.Count;
        List<Vector3> preview = CatmullRomSpline.BuildRaw(_drawnPoints, samplesPerSegment);

        if (_lineBuffer.Length < preview.Count)
            _lineBuffer = new Vector3[preview.Count * 2];

        for (int i = 0; i < preview.Count; i++)
            _lineBuffer[i] = preview[i];

        lineRenderer.positionCount = preview.Count;
        lineRenderer.SetPositions(_lineBuffer);
    }

    private void CommitPath()
    {
        List<Vector3> splinePoints = CatmullRomSpline.Build(_drawnPoints, samplesPerSegment, simplifyTolerance);
        OnPathCommitted?.Invoke(splinePoints);
        ClearDrawing();
    }

    private bool TryGetMouseWorldPosition(out Vector3 result)
    {
        result = transform.position;
        if (Camera.main == null) return false;
        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer)) return false;
        result = hit.point;
        return true;
    }
}