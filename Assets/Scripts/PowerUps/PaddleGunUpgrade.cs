using Unity.Mathematics;
using UnityEngine;

public class PaddleGunUpgrade : MonoBehaviour
{
    [SerializeField] private GameObject laserPrefab;

    [SerializeField] private Transform left, right;

    public void Shoot()
    {
        Instantiate(laserPrefab,left.position,quaternion.identity);
        Instantiate(laserPrefab,right.position,quaternion.identity);
    }
}
