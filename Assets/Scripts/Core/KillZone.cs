using UnityEngine;

public class KillZone : MonoBehaviour
{
    /// <summary>
    /// Reference to the ball, used to reset it after losing a life.
    /// </summary>
    [SerializeField] private Ball ball;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ball _))
        {
            if (GameManager.Instance.IsGameOver)
                return;

            LifeManager.Instance.RemoveLife();

            if (!GameManager.Instance.IsGameOver)
                ball.ResetBall();
        }
    }
}
