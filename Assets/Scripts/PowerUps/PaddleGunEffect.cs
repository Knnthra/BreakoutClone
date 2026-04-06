using System;
using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Paddle Gun")]
public class PaddleGunEffect : PowerUpEffect
{
    /// <summary>
    /// Prefab for the gun upgrade attached to the paddle.
    /// </summary>
    [SerializeField] private PaddleGunUpgrade upgradePrefab;

    /// <summary>
    /// Material used for the wireframe build-in/out animation.
    /// </summary>
    [SerializeField] private Material wireframeMaterial;

    /// <summary>
    /// Duration of the wireframe build animation in seconds.
    /// </summary>
    [SerializeField] private float buildDuration = 1f;

    /// <summary>
    /// Sound played each time the gun fires.
    /// </summary>
    [SerializeField] private AudioClip laserSound;

    /// <summary>
    /// Instance of the spawned gun upgrade on the paddle.
    /// </summary>
    private PaddleGunUpgrade spawnedUpgrade;

    /// <summary>
    /// Wireframe animator attached to the spawned upgrade.
    /// </summary>
    private WireframeBuildUp wireframeBuildUp;

    /// <summary>
    /// Spawns the gun upgrade on the paddle with a wireframe build-in animation.
    /// </summary>
    /// <param name="handler">Provides paddle references; Shoot is subscribed to its BallHit event.</param>
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

    /// <summary>
    /// Removes the gun upgrade with a wireframe build-out animation.
    /// </summary>
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

    /// <summary>
    /// Fires both lasers and plays the laser sound effect.
    /// </summary>
    public void Shoot()
    {
        spawnedUpgrade.Shoot();
        AudioManager.Instance.PlaySFX(laserSound);
    }
}
