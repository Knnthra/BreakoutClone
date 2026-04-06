using UnityEngine;

public class PowerUp : MonoBehaviour
{
    /// <summary>
    /// Y-axis rotation speed in degrees per second.
    /// </summary>
    [SerializeField] private float rotaitonSpeed;

    /// <summary>
    /// Downward fall speed in units per second.
    /// </summary>
    [SerializeField] private float fallSpeed;

    /// <summary>
    /// Array of possible effects this power-up can apply. Cycled through sequentially.
    /// </summary>
    [SerializeField] private PowerUpEffect[] powerUpEffects;

    /// <summary>
    /// Static index cycling through powerUpEffects so each pickup gives a different effect.
    /// </summary>
    private static int powerUpIndex;

    private void Update()
    {
        transform.Rotate(new Vector3(0, rotaitonSpeed * Time.deltaTime, 0));
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.IsGameOver) return;

        ApplyPowerUp(other.GetComponent<Paddle>());
    }

    /// <summary>
    /// Applies a powerup to a target
    /// </summary>
    /// <param name="target"></param>
    private void ApplyPowerUp(Paddle target)
    {
        if (target != null)
        {
            PowerUpEffectHandler.Instance.Apply(powerUpEffects[powerUpIndex]);
            powerUpIndex = (powerUpIndex + 1) % powerUpEffects.Length;
            Destroy(gameObject);
        }
    }
}
