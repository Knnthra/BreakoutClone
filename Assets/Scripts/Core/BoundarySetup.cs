using UnityEngine;

public class BoundarySetup : MonoBehaviour
{
    /// <summary>
    /// Thickness of the wall colliders. Must be wide enough to prevent tunneling.
    /// </summary>
    [SerializeField] private float wallThickness = 2f;

    /// <summary>
    /// If true, creates a bottom wall. If false, the ball falls out the bottom as a death zone.
    /// </summary>
    [SerializeField] private bool createBottomWall = false;

    /// <summary>
    /// Parent transform for the spawned wall GameObjects.
    /// </summary>
    [SerializeField] private Transform wallParent;

    private void Start()
    {
       (Vector2 center, Vector2 halfSize, float gameplayZ) = CalculateVisibleGamePlayArea();

        CreateWalls(center,halfSize, gameplayZ);

    }

    /// <summary>
    /// Calculates the visible gameplay area at the gameplay Z depth.
    /// </summary>
    /// <returns>The screen center, half-size of the visible area, and the gameplay Z depth.</returns>
    private (Vector2, Vector2, float gameplayZ) CalculateVisibleGamePlayArea()
    {
        Camera cam = Camera.main;
        float gameplayZ = cam.transform.position.z + 10f;

        // Calculate the visible area at the gameplay Z depth
        float halfWidth = CameraUtils.HalfWidth(gameplayZ);
        float halfHeight = CameraUtils.HalfHeight(gameplayZ);
        Vector2 halfSize = new Vector2(halfWidth, halfHeight);

        float centerX = cam.transform.position.x;
        float centerY = cam.transform.position.y;
        Vector2 center = new Vector2(centerX, centerY);

        return (center,halfSize,gameplayZ);
    }

    /// <summary>
    /// Creates the left, right, top, and optionally bottom boundary walls.
    /// </summary>
    /// <param name="center">Screen center position used to place walls symmetrically.</param>
    /// <param name="halfSize">Half the visible area's width and height, defining wall offsets.</param>
    /// <param name="gameplayZ">Z depth at which the walls are placed.</param>
    private void CreateWalls(Vector2 center, Vector2 halfSize, float gameplayZ)
    {
                // Left wall
        CreateWall("Wall_Left",
            new Vector3(center.x - halfSize.x - wallThickness / 2f, center.y, gameplayZ),
            new Vector3(wallThickness, halfSize.y * 2f + wallThickness * 2f, wallThickness));

        // Right wall
        CreateWall("Wall_Right",
            new Vector3(center.x + halfSize.x + wallThickness / 2f, center.y, gameplayZ),
            new Vector3(wallThickness, halfSize.y * 2f + wallThickness * 2f, wallThickness));

        // Top wall
        CreateWall("Wall_Top",
            new Vector3(center.x, center.y + halfSize.y + wallThickness / 2f, gameplayZ),
            new Vector3(halfSize.x * 2f + wallThickness * 2f, wallThickness, wallThickness));

        // Bottom wall (optional — usually this is the death zone)
        if (createBottomWall)
        {
            CreateWall("Wall_Bottom",
                new Vector3(center.x, center.y - halfSize.y - wallThickness / 2f, gameplayZ),
                new Vector3(halfSize.x * 2f + wallThickness * 2f, wallThickness, wallThickness));
        }
    }

    /// <summary>
    /// Creates an invisible box collider wall at the given position and size.
    /// </summary>
    /// <param name="name">GameObject name for the wall (e.g. "Wall_Left").</param>
    /// <param name="position">World-space center of the wall collider.</param>
    /// <param name="size">Dimensions of the box collider.</param>
    private void CreateWall(string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        wall.transform.parent = wallParent;
    }

}
