using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionBlock : MonoBehaviour
{
    [SerializeField] private float popHeight = 0.5f;
    [SerializeField] private float popSpeed = 6f;

    [SerializeField] private Mesh blankBlockMesh;

    [SerializeField] private MeshFilter meshFilter;

    [Header("Staircase Spawn")]
    [SerializeField] private GameObject stepPrefab;
    [SerializeField] private float stepSpacing = 1f;
    [SerializeField] private float spawnDelay = 0.15f;
    [SerializeField] private float clearance = 1.5f;
    [SerializeField] private float jumpHeight = 2.5f;

    [Header("Bridge Spawn")]
    [SerializeField] private float bridgeSpacing = 1f;

    private Vector3 originalPosition;
    private bool isAnimating;
    private bool goingUp;

    private bool active = true;
    public List<GameObject> SpawnedSteps { get; private set; } = new List<GameObject>();

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        if (!isAnimating)
            return;

        if (goingUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition + Vector3.up * popHeight, popSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, originalPosition + Vector3.up * popHeight) < 0.01f)
                goingUp = false;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, popSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
            {
                transform.position = originalPosition;
                isAnimating = false;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var step in SpawnedSteps)
        {
            if (step != null)
                Destroy(step);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isAnimating || !active)
            return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && collision.gameObject.GetComponent<Mario>() != null)
            {
                isAnimating = true;
                goingUp = true;
                active = false;
                meshFilter.mesh = blankBlockMesh;
                if (MarioEffect.Active != null)
                    MarioEffect.Active.StartTimer();
                StartCoroutine(SpawnSteps());
                return;
            }
        }
    }

    private IEnumerator SpawnSteps()
    {
        if (stepPrefab == null)
            yield break;

        Brick[] bricks = FindObjectsByType<Brick>(FindObjectsSortMode.None);
        float targetY = float.MaxValue;

        foreach (Brick brick in bricks)
        {
            if (brick.Health <= 0) continue;
            float brickY = brick.transform.position.y;
            if (brickY > originalPosition.y && brickY < targetY)
                targetY = brickY;
        }

        if (targetY == float.MaxValue)
            yield break;

        float maxStepY = targetY - jumpHeight;

        List<Bounds> brickBounds = new List<Bounds>();
        foreach (Brick brick in bricks)
        {
            if (brick.Health <= 0) continue;
            Collider col = brick.GetComponent<Collider>();
            if (col != null)
                brickBounds.Add(col.bounds);
        }

        float stepHalfSize = 0.5f;
        Collider stepCol = stepPrefab.GetComponent<Collider>();
        if (stepCol != null)
            stepHalfSize = Mathf.Max(stepCol.bounds.extents.x, stepCol.bounds.extents.y);

        // Spawn stairs on both sides, track end position and height
        float rightEndX = 0f, rightEndY = 0f;
        float leftEndX = 0f, leftEndY = 0f;

        yield return StartCoroutine(SpawnSide(1f, maxStepY, brickBounds, stepHalfSize,
            (x, y) => { rightEndX = x; rightEndY = y; }));
        yield return StartCoroutine(SpawnSide(-1f, maxStepY, brickBounds, stepHalfSize,
            (x, y) => { leftEndX = x; leftEndY = y; }));

        // Use the highest actual stair height for the bridge
        float bridgeY = Mathf.Max(leftEndY, rightEndY);

        // Bridge from left staircase to left edge of level, and right staircase to right edge
        // Use brick Z for screen width (bricks are on the main play plane)
        Camera cam = Camera.main;
        float brickZ = bricks.Length > 0 ? bricks[0].transform.position.z : originalPosition.z;
        float halfWidth = CameraUtils.HalfWidth(brickZ);
        float levelMinX = cam.transform.position.x - halfWidth;
        float levelMaxX = cam.transform.position.x + halfWidth;

        yield return StartCoroutine(SpawnBridge(leftEndX, levelMinX, bridgeY));
        yield return StartCoroutine(SpawnBridge(rightEndX, levelMaxX, bridgeY));
    }

    private IEnumerator SpawnSide(float direction, float maxStepY, List<Bounds> brickBounds, float stepHalfSize, System.Action<float, float> onEnd)
    {
        int step = 0;
        float currentY = originalPosition.y + stepSpacing;
        float lastX = originalPosition.x;
        float lastY = originalPosition.y;

        while (currentY <= maxStepY)
        {
            float xOffset = stepSpacing * (step + 2) * direction;

            Vector3 pos = new Vector3(
                originalPosition.x + xOffset,
                currentY,
                originalPosition.z
            );

            bool blocked = false;
            foreach (Bounds b in brickBounds)
            {
                Bounds stepBounds = new Bounds(pos, Vector3.one * stepHalfSize * 2f);
                Bounds expanded = new Bounds(b.center, b.size + Vector3.one * clearance);
                if (stepBounds.Intersects(expanded))
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
            {
                SpawnedSteps.Add(Instantiate(stepPrefab, pos, Quaternion.identity));
                lastX = pos.x;
                lastY = pos.y;
                yield return new WaitForSeconds(spawnDelay);
            }

            currentY += stepSpacing;
            step++;
        }

        onEnd?.Invoke(lastX, lastY);
    }

    private IEnumerator SpawnBridge(float fromX, float toX, float bridgeY)
    {
        float direction = Mathf.Sign(toX - fromX);
        float x = fromX + bridgeSpacing * direction;

        while ((direction > 0 && x < toX) || (direction < 0 && x > toX))
        {
            Vector3 pos = new Vector3(x, bridgeY, originalPosition.z);
            SpawnedSteps.Add(Instantiate(stepPrefab, pos, Quaternion.identity));
            yield return new WaitForSeconds(spawnDelay);
            x += bridgeSpacing * direction;
        }
    }
}
