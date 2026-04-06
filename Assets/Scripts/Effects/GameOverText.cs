using UnityEngine;

public class GameOverText : MonoBehaviour
{
    /// <summary>
    /// World-space position the text moves toward.
    /// </summary>
    [SerializeField] private Vector3 targetPosition;

    /// <summary>
    /// Movement speed in units per second.
    /// </summary>
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}
