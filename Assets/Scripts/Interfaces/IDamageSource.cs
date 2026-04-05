using UnityEngine;

public interface IDamageSource
{
    bool CanDamage(Collision collision);
}
