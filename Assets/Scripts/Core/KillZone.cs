using UnityEngine;

public class KillZone : MonoBehaviour
{
    /// <summary>
    /// Reference to the ball, used to reset it after losing a life.
    /// </summary>
    [SerializeField] private Ball ball;
    [SerializeField] private AudioClip deathSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ball _))
        {
            if (GameManager.Instance.IsGameOver)
                return;

            if (deathSound != null) AudioManager.Instance.PlaySFX(deathSound);
            LifeManager.Instance.RemoveLife();

            if (!GameManager.Instance.IsGameOver)
                ball.ResetBall();
        }
    }
}
