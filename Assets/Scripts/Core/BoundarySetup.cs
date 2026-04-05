using UnityEngine;

// Spawns invisible wall colliders at the camera edges so the ball and paddle stay in bounds.
// Works with both perspective and orthographic cameras.
// Attach to any GameObject in the scene (e.g. an empty "GameManager").
public class BoundarySetup : MonoBehaviour
{
    // How thick the walls are (needs to be wide enough that the ball can't tunnel through)
    [SerializeField] private float wallThickness = 2f;

    // If true, creates a bottom wall. If false, the ball falls out the bottom (death zone).
    [SerializeField] private bool createBottomWall = false;

    [SerializeField] private Transform wallParent;

    private void Start()
    {
        Camera cam = Camera.main;
        float gameplayZ = cam.transform.position.z + 10f;

        // Calculate the visible area at the gameplay Z depth
        float halfWidth = CameraUtils.HalfWidth(gameplayZ);
        float halfHeight = CameraUtils.HalfHeight(gameplayZ);
        float centerX = cam.transform.position.x;
        float centerY = cam.transform.position.y;

        // Left wall
        CreateWall("Wall_Left",
            new Vector3(centerX - halfWidth - wallThickness / 2f, centerY, gameplayZ),
            new Vector3(wallThickness, halfHeight * 2f + wallThickness * 2f, wallThickness));

        // Right wall
        CreateWall("Wall_Right",
            new Vector3(centerX + halfWidth + wallThickness / 2f, centerY, gameplayZ),
            new Vector3(wallThickness, halfHeight * 2f + wallThickness * 2f, wallThickness));

        // Top wall
        CreateWall("Wall_Top",
            new Vector3(centerX, centerY + halfHeight + wallThickness / 2f, gameplayZ),
            new Vector3(halfWidth * 2f + wallThickness * 2f, wallThickness, wallThickness));

        // Bottom wall (optional — usually this is the death zone)
        if (createBottomWall)
        {
            CreateWall("Wall_Bottom",
                new Vector3(centerX, centerY - halfHeight - wallThickness / 2f, gameplayZ),
                new Vector3(halfWidth * 2f + wallThickness * 2f, wallThickness, wallThickness));
        }
    }

    private void CreateWall(string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        wall.transform.parent = wallParent;
    }

}
