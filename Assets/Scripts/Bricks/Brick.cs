using System;
using UnityEngine;

public class Brick : MonoBehaviour, IDestructable
{
    /// <summary>
    /// Current hit points remaining.
    /// </summary>
    public int Health { get; set; }

    /// <summary>
    /// Color assigned based on health level.
    /// </summary>
    public Color BrickColor { get; set; }

    /// <summary>
    /// Handles chunk creation and scatter/break animations.
    /// </summary>
    [SerializeField] private BrickDestruction destruction;

    /// <summary>
    /// The brick's collider, disabled after destruction to prevent double-hits.
    /// </summary>
    [field: SerializeField] public Collider Collider { get; private set; }

    /// <summary>
    /// The brick's mesh renderer, used for color and visibility.
    /// </summary>
    [field: SerializeField] public MeshRenderer meshRenderer { get; private set; }

    /// <summary>
    /// Sound played each time the brick takes damage.
    /// </summary>
    [SerializeField] private AudioClip brickBreakAudioClip;

    private void Start()
    {
        GameManager.Instance.RegisterBrick();
    }

    /// <summary>
    /// Applies damage using collision contact data for chunk break direction.
    /// </summary>
    /// <param name="collision">The collision data containing contact points, or null for a default direction.</param>
    public void Damage(Collision collision = null)
    {
        Vector3 hitPoint = collision != null && collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
        Vector3 normal = collision != null && collision.contactCount > 0 ? collision.contacts[0].normal : Vector3.up;
        ApplyDamage(hitPoint, normal);
    }

    /// <summary>
    /// Applies damage from a world-space hit point, used by laser boxcasting.
    /// </summary>
    /// <param name="hitPoint">Where the laser or projectile struck, used to aim chunk breaks.</param>
    public void Damage(Vector3 hitPoint)
    {
        Vector3 normal = (transform.position - hitPoint).normalized;
        ApplyDamage(hitPoint, normal);
    }

    /// <summary>
    /// Reduces health, plays break sound, and either destroys or breaks chunks at the hit point.
    /// </summary>
    /// <param name="hitPoint">Where the hit landed, forwarded to BrickDestruction for chunk breaking.</param>
    /// <param name="normal">Inward direction from the hit, controls which chunk detaches.</param>
    private void ApplyDamage(Vector3 hitPoint, Vector3 normal)
    {
        Health--;
        AudioManager.Instance.PlaySFX(brickBreakAudioClip);

        if (!TryDestroy() && destruction != null)
            destruction.BreakChunksAtPoint(hitPoint, normal);
    }

    /// <summary>
    /// Destroys the brick if health is zero or below. Returns true if destroyed.
    /// </summary>
    /// <returns>True if health reached zero and the brick was destroyed.</returns>
    public virtual bool TryDestroy()
    {
        if (Health <= 0)
        {
            if (destruction != null)
                destruction.ScatterAll();

            // Disable collider so ball doesn't double-hit during scatter animation
            if (Collider != null) Collider.enabled = false;

            // Delay destroy so chunks can animate
            Destroy(gameObject, 2.5f);
            GameManager.Instance.ScorePoint();
            GameManager.Instance.RemoveBrick();
            return true;
        }

        return false;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (DamageHelper.IsValidDamageSource(collision))
            Damage(collision);
    }
}
