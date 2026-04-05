using System;
using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Paddle Gun")]
public class PaddleGunEffect : PowerUpEffect
{
    [SerializeField] private PaddleGunUpgrade upgradePrefab;
    [SerializeField] private Material wireframeMaterial;
    [SerializeField] private float buildDuration = 1f;

    [SerializeField] private AudioClip laserSound;

    private PaddleGunUpgrade spawnedUpgrade;
    private WireframeBuildUp wireframeBuildUp;

    

    public override void Apply(PowerUpEffectHandler handler)
    {
        handler.Paddle.BallHit += Shoot;
        effectHandler = handler;

        if (effectHandler == null || upgradePrefab == null)
            return;

        spawnedUpgrade = Instantiate(upgradePrefab, handler.VisualTransform);
        spawnedUpgrade.transform.localPosition = Vector3.zero;
        spawnedUpgrade.transform.localRotation = Quaternion.identity;

        wireframeBuildUp = spawnedUpgrade.gameObject.AddComponent<WireframeBuildUp>();
        wireframeBuildUp.Configure(wireframeMaterial, buildDuration);
        wireframeBuildUp.StartBuildIn();
    }

    public override void Remove()
    {
        if (spawnedUpgrade != null && wireframeBuildUp != null)
        {
            GameObject upgradeObject = spawnedUpgrade.gameObject;
            wireframeBuildUp.StartBuildOut(() =>
            {
                Destroy(upgradeObject);
            });
        }   
        else if (spawnedUpgrade != null)
        {
            Destroy(spawnedUpgrade.gameObject);
        }

        spawnedUpgrade = null;
        wireframeBuildUp = null;
        effectHandler.Paddle.BallHit -= Shoot;
        base.Remove();
    }

    public void Shoot()
    {
        spawnedUpgrade.Shoot();
        AudioManager.Instance.PlaySFX(laserSound);
    }
}
