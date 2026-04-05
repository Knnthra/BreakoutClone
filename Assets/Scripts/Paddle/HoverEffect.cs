using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    [SerializeField] private float bobAmount = 0.03f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float tiltAmount = 0.8f;
    [SerializeField] private float tiltSpeed = 1.3f;
    [SerializeField] private float jitterAmount = 0.005f;

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private float bobOffset;
    private float tiltOffset;

    private void Start()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
        // Random offset so multiple objects don't bob in sync
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        tiltOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float t = Time.time;

        // Smooth bob up/down using two overlapping sine waves for organic feel
        float bob = Mathf.Sin(t * bobSpeed + bobOffset) * bobAmount
                  + Mathf.Sin(t * bobSpeed * 1.7f + bobOffset) * bobAmount * 0.3f;

        // Tiny random jitter on Y only to simulate engine vibration
        float jitterY = (Mathf.PerlinNoise(t * 15f, 0f) - 0.5f) * 2f * jitterAmount;

        transform.localPosition = startLocalPos + new Vector3(0f, bob + jitterY, 0f);

        // Gentle tilt forward/back around X axis
        float tilt = Mathf.Sin(t * tiltSpeed + tiltOffset) * tiltAmount
                   + Mathf.Sin(t * tiltSpeed * 2.1f + tiltOffset) * tiltAmount * 0.2f;

        transform.localRotation = startLocalRot * Quaternion.Euler(tilt, 0f, 0f);
    }
}
