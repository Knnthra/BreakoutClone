using UnityEngine;

public static class DamageHelper
{
    /// <summary>
    /// Returns true if the collision came from a tagged DamageSource that confirms it can deal damage.
    /// </summary>
    /// <param name="collision">The collision to inspect for a valid DamageSource component and tag.</param>
    /// <returns>True if the colliding object is tagged DamageSource and its IDamageSource confirms the hit.</returns>
    public static bool IsValidDamageSource(Collision collision)
    {
        if (!collision.gameObject.CompareTag("DamageSource"))
            return false;

        return collision.gameObject.TryGetComponent(out IDamageSource source) && source.CanDamage(collision);
    }
}
