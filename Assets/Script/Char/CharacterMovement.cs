using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float destinationThreshold = 0.5f;
    [SerializeField, Min(0f)] private float maxWaypointDistance = 5f;
    [SerializeField] private float rayDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float checkRadiusPadding = -0.05f;

    [Header("Grounding Settings")]
    [SerializeField] private float groundRaycastDistance = 1f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private float rotationDamping = 10f;

    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private readonly Queue<Vector3> _pathQueue = new();
    private bool _isFollowingPath;

    public float CurrentSpeed => _agent.enabled ? _agent.velocity.magnitude : 0f;
    public bool IsFollowingPath => _isFollowingPath;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _rb.isKinematic = true;
    }

    public void HandleMovementTick()
    {
        CheckFallState();
        HandleGravity();
        TickPathFollowing();
        AdjustToGround();
    }

    public void SetNewPath(List<Vector3> points)
    {
        ClearPath();
        foreach (Vector3 point in points) _pathQueue.Enqueue(point);
        _isFollowingPath = true;
        AdvanceToNextWaypoint();
    }

    public void ClearPath()
    {
        _pathQueue.Clear();
        _isFollowingPath = false;
        if (_agent != null && _agent.enabled) _agent.ResetPath();
    }

    public void StopMoving()
    {
        ClearPath();
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    private void HandleGravity()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, rayDistance, groundLayer);
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

    private void TickPathFollowing()
    {
        if (!_isFollowingPath) return;
        if (_agent.pathPending || _agent.remainingDistance > destinationThreshold) return;

        if (_pathQueue.Count > 0) AdvanceToNextWaypoint();
        else _isFollowingPath = false;
    }

    private void AdvanceToNextWaypoint()
    {
        while (_pathQueue.TryDequeue(out Vector3 next))
        {
            if (Vector3.Distance(transform.position, next) > maxWaypointDistance) continue;

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
        float radius = (_capsule != null ? _capsule.radius : _agent.radius) + checkRadiusPadding;
        float height = _capsule != null ? _capsule.height : _agent.height;

        Vector3 pointBottom = targetPosition + Vector3.up * radius;
        Vector3 pointTop = targetPosition + Vector3.up * (height - radius);

        return !Physics.CheckCapsule(pointBottom, pointTop, radius, obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    private void CheckFallState()
    {
        if (transform.position.y <= -50f) GameManager.Instance.EndGame();
    }

    private void AdjustToGround()
    {
        if (!_agent.enabled) return;

        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (!Physics.Raycast(ray, out RaycastHit hit, groundRaycastDistance, groundLayer)) return;

        _agent.nextPosition = hit.point + Vector3.up * groundOffset;

        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationDamping);
    }
}