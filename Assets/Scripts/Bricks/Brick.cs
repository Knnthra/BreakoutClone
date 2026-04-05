using System;
using UnityEngine;

public class Brick : MonoBehaviour, IDestructable
{
    public int Health { get; set; }

    public Color BrickColor { get; set; }

    [SerializeField] private BrickDestruction destruction;

    [field: SerializeField] public Collider Collider { get; private set; }

    [field: SerializeField] public MeshRenderer meshRenderer { get; private set; }

    [SerializeField] private AudioClip brickBreakAudioClip;

    private void Start()
    {
        GameManager.Instance.RegisterBrick();
    }

    /// <summary>
    /// Deals damage to the brick using collision data
    /// </summary>
    /// <param name="collision">Describes the collision</param>
    public void Damage(Collision collision = null)
    {
        Vector3 hitPoint = collision != null && collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
        Vector3 normal = collision != null && collision.contactCount > 0 ? collision.contacts[0].normal : Vector3.up;
        ApplyDamage(hitPoint, normal);
    }

    /// <summary>
    /// Deals damage to the brick at a specific point (used by the laser with boxcasting)
    /// </summary>
    /// <param name="hitPoint">The location the brick was hit</param>
    public void Damage(Vector3 hitPoint)
    {
        Vector3 normal = (transform.position - hitPoint).normalized;
        ApplyDamage(hitPoint, normal);
    }

    /// <summary>
    /// Applies damage to the brick, reducing health and breaking chunks at the hit point
    /// </summary>
    private void ApplyDamage(Vector3 hitPoint, Vector3 normal)
    {
        Health--;
        AudioManager.Instance.PlaySFX(brickBreakAudioClip);

        if (!TryDestroy() && destruction != null)
            destruction.BreakChunksAtPoint(hitPoint, normal);
    }

    /// <summary>
    /// Tries to destroy the brick
    /// </summary>
    /// <returns>True if the brick was destroyed, 0 health left</returns>
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
