using UnityEngine;

public class KillZone : MonoBehaviour
{
    [SerializeField] private Ball ball;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ball _))
        {
            LifeManager.Instance.RemoveLife();

            if (!GameManager.Instance.IsGameOver)
                ball.ResetBall();
        }
    }
}
