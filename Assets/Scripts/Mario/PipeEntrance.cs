using System;
using System.Collections;
using UnityEngine;

public class PipeEntrance : MonoBehaviour
{
    [SerializeField] private float riseHeight = 2f;
    [SerializeField] private float riseSpeed = 3f;
    [SerializeField] private AudioClip pipeAudioClip;

    public void Rise(Transform target, Action onComplete)
    {
        target.position = transform.position;
        StartCoroutine(RiseCoroutine(target, onComplete));
    }

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
