using UnityEngine;

public class Chunk : MonoBehaviour
{
    /// <summary>
    /// A reference to the brick this chunk belongs to
    /// </summary>
    private Brick brick;

    /// <summary>
    /// The chunk's collider
    /// </summary>
    [SerializeField] private Collider collider;

    /// <summary>
    /// The chunk's mesh renderer
    /// </summary>
    [SerializeField] private MeshRenderer meshRenderer;

    /// <summary>
    /// Default material for the chunks
    /// </summary>
    [SerializeField] private Material defaultMaterial;


    /// <summary>
    /// True while still part of the brick grid
    /// </summary>
    public bool IsAttached { get; private set; } = true;

    /// <summary>
    /// Current movement direction and speed in world units per second
    /// </summary>
    private Vector3 velocity;

    /// <summary>
    /// Rotation speed in degrees per second
    /// </summary>
    private float angularSpeed;

    /// <summary>
    /// Random axis the chunk tumbles around after detaching
    /// </summary>
    private Vector3 rotationAxis;

    /// <summary>
    /// How long this chunk has been detached in seconds
    /// </summary>
    private float lifetime;

    /// <summary>
    /// Scale at detach time, used as the start value for shrink lerp
    /// </summary>
    private Vector3 originalScale;

    /// <summary>
    /// Downward acceleration applied every frame
    /// </summary>
    private float gravity;

    /// <summary>
    /// Total time the chunk lives before being destroyed
    /// </summary>
    private float chunkLifetime;

    /// <summary>
    /// Time after detach before the chunk starts shrinking to zero
    /// </summary>
    private float shrinkStartTime;

    public void Initialize(Brick brick, float gravity, float chunkLifetime, float shrinkStartTime)
    {
        this.brick = brick;
        this.gravity = gravity;
        this.chunkLifetime = chunkLifetime;
        this.shrinkStartTime = shrinkStartTime;

        collider.material = brick.Collider.material;

        Material chunkMaterial = new Material(defaultMaterial);

        chunkMaterial.SetColor("_BaseColor", brick.meshRenderer.material.GetColor("_BaseColor"));

        meshRenderer.sharedMaterial = chunkMaterial;
    }

    /// <summary>
    /// Detaches this chunk from the brick and sends it flying
    /// </summary>
    public void Detach(Vector3 impactPoint, Vector3 hitNormal, float launchSpeedMin, float launchSpeedMax)
    {
        IsAttached = false;

         lifetime = 0f;

        // Disable collider and brick forwarding
        if (collider != null) collider.enabled = false;

        // Unparent from the brick
        transform.SetParent(null, true);

        // Capture scale AFTER unparenting so it reflects the actual world size
        originalScale = transform.localScale;

        CalculateLaunchDirection(impactPoint,hitNormal,launchSpeedMin,launchSpeedMax);                
    }

    /// <summary>
    /// Calculates chunk's launch direction
    /// </summary>
    /// <param name="impactPoint"></param>
    /// <param name="hitNormal"></param>
    /// <param name="launchSpeedMin"></param>
    /// <param name="launchSpeedMax"></param>
    private void CalculateLaunchDirection(Vector3 impactPoint,Vector3 hitNormal, float launchSpeedMin,  float launchSpeedMax)
    {
        // Calculate launch direction
        Vector3 awayDir = (transform.position - impactPoint).normalized;
        Vector3 launchDir = (awayDir * 0.6f + hitNormal * 0.4f).normalized;
        launchDir += Random.insideUnitSphere * 0.3f;
        launchDir.Normalize();

        float launchSpeed = Random.Range(launchSpeedMin, launchSpeedMax);
        velocity = launchDir * launchSpeed;

        // Random tumble
        rotationAxis = Random.onUnitSphere;
        angularSpeed = Random.Range(180f, 540f);
    }

    /// <summary>
    /// Boosts velocity by a multiplier (used for final scatter explosion)
    /// </summary>
    public void BoostVelocity(float multiplier)
    {
        velocity *= multiplier;
    }

    private void Update()
    {
        if (IsAttached) return;

        float dt = Time.deltaTime;

        lifetime += dt;

        if (lifetime >= chunkLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Gravity
        velocity += new Vector3(0f, gravity * dt, 0f);

        // Movement
        transform.position += velocity * dt;

        // Tumble
        transform.Rotate(rotationAxis, angularSpeed * dt, Space.World);

        // Shrink
        if (lifetime > shrinkStartTime)
        {
            float t = (lifetime - shrinkStartTime) / (chunkLifetime - shrinkStartTime);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
        }
    }

    /// <summary>
    /// Reduces the brick's(parent) health when the chunk gets hit
    /// </summary>
    /// <param name="hitPoint"></param>
    public void DamageParent(Vector3 hitPoint)
    {
        if (brick != null)
            brick.Damage(hitPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (DamageHelper.IsValidDamageSource(collision))
            brick.Damage(collision);
    }
}
