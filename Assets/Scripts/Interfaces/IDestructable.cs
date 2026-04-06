using UnityEngine;
public interface IDestructable
{
    /// <summary>
    /// Current hit points remaining.
    /// </summary>
    int Health { get; set; }

    /// <summary>
    /// Destroys the object if health has reached zero.
    /// </summary>
    /// <returns>True if the object was destroyed.</returns>
    bool TryDestroy();

    /// <summary>
    /// Applies damage using contact information from a collision.
    /// </summary>
    /// <param name="collision">Physics collision providing contact points and normals, or null for a default direction.</param>
    void Damage(Collision collision = null);

    /// <summary>
    /// Applies damage from a specific world-space hit point.
    /// </summary>
    /// <param name="hitPoint">World position where the hit landed, used to direct chunk breaks.</param>
    void Damage(Vector3 hitPoint);
}
