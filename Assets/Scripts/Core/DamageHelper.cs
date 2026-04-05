using UnityEngine;

public static class DamageHelper
{
    public static bool IsValidDamageSource(Collision collision)
    {
        if (!collision.gameObject.CompareTag("DamageSource"))
            return false;

        return collision.gameObject.TryGetComponent(out IDamageSource source) && source.CanDamage(collision);
    }
}
