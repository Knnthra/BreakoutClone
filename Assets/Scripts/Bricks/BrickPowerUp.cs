using Unity.Mathematics;
using UnityEngine;

public class BrickPowerUp : Brick
{
    /// <summary>
    /// Power-up prefab spawned the first time this brick is destroyed.
    /// </summary>
    [SerializeField]private GameObject powerUpPrefab;

    /// <summary>
    /// Prevents the power-up from being spawned more than once.
    /// </summary>
    private bool spawned = false;

    /// <summary>
    /// Spawns a power-up on first destruction, then delegates to the base class.
    /// </summary>
    /// <returns>True if health reached zero and the brick was destroyed.</returns>
    public override bool TryDestroy()
    {
        if (!spawned)
        {
            spawned = true;
            // Spawn power-up before the base class destroys the brick
            Instantiate(powerUpPrefab, transform.position, Quaternion.identity);
        }

        return base.TryDestroy();
    }
}
