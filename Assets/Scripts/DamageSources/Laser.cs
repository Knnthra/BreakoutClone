using UnityEngine;

public class Laser : MonoBehaviour
{
    /// <summary>
    /// The laser's speed
    /// </summary>
    [SerializeField] private float speed;

    /// <summary>
    /// A reference to the lasers renderer.
    /// </summary>
    [SerializeField] private Renderer renderer;

    /// <summary>
    /// Half extend used for collision detection
    /// </summary>
    private Vector3 halfExtents;


    private void Start()
    {
        Destroy(gameObject, 10);

        if (renderer != null)
            halfExtents = renderer.bounds.extents;
    }

    private void Update()
    {
        float distance = speed * Time.deltaTime;

        CollisionDetection(distance);

        transform.position += Vector3.up * distance;
    }

    /// <summary>
    /// Collision detection with Boxcast, to make the collisoin more reliable
    /// </summary>
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
