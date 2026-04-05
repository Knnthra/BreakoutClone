using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [SerializeField] private float rotaitonSpeed;
    [SerializeField] private float fallSpeed;
    [SerializeField] private PowerUpEffect[] powerUpEffects;

    private static int powerUpIndex;

    private void Update()
    {
        transform.Rotate(new Vector3(0, rotaitonSpeed * Time.deltaTime, 0));
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.IsGameOver) return;

        if (other.TryGetComponent(out PowerUpEffectHandler handler))
        {
            handler.Apply(powerUpEffects[powerUpIndex]);
            powerUpIndex = (powerUpIndex + 1) % powerUpEffects.Length;
            Destroy(gameObject);
        }
    }
}
