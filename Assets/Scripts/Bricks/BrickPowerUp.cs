using Unity.Mathematics;
using UnityEngine;

public class BrickPowerUp : Brick
{
    /// <summary>
    /// The PowerUp this Brick will spawn when destroyed
    /// </summary>
    [SerializeField]private GameObject powerUpPrefab;

    /// <summary>
    /// Indicates if the powerup has spawned or not
    /// </summary>
    private bool spawned = false;

    /// <summary>
    /// Spawns the powerup the first time the brick gets hit
    /// </summary>
    /// <returns></returns>
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