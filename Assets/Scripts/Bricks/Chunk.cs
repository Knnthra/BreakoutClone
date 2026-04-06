using UnityEngine;

public class Chunk : MonoBehaviour
{
    /// <summary>
    /// Reference to the parent brick this chunk belongs to.
    /// </summary>
    private Brick brick;

    /// <summary>
    /// The chunk's physics collider, disabled after detaching.
    /// </summary>
    [SerializeField] private Collider collider;

    /// <summary>
    /// The chunk's mesh renderer for visual display.
    /// </summary>
    [SerializeField] private MeshRenderer meshRenderer;

    /// <summary>
    /// Default material cloned and tinted to match the brick's color.
    /// </summary>
    [SerializeField] private Material defaultMaterial;

    /// <summary>
    /// True while still attached to the brick grid.
    /// </summary>
    public bool IsAttached { get; private set; } = true;

    /// <summary>
    /// Current movement velocity in world units per second.
    /// </summary>
    private Vector3 velocity;

    /// <summary>
    /// Rotation speed in degrees per second after detaching.
    /// </summary>
    private float angularSpeed;

    /// <summary>
    /// Random axis the chunk tumbles around after detaching.
    /// </summary>
    private Vector3 rotationAxis;

    /// <summary>
    /// Seconds elapsed since this chunk was detached.
    /// </summary>
    private float lifetime;

    /// <summary>
    /// Scale captured at detach time, used as the start value for shrink lerp.
    /// </summary>
    private Vector3 originalScale;

    /// <summary>
    /// Downward acceleration applied every frame.
    /// </summary>
    private float gravity;

    /// <summary>
    /// Total seconds the chunk lives before being destroyed.
    /// </summary>
    private float chunkLifetime;

    /// <summary>
    /// Seconds after detach before the chunk begins shrinking to zero.
    /// </summary>
    private float shrinkStartTime;

    /// <summary>
    /// Sets up the chunk with its parent brick reference, physics material, and color.
    /// </summary>
    /// <param name="brick">Provides the physics material and base color to copy onto this chunk.</param>
    /// <param name="gravity">Stored and applied as downward acceleration each frame after detaching.</param>
    /// <param name="chunkLifetime">Maximum seconds this chunk exists after detaching before auto-destroy.</param>
    /// <param name="shrinkStartTime">Delay in seconds before the chunk begins scaling down to zero.</param>
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
    /// Detaches this chunk from the brick grid and launches it in a direction based on the impact.
    /// </summary>
    /// <param name="impactPoint">Hit location used to calculate the away-from-impact launch direction.</param>
    /// <param name="hitNormal">Blended with the away direction to shape the final launch angle.</param>
    /// <param name="launchSpeedMin">Lower bound of the random speed range for trajectory variety.</param>
    /// <param name="launchSpeedMax">Upper bound of the random speed range for trajectory variety.</param>
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
    /// Computes a randomized launch direction blending the away-from-impact and hit-normal vectors.
    /// </summary>
    /// <param name="impactPoint">Origin for the away-from-impact vector (60% weight in blend).</param>
    /// <param name="hitNormal">Surface normal contributing 40% of the launch direction blend.</param>
    /// <param name="launchSpeedMin">Lower bound of the random speed picked for this chunk.</param>
    /// <param name="launchSpeedMax">Upper bound of the random speed picked for this chunk.</param>
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
    /// Multiplies the current velocity for a more explosive scatter.
    /// </summary>
    /// <param name="multiplier">Scales the existing velocity for a more dramatic scatter.</param>
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
    /// Forwards damage to the parent brick from a world-space hit point.
    /// </summary>
    /// <param name="hitPoint">Forwarded to the parent brick to direct the next chunk break.</param>
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
