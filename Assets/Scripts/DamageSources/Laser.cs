using UnityEngine;

public class Laser : MonoBehaviour
{
    /// <summary>
    /// Upward movement speed in units per second.
    /// </summary>
    [SerializeField] private float speed;

    /// <summary>
    /// Renderer used to calculate collision half-extents from bounds.
    /// </summary>
    [SerializeField] private Renderer laserRenderer;

    /// <summary>
    /// Half-extents of the laser's bounds, used for boxcast collision detection.
    /// </summary>
    private Vector3 halfExtents;

    private void Start()
    {
        Destroy(gameObject, 10);

        if (laserRenderer != null)
            halfExtents = laserRenderer.bounds.extents;
    }

    private void Update()
    {
        float distance = speed * Time.deltaTime;

        CollisionDetection(distance);

        transform.position += Vector3.up * distance;
    }

    /// <summary>
    /// Performs a boxcast ahead of the laser to detect and damage bricks or chunks.
    /// </summary>
    /// <param name="distance">How far ahead to cast the box, matching this frame's movement distance.</param>
    private void CollisionDetection(float distance)
    {
        RaycastHit[] hits = Physics.BoxCastAll(transform.position, halfExtents, Vector3.up, Quaternion.identity, distance);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent(out Brick brick))
                brick.Damage(hit.point);
            else if (hit.collider.TryGetComponent(out Chunk chunk))
                chunk.DamageParent(hit.point);
        }
    }
}
