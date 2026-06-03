using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class CharacterControl : MonoBehaviour
{
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Min(0f)] private float pointSpacing = 0.3f;
    [SerializeField, Min(0f)] private float destinationThreshold = 0.5f;
    [SerializeField] private float cameraRotationSpeed = 200f;

    [Header("Catmull-Rom Spline Settings")] [SerializeField, Range(4, 32)]
    private int samplesPerSegment = 12;

    [SerializeField, Range(0f, 1f)] private float simplifyTolerance = 0.15f;

    [Header("Editor Mode")] [SerializeField]
    private bool isEditorMode;

    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Physics & Gravity")] [SerializeField]
    private float rayDistance = 1.2f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Footsteps")] [SerializeField] private AudioSource audioFootsteps;
    [SerializeField] private Sprite footprintSprite;
    [SerializeField] private Transform leftFootTransform;
    [SerializeField] private Transform rightFootTransform;
    [SerializeField] private float footprintDuration = 5f;

    [Header("Path Validation")] [SerializeField, Min(0f)]
    private float maxWaypointDistance = 5f;

    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float checkRadiusPadding = -0.05f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Rigidbody _rb;
    private CinemachineOrbitalFollow _orbital;

    private readonly List<Vector3> _drawnPoints = new();
    private readonly Queue<Vector3> _pathQueue = new();
    private bool _isFollowingPath;
    private GameObject _currentRoad;

    private InputSystem_Actions _actions;
    private bool _isStopped;

    private int _lastPreviewCount = 0;
    private const int PreviewUpdateInterval = 4;
    private Vector3[] _lineBuffer = new Vector3[256];

    private void Awake()
    {
        _actions = new InputSystem_Actions();

        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        _orbital = FindAnyObjectByType<CinemachineOrbitalFollow>();

        _rb.isKinematic = true;
    }

    private void Start()
    {
        if (lineRenderer == null) return;
        lineRenderer = Instantiate(lineRenderer);
        lineRenderer.positionCount = 0;
    }

    private void OnEnable()
    {
        _actions?.Enable();
    }

    private void OnDisable()
    {
        _actions?.Disable();
    }

    private void Update()
    {
        if (_isStopped) return;

        CheckFallState();
        HandleGravity();

        if (_actions.Player.E.WasPressedThisFrame())
        {
            isEditorMode = !isEditorMode;
            ClearPath();
            _animator.SetFloat(AnimSpeed, 0f);
        }

        HandleCameraRotation();

        if (isEditorMode)
        {
            HandleEditorDrawing();
        }
        else
        {
            HandlePathDrawing();
            TickPathFollowing();
            SyncAnimator();
        }
    }

    private void CheckFallState()
    {
        if (transform.position.y <= -50f)
        {
            GameManager.Instance.EndGame();
        }
    }

    public void StopCharacter()
    {
        _isStopped = true;
        ClearPath();

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (_animator != null)
        {
            _animator.SetFloat(AnimSpeed, 0f);
        }

        enabled = false;
    }

    private void HandleGravity()
    {
        bool isGrounded =
            Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, rayDistance, groundLayer);
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * rayDistance, Color.red);
        if (isGrounded)
        {
            if (!_agent.enabled)
            {
                _agent.enabled = true;
                _rb.isKinematic = true;
            }
        }
        else
        {
            if (!_agent.enabled) return;
            _agent.enabled = false;
            _rb.isKinematic = false;
        }
    }

    private void OnFootstep(int isLeftFootInt)
    {
        if (audioFootsteps != null) audioFootsteps.Play();
        SpawnFootprint(isLeftFootInt == 1);
    }

    private void SpawnFootprint(bool isLeft)
    {
        if (footprintSprite == null) return;
        Transform activeFoot = isLeft ? leftFootTransform : rightFootTransform;
        Vector3 spawnPos = activeFoot != null ? activeFoot.position : transform.position;
        spawnPos.y += 0.01f;

        GameObject footprintObj = new GameObject("Footprint");
        SpriteRenderer sr = footprintObj.AddComponent<SpriteRenderer>();
        sr.sprite = footprintSprite;
        footprintObj.transform.position = spawnPos;
        footprintObj.transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        Destroy(footprintObj, footprintDuration);
    }

    private void HandleEditorDrawing()
    {
        if (_actions.Player.Left.WasPressedThisFrame()) _drawnPoints.Clear();
        if (_actions.Player.Left.IsPressed()) TrySamplePoint();
        if (_actions.Player.Left.WasReleasedThisFrame() && _drawnPoints.Count > 1)
        {
            CreatePermanentRoad();
            ClearLineRenderer();
        }
    }

    private void CreatePermanentRoad()
    {
        if (_currentRoad != null)
        {
            Destroy(_currentRoad);
            _currentRoad = null;
            StartCoroutine(RebuildNavMeshThenSpawn());
            return;
        }

        SpawnRoad();
    }

    private void SpawnRoad()
    {
        List<Vector3> splinePoints = CatmullRomSpline.Build(
            _drawnPoints, samplesPerSegment, simplifyTolerance);

        _currentRoad = Instantiate(roadPrefab);
        _currentRoad.transform.position = new Vector3(0f, -0.1f, 0f);

        if (!_currentRoad.TryGetComponent(out NavMeshModifier modifier))
            modifier = _currentRoad.AddComponent<NavMeshModifier>();

        modifier.overrideArea = true;
        modifier.area = 0;

        if (_currentRoad.TryGetComponent(out RoadMeshBuilder meshBuilder))
            meshBuilder.Build(splinePoints);

        _drawnPoints.Clear();
        StartCoroutine(BuildNavMeshAsync());
    }

    private IEnumerator RebuildNavMeshThenSpawn()
    {
        yield return null;
        if (navMeshSurface != null)
            yield return navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        SpawnRoad();
    }

    private IEnumerator BuildNavMeshAsync()
    {
        if (navMeshSurface == null) yield break;
        yield return null;
        yield return navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }

    private void HandleCameraRotation()
    {
        if (_orbital == null || !_actions.Player.Right.IsPressed()) return;
        float delta = _actions.Player.Look.ReadValue<Vector2>().x * cameraRotationSpeed * Time.deltaTime;
        _orbital.HorizontalAxis.Value += delta;
    }

    private void HandlePathDrawing()
    {
        if (_actions.Player.Left.WasPressedThisFrame())
        {
            ClearPath();
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

    private void TrySamplePoint()
    {
        if (!TryGetMouseWorldPosition(out Vector3 worldPos)) return;
        bool isFarEnough = _drawnPoints.Count == 0
                           || Vector3.Distance(_drawnPoints[^1], worldPos) > pointSpacing;
        if (!isFarEnough) return;
        _drawnPoints.Add(worldPos);
        RefreshLineRendererPreview();
    }

    private void RefreshLineRendererPreview()
    {
        if (lineRenderer == null) return;

        if (_drawnPoints.Count - _lastPreviewCount < PreviewUpdateInterval
            && _drawnPoints.Count > PreviewUpdateInterval) return;

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
        foreach (Vector3 point in splinePoints) _pathQueue.Enqueue(point);
        ClearLineRenderer();
        _drawnPoints.Clear();
        _isFollowingPath = true;
        AdvanceToNextWaypoint();
    }

    private void TickPathFollowing()
    {
        if (!_isFollowingPath) return;
        if (_agent.pathPending) return;
        if (_agent.remainingDistance > destinationThreshold) return;

        if (_pathQueue.Count > 0) AdvanceToNextWaypoint();
        else _isFollowingPath = false;
    }

    private void AdvanceToNextWaypoint()
    {
        while (_pathQueue.TryDequeue(out Vector3 next))
        {
            if (Vector3.Distance(transform.position, next) > maxWaypointDistance)
                continue;

            if (!IsSpaceAvailable(next))
            {
                ClearPath();
                return;
            }

            NavMeshPath testPath = new NavMeshPath();
            if (_agent.CalculatePath(next, testPath) && testPath.status == NavMeshPathStatus.PathComplete)
            {
                _agent.SetDestination(next);
                return;
            }
        }

        _isFollowingPath = false;
    }

    private bool IsSpaceAvailable(Vector3 targetPosition)
    {
        if (_agent == null) return true;

        float radius = _agent.radius + checkRadiusPadding;
        float height = _agent.height;

        Vector3 pointBottom = targetPosition + Vector3.up * radius;
        Vector3 pointTop = targetPosition + Vector3.up * (height - radius);

        bool hasCollision =
            Physics.CheckCapsule(pointBottom, pointTop, radius, obstacleLayers, QueryTriggerInteraction.Ignore);

        return !hasCollision;
    }

    private void ClearPath()
    {
        _drawnPoints.Clear();
        _pathQueue.Clear();
        _isFollowingPath = false;
        _lastPreviewCount = 0;
        if (_agent != null && _agent.enabled) _agent.ResetPath();
        ClearLineRenderer();
    }

    private void ClearLineRenderer()
    {
        if (lineRenderer != null) lineRenderer.positionCount = 0;
    }

    private void SyncAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat(AnimSpeed, _agent.enabled ? _agent.velocity.magnitude : 0f);
    }

    private bool TryGetMouseWorldPosition(out Vector3 result)
    {
        result = transform.position;
        if (Camera.main == null) return false;

        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);
        Plane plane = new Plane(Vector3.up, transform.position.y);
        if (!plane.Raycast(ray, out float distance)) return false;
        result = ray.GetPoint(distance);
        return true;
    }
}