using UnityEngine;

public interface IDamageSource
{
    /// <summary>
    /// Returns true if this source is allowed to damage the collided object.
    /// </summary>
    /// <param name="collision">Contact data from the physics engine, used to check impact direction.</param>
    /// <returns>True if this source should deal damage to the collided object.</returns>
    bool CanDamage(Collision collision);
}
