using System;
using System.Collections;
using UnityEngine;

public class PipeEntrance : MonoBehaviour
{
    /// <summary>
    /// How far the target rises above the pipe in world units.
    /// </summary>
    [SerializeField] private float riseHeight = 2f;

    /// <summary>
    /// Speed of the rise animation in units per second.
    /// </summary>
    [SerializeField] private float riseSpeed = 3f;

    /// <summary>
    /// Sound played when the rise animation starts.
    /// </summary>
    [SerializeField] private AudioClip pipeAudioClip;

    /// <summary>
    /// Positions the target at the pipe and starts the rise animation coroutine.
    /// </summary>
    /// <param name="target">Snapped to the pipe's position, then animated upward by riseHeight.</param>
    /// <param name="onComplete">Called after the rise finishes, typically to enable Mario's input.</param>
    public void Rise(Transform target, Action onComplete)
    {
        target.position = transform.position;
        StartCoroutine(RiseCoroutine(target, onComplete));
    }

    /// <summary>
    /// Animates the target rising out of the pipe with physics temporarily disabled.
    /// </summary>
    /// <param name="target">Moved upward each frame; its Rigidbody and Collider are temporarily disabled.</param>
    /// <param name="onComplete">Fired after the target reaches the end position and physics are restored.</param>
    /// <returns>Coroutine that animates the rise and restores physics on completion.</returns>
    private IEnumerator RiseCoroutine(Transform target, Action onComplete)
    {
        // Disable physics so Mario passes through the pipe
        Rigidbody rb = target.GetComponent<Rigidbody>();
        Collider col = target.GetComponent<Collider>();

        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }
        if (col != null)
            col.enabled = false;

        if (pipeAudioClip != null)
            AudioManager.Instance.PlaySFX(pipeAudioClip);

        Vector3 endPos = transform.position + Vector3.up * riseHeight;

        while (target.position.y < endPos.y)
        {
            target.position = Vector3.MoveTowards(target.position, endPos, riseSpeed * Time.deltaTime);
            yield return null;
        }

        // Restore physics
        if (rb != null)
            rb.isKinematic = wasKinematic;
        if (col != null)
            col.enabled = true;

        onComplete?.Invoke();
    }
}
