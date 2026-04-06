using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionBlock : MonoBehaviour
{
    /// <summary>
    /// How far the block pops upward when hit.
    /// </summary>
    [SerializeField] private float popHeight = 0.5f;

    /// <summary>
    /// Speed of the pop up/down animation.
    /// </summary>
    [SerializeField] private float popSpeed = 6f;

    /// <summary>
    /// Sound played when Mario hits the block from below.
    /// </summary>
    [SerializeField] private AudioClip hitAudioClip;

    /// <summary>
    /// Mesh swapped in after the block has been hit (empty/used look).
    /// </summary>
    [SerializeField] private Mesh blankBlockMesh;

    /// <summary>
    /// MeshFilter used to swap between active and blank meshes.
    /// </summary>
    [SerializeField] private MeshFilter meshFilter;

    /// <summary>
    /// Prefab instantiated for each staircase step and bridge segment.
    /// </summary>
    [Header("Staircase Spawn")]
    [SerializeField] private GameObject stepPrefab;

    /// <summary>
    /// Vertical and horizontal distance between each staircase step.
    /// </summary>
    [SerializeField] private float stepSpacing = 1f;

    /// <summary>
    /// Delay in seconds between spawning each step for a sequential effect.
    /// </summary>
    [SerializeField] private float spawnDelay = 0.15f;

    /// <summary>
    /// Extra buffer around bricks to prevent steps from spawning too close.
    /// </summary>
    [SerializeField] private float clearance = 1.5f;

    /// <summary>
    /// Vertical distance below the lowest brick row where stairs stop building.
    /// </summary>
    [SerializeField] private float jumpHeight = 2.5f;

    /// <summary>
    /// Horizontal spacing between bridge segments.
    /// </summary>
    [Header("Bridge Spawn")]
    [SerializeField] private float bridgeSpacing = 1f;

    /// <summary>
    /// Position captured at start, used as the baseline for the pop animation.
    /// </summary>
    private Vector3 originalPosition;

    /// <summary>
    /// True while the pop animation is playing.
    /// </summary>
    private bool isAnimating;

    /// <summary>
    /// True during the upward phase of the pop animation.
    /// </summary>
    private bool goingUp;

    /// <summary>
    /// False after the block has been hit once, preventing re-activation.
    /// </summary>
    private bool active = true;

    /// <summary>
    /// All spawned step and bridge GameObjects, tracked for cleanup on remove.
    /// </summary>
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

        CheckContact(collision);

    }

    /// <summary>
    /// Checks if Mario hit the block from below and activates the block if so.
    /// </summary>
    /// <param name="collision">Scanned for upward normals from a Mario component to confirm a bottom hit.</param>
    private void CheckContact(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && collision.gameObject.GetComponent<Mario>() != null)
            {
                isAnimating = true;
                goingUp = true;
                active = false;
                meshFilter.mesh = blankBlockMesh;
                if (hitAudioClip != null)
                    AudioManager.Instance.PlaySFX(hitAudioClip);
                if (MarioEffect.Active != null)
                    MarioEffect.Active.StartTimer();
                StartCoroutine(SpawnSteps());
                return;
            }
        }
    }

    /// <summary>
    /// Spawns staircases on both sides and connects them with bridges to the screen edges.
    /// </summary>
    /// <returns>Coroutine that sequentially places steps and bridges with delays between each.</returns>
    private IEnumerator SpawnSteps()
    {
        if (stepPrefab == null)
            yield break;

        Brick[] bricks = FindObjectsByType<Brick>();

        float targetY = FindTargetYPosition(bricks);

        if (targetY == float.MaxValue)
            yield break;

        List<Bounds> brickBounds = FindBrickBounds(bricks);

        Collider stepCol = stepPrefab.GetComponent<Collider>();
        float stepHalfSize = stepCol != null ? Mathf.Max(stepCol.bounds.extents.x, stepCol.bounds.extents.y) : 0.5f;

        // Spawn stairs on both sides, track end position and height
        Vector2 rightEnd = Vector2.zero;
        Vector2 leftEnd = Vector2.zero;

        float maxStepY = targetY - jumpHeight;

        yield return StartCoroutine(SpawnSide(1f, maxStepY, brickBounds, stepHalfSize,
            (x, y) => { rightEnd.x = x; rightEnd.y = y; }));
        yield return StartCoroutine(SpawnSide(-1f, maxStepY, brickBounds, stepHalfSize,
            (x, y) => { leftEnd.x = x; leftEnd.y = y; }));


        (float levelMinX, float levelMaxX) = FindBridgeEdges(bricks);

        // Use the highest actual stair height for the bridge
        float bridgeY =  CalculateBridgePositions(leftEnd,rightEnd);

        yield return StartCoroutine(SpawnBridge(leftEnd.x, levelMinX, bridgeY));
        yield return StartCoroutine(SpawnBridge(rightEnd.x, levelMaxX, bridgeY));
    }

    /// <summary>
    /// Enforces a minimum gap around the question block and returns the bridge height.
    /// </summary>
    /// <param name="leftEnd">Left staircase end position; X is pushed outward if too close.</param>
    /// <param name="rightEnd">Right staircase end position; X is pushed outward if too close.</param>
    /// <returns>The Y height for the bridge, taken from the higher staircase end.</returns>
    private float CalculateBridgePositions(Vector2 leftEnd, Vector2 rightEnd)
    {
        // Ensure a gap around the question block so Mario can jump up onto the bridge
        float minBridgeDistance = bridgeSpacing;
        if (Mathf.Abs(leftEnd.x - originalPosition.x) < minBridgeDistance)
            rightEnd.x = originalPosition.x + minBridgeDistance;
        if (Mathf.Abs(leftEnd.x - originalPosition.x) < minBridgeDistance)
            leftEnd.x = originalPosition.x - minBridgeDistance;

        return Mathf.Max(leftEnd.y, rightEnd.y);
    }

    /// <summary>
    /// Finds the left and right screen-edge X positions where bridges should end.
    /// </summary>
    /// <param name="bricks">Used to determine the Z depth for screen-width calculation.</param>
    /// <returns>A tuple of (levelMinX, levelMaxX) at the brick plane depth.</returns>
    private (float, float) FindBridgeEdges(Brick[] bricks)
    {
        Camera cam = Camera.main;
        float brickZ = bricks.Length > 0 ? bricks[0].transform.position.z : originalPosition.z;
        float halfWidth = CameraUtils.HalfWidth(brickZ);
        float levelMinX = cam.transform.position.x - halfWidth;
        float levelMaxX = cam.transform.position.x + halfWidth;
        return (levelMinX, levelMaxX);
    }

    /// <summary>
    /// Look through all brick position to find the lowest Y-Position
    /// </summary>
    /// <param name="bricks">An array of all bricks</param>
    /// <returns>The lowest found Y-Position</returns>
    private float FindTargetYPosition(Brick[] bricks)
    {
        float targetY = float.MaxValue;

        foreach (Brick brick in bricks)
        {
            if (brick.Health <= 0) continue;
            float brickY = brick.transform.position.y;
            if (brickY > originalPosition.y && brickY < targetY)
                targetY = brickY;
        }

        return targetY;
    }

    /// <summary>
    /// Collects the collider bounds of all living bricks for overlap checks.
    /// </summary>
    /// <param name="bricks">Filtered to only include bricks with health above zero.</param>
    /// <returns>List of collider bounds from all surviving bricks.</returns>
    private List<Bounds> FindBrickBounds(Brick[] bricks)
    {
        List<Bounds> brickBounds = new List<Bounds>();
        foreach (Brick brick in bricks)
        {
            if (brick.Health <= 0) continue;
            Collider col = brick.GetComponent<Collider>();
            if (col != null)
                brickBounds.Add(col.bounds);
        }

        return brickBounds;
    }

    /// <summary>
    /// Spawns steps ascending in the given direction, skipping positions blocked by bricks.
    /// </summary>
    /// <param name="direction">1 builds rightward, -1 builds leftward from the question block.</param>
    /// <param name="maxStepY">Highest Y a step can be placed, based on the lowest brick row minus jumpHeight.</param>
    /// <param name="brickBounds">Expanded brick volumes used to skip positions that would overlap existing bricks.</param>
    /// <param name="stepHalfSize">Half the step's collider size, used to build overlap-check bounds.</param>
    /// <param name="onEnd">Receives the final step's X and Y so the bridge knows where to start.</param>
    /// <returns>Coroutine that places steps one at a time with spawnDelay between each.</returns>
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

    /// <summary>
    /// Spawns horizontal bridge segments between two X positions at the given height.
    /// </summary>
    /// <param name="fromX">X of the last staircase step; the bridge starts one spacing beyond this.</param>
    /// <param name="toX">Screen-edge X the bridge extends toward.</param>
    /// <param name="bridgeY">Vertical position shared by all bridge segments.</param>
    /// <returns>Coroutine that places bridge segments with spawnDelay between each.</returns>
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
