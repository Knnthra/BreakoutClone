using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces the brick's solid mesh with a grid of small cubes on first hit.
/// Each subsequent hit breaks off cubes near the impact point.
/// When the brick takes a final hit, all remaining cubes scatter outward.
/// </summary>
public class BrickDestruction : MonoBehaviour
{
    /// <summary>
    /// Minimum launch speed for detached chunks.
    /// </summary>
    [SerializeField] private float launchSpeedMin = 2f;

    /// <summary>
    /// Maximum launch speed for detached chunks.
    /// </summary>
    [SerializeField] private float launchSpeedMax = 5f;

    /// <summary>
    /// Velocity multiplier applied during final scatter for a more explosive effect.
    /// </summary>
    [SerializeField] private float scatterSpeedMultiplier = 1.5f;

    /// <summary>
    /// Downward acceleration applied to every detached chunk each frame.
    /// </summary>
    [SerializeField] private float gravity = -9.8f;

    /// <summary>
    /// Seconds a detached chunk lives before being destroyed.
    /// </summary>
    [SerializeField] private float chunkLifetime = 2.0f;

    /// <summary>
    /// Seconds after detach before a chunk begins shrinking to zero.
    /// </summary>
    [SerializeField] private float shrinkStartTime = 1.2f;

    /// <summary>
    /// Reference to the parent brick this destruction component belongs to.
    /// </summary>
    [SerializeField] private Brick brick;

    /// <summary>
    /// Prefab instantiated for each chunk in the grid.
    /// </summary>
    [SerializeField] private Chunk chunkPrefab;

    /// <summary>
    /// Holds the dimensions of a single chunk in the grid.
    /// </summary>
    private struct ChunkDimensions
    {
        /// <summary>
        /// World-space width per column.
        /// </summary>
        public float width;

        /// <summary>
        /// World-space height (full brick height).
        /// </summary>
        public float height;

        /// <summary>
        /// World-space depth (full brick depth).
        /// </summary>
        public float depth;

        /// <summary>
        /// Tiny gap between chunks to prevent z-fighting.
        /// </summary>
        public float gap;

        /// <summary>
        /// Returns the final scale vector with gaps subtracted.
        /// </summary>
        /// <returns>Width/height/depth minus the gap, ready to assign as localScale.</returns>
        public Vector3 Scale
        {
            get
            {
                return new Vector3(width - gap, height - gap, depth - gap);
            }
        }
    }

    /// <summary>
    /// All chunks created for this brick.
    /// </summary>
    private List<Chunk> chunks = new List<Chunk>();

    /// <summary>
    /// True after chunks have been lazily created on first hit.
    /// </summary>
    private bool initialized;

    /// <summary>
    /// The brick's starting health, determines how many chunks to create.
    /// </summary>
    private int maxHealth;

    private void Start()
    {
        maxHealth = GetComponent<Brick>().Health;
    }

    /// <summary>
    /// Lazily creates the chunk grid the first time the brick is hit.
    /// </summary>
    private void InitializeChunks()
    {
        initialized = true;

        ChunkDimensions chunkDimensions = CalculateChunkDimensions(brick.meshRenderer);

        // Create one chunk per health point
        for (int col = 0; col < maxHealth; col++)
        {
            CreateChunkObject(chunkDimensions, col);
        }

        DisableParentBrick();
    }

    /// <summary>
    /// Calculates each chunk's world-space dimensions from the brick's renderer bounds.
    /// </summary>
    /// <param name="brickRenderer">Provides world bounds to divide into equal-width columns.</param>
    /// <returns>Per-chunk sizing with width split by health count and a small visual gap.</returns>
    private ChunkDimensions CalculateChunkDimensions(MeshRenderer brickRenderer)
    {
        ChunkDimensions cd = new ChunkDimensions();

        Bounds bounds = brickRenderer.bounds;

        cd.width = bounds.size.x / maxHealth;
        cd.height = bounds.size.y;
        cd.depth = bounds.size.z;
        cd.gap = 0.002f;

        return cd;
    }

    /// <summary>
    /// Hides the parent brick's mesh and collider after chunks are created.
    /// </summary>
    private void DisableParentBrick()
    {
        brick.meshRenderer.enabled = false;

        if (brick.Collider != null)
            brick.Collider.enabled = false;
    }

    /// <summary>
    /// Instantiates a single chunk at the correct grid position.
    /// </summary>
    /// <param name="chunkDimensions">Sizing data applied to the chunk's scale and grid offset.</param>
    /// <param name="col">Zero-based column determining the chunk's horizontal position.</param>
    private void CreateChunkObject(ChunkDimensions chunkDimensions, int col)
    {
        Chunk chunk = Instantiate(chunkPrefab);

        chunk.transform.position = CalculateWorldCenterPosition(brick.meshRenderer.bounds, chunkDimensions, col);
        chunk.transform.localScale = chunkDimensions.Scale;
        chunk.transform.SetParent(transform, true);

        chunk.Initialize(brick, gravity, chunkLifetime, shrinkStartTime);

        chunks.Add(chunk);
    }

    /// <summary>
    /// Returns the world-space center position for a chunk at the given column index.
    /// </summary>
    /// <param name="bounds">Brick's world bounds used as the grid origin.</param>
    /// <param name="dimensions">Provides column width for horizontal offset calculation.</param>
    /// <param name="col">Zero-based column determining horizontal offset from the left edge.</param>
    /// <returns>Center point for placing the chunk at the given column.</returns>
    private Vector3 CalculateWorldCenterPosition(Bounds bounds, ChunkDimensions dimensions, int col)
    {
        float x = bounds.min.x + (col + 0.5f) * dimensions.width;
        float y = bounds.center.y;
        float z = bounds.center.z;

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Detaches the closest attached chunk to the impact point.
    /// </summary>
    /// <param name="worldPoint">Where the hit landed, used to find the nearest attached chunk.</param>
    /// <param name="hitNormal">Impact direction passed to the chunk's launch calculation.</param>
    public void BreakChunksAtPoint(Vector3 worldPoint, Vector3 hitNormal)
    {
        // Lazy init: create the chunk grid on first hit
        if (!initialized) InitializeChunks();

        // Find the closest attached chunk to the impact point
        Chunk closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < chunks.Count; i++)
        {
            if (chunks[i] == null || !chunks[i].IsAttached) continue;

            float dist = Vector3.SqrMagnitude(chunks[i].transform.position - worldPoint);

            if (dist < closestDist)
            {
                closest = chunks[i];
                closestDist = dist;
            }
        }

        if (closest == null) return;

        // Detach exactly one chunk per hit (one chunk = one health point)
        closest.Detach(worldPoint, hitNormal, launchSpeedMin, launchSpeedMax);
    }

    /// <summary>
    /// Detaches all remaining chunks outward from the brick's center for the final explosion.
    /// </summary>
    public void ScatterAll()
    {
        // If the brick was one-shot killed before taking partial damage, initialize first
        if (!initialized) InitializeChunks();

        Bounds bounds = brick.meshRenderer.bounds;
        Vector3 center = bounds.center;

        for (int i = 0; i < chunks.Count; i++)
        {
            if (chunks[i] == null || !chunks[i].IsAttached) continue;

            // Direction from center to this chunk — each chunk flies outward
            Vector3 outDir = (chunks[i].transform.position - center).normalized;

            // Fallback for chunks exactly at center
            if (outDir.sqrMagnitude < 0.001f)
                outDir = Random.onUnitSphere;

            chunks[i].Detach(center, outDir, launchSpeedMin, launchSpeedMax);
            chunks[i].BoostVelocity(scatterSpeedMultiplier);
        }
    }
}
