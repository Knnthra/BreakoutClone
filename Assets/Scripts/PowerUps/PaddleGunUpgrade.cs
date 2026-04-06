using Unity.Mathematics;
using UnityEngine;

public class PaddleGunUpgrade : MonoBehaviour
{
    /// <summary>
    /// Laser projectile prefab instantiated on each shot.
    /// </summary>
    [SerializeField] private GameObject laserPrefab;

    /// <summary>
    /// Left-side fire point transform.
    /// </summary>
    /// <summary>
    /// Right-side fire point transform.
    /// </summary>
    [SerializeField] private Transform left, right;

    /// <summary>
    /// Fires a laser from both the left and right fire points.
    /// </summary>
    public void Shoot()
    {
        Instantiate(laserPrefab,left.position,quaternion.identity);
        Instantiate(laserPrefab,right.position,quaternion.identity);
    }
}
