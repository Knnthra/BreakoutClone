using UnityEngine;

public class GameOverText : MonoBehaviour
{
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}
