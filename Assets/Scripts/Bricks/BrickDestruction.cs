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
    /// Min speed for chunks flying off
    /// </summary>
    [SerializeField] private float launchSpeedMin = 2f;

    /// <summary>
    /// Max speed for chunks flying off
    /// </summary>
    [SerializeField] private float launchSpeedMax = 5f;

    /// <summary>
    /// Multiplier used on final hit for more explosive effect
    /// </summary>
    [SerializeField] private float scatterSpeedMultiplier = 1.5f;

    /// <summary>
    /// Downward force used on every chunk each frame
    /// </summary>
    [SerializeField] private float gravity = -9.8f;

    /// <summary>
    /// How long time each chunk lives before being destroyed
    /// </summary>
    [SerializeField] private float chunkLifetime = 2.0f;

    /// <summary>
    /// Amount of time that needs to pass before chunks should start shrinking to zero
    /// </summary>
    [SerializeField] private float shrinkStartTime = 1.2f;

    /// <summary>
    /// A reference to the destroyed brick
    /// </summary>
    [SerializeField] private Brick brick;

    /// <summary>
    /// A prefab for spawning chunks
    /// </summary>
    [SerializeField] private Chunk chunkPrefab;

    /// <summary>
    /// Describes the dimensions of the chunk
    /// </summary>
    private struct ChunkDimensions
    {
        /// <summary>
        /// World-space width per column
        /// </summary>
        public float width;

        /// <summary>
        /// World-space height per row
        /// </summary>
        public float height;

        /// <summary>
        /// Full depth (one layer deep)
        /// </summary>
        public float depth;

        /// <summary>
        /// Tiny gap between chunks to prevent z-fighting
        /// </summary>
        public float gap;

        /// <summary>
        /// Calculates the Chunk's scale
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                return new Vector3(width - gap, height - gap, depth - gap);
            }
        }
    }

    /// <summary>
    /// A list of all chunks
    /// </summary>
    private List<Chunk> chunks = new List<Chunk>();

    /// <summary>
    /// True after InitializeChunks() has run (lazy — only on first hit, not at level load)
    /// </summary>
    private bool initialized;

    /// <summary>
    /// The bricks start health, used to calculate how many chunks to create
    /// </summary>
    private int maxHealth;

    private void Start()
    {
        maxHealth = GetComponent<Brick>().Health;
    }

    /// <summary>
    /// Lazy loads the chunks first time the brick takes a hit
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
    /// Calculates the dimensions of each chunk
    /// </summary>
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
    /// Disables the parent brick after the first hit
    /// </summary>
    private void DisableParentBrick()
    {
        brick.meshRenderer.enabled = false;

        if (brick.Collider != null)
            brick.Collider.enabled = false;
    }

    /// <summary>
    /// Creates a chunk object
    /// </summary>
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
    /// Calculate the center position of this chunk in world space
    /// </summary>
    private Vector3 CalculateWorldCenterPosition(Bounds bounds, ChunkDimensions dimensions, int col)
    {
        float x = bounds.min.x + (col + 0.5f) * dimensions.width;
        float y = bounds.center.y;
        float z = bounds.center.z;

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Finds the closest chunk to the impact point and detaches it.
    /// </summary>
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
    /// All remaining attached chunks explode outward from the brick's center.
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


