using UnityEngine;

public class Firefly : MonoBehaviour
{
    public float radius = 5f;
    public float speed = 2f;

    private Vector3 spawnPosition;
    private Vector3 targetPosition;

    void Start()
    {
        spawnPosition = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            PickNewTarget();
        }
    }

    void PickNewTarget()
    {
        Vector2 random = Random.insideUnitCircle * radius;

        targetPosition = spawnPosition + new Vector3(
            random.x,
            0,
            random.y
        );
    }
}