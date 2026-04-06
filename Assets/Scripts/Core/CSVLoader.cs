using System.IO;
using System.Linq;
using UnityEngine;

public class CSVLoader : MonoBehaviour, ILevelLoader
{
    /// <summary>
    /// Filename of the CSV level file inside StreamingAssets (e.g. "level.csv").
    /// </summary>
    [SerializeField] private string levelFileName = "level.csv";

    /// <summary>
    /// Prefab used for regular bricks.
    /// </summary>
    [SerializeField] private Brick brickPrefab;

    /// <summary>
    /// Prefab used for bricks that drop a power-up.
    /// </summary>
    [SerializeField] private Brick powerUpPrefab;

    /// <summary>
    /// Fraction of cell size used as spacing between bricks.
    /// </summary>
    [SerializeField][Range(0f, 0.5f)] private float spacingRatio = 0.05f;

    /// <summary>
    /// Fraction of the screen height used for the brick grid.
    /// </summary>
    [SerializeField][Range(0f, 1f)] private float levelHeightRatio = 0.5f;

    /// <summary>
    /// Width-to-height ratio of each brick.
    /// </summary>
    [SerializeField] private float brickAspectRatio = 3f;

    /// <summary>
    /// Multiplier applied to the overall brick grid size.
    /// </summary>
    [SerializeField][Range(0.1f, 2f)] private float scaleFactor = 1f;

    /// <summary>
    /// Vertical offset from the top of the screen to the first row.
    /// </summary>
    [SerializeField] private float topOffset = 1f;

    /// <summary>
    /// Depth thickness of each brick in world units.
    /// </summary>
    [SerializeField] private float brickDepth = 0.3f;

    /// <summary>
    /// Mesh variants assigned to bricks based on their health.
    /// </summary>
    [SerializeField] private MeshFilter[] meshes;

    /// <summary>
    /// Colors assigned to bricks based on their health level.
    /// </summary>
    [SerializeField] private Color[] brickColors;

    /// <summary>
    /// Parsed 2D grid of cell values from the CSV file.
    /// </summary>
    private string[][] grid;

    private void Start()
    {
        LoadLevel();
    }

    /// <summary>
    /// Reads the CSV file from StreamingAssets and parses it into a 2D string grid.
    /// </summary>
    public void ReadFile()
    {
        string path = Path.Combine(Application.streamingAssetsPath, levelFileName);
        string text = File.ReadAllText(path);
        string[] rows = text.Trim().Split('\n');

        grid = new string[rows.Length][];

        for (int i = 0; i < rows.Length; i++)
        {
            grid[i] = rows[i].Trim().Split(';');
        }
    }

    /// <summary>
    /// Reads the level file and instantiates bricks in a grid fitted to the camera view.
    /// </summary>
    public void LoadLevel()
    {
        ReadFile();

        Camera cam = Camera.main;
        float gameplayZ = cam.transform.position.z + 10f;
        float camWidth = CameraUtils.HalfWidth(gameplayZ) * 2f;
        float camHeight = camWidth / cam.aspect;

        int rowCount = grid.Length;
        int colCount = grid.Max(r => r.Length);

        float availableHeight = camHeight * levelHeightRatio;

        // Size based on horizontal fit
        float cellWidthFromCols = camWidth * scaleFactor / colCount;
        float cellHeightFromCols = cellWidthFromCols / brickAspectRatio;

        // Size based on vertical fit
        float cellHeightFromRows = availableHeight / rowCount;
        float cellWidthFromRows = cellHeightFromRows * brickAspectRatio;

        // Use the smaller of the two so bricks fit both horizontally and vertically
        float cellWidth = Mathf.Min(cellWidthFromCols, cellWidthFromRows);
        float cellHeight = cellWidth / brickAspectRatio;

        // Use cell height (smaller dimension) as the base for spacing so gaps are equal on all sides
        float spacing = cellHeight * spacingRatio;
        float targetWidth = cellWidth - spacing;
        float targetHeight = cellHeight - spacing;

        float gridWidth = colCount * cellWidth;
        float gridHeight = rowCount * cellHeight;

        Vector3 camPos = cam.transform.position;
        float startX = camPos.x - gridWidth / 2f;
        float startY = camPos.y + camHeight / 2f - topOffset;

        // Get the native mesh size from the prefab so we can scale to exact world dimensions
        Vector3 meshSize = brickPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.size;

        for (int row = 0; row < grid.Length; row++)
        {
            for (int col = 0; col < grid[row].Length; col++)
            {

                string cell = grid[row][col];
                if (string.IsNullOrEmpty(cell))
                    continue;

                float x = startX + col * cellWidth + cellWidth / 2f;
                float y = startY - row * cellHeight - cellHeight / 2f;

                Brick prefab = cell.Contains('x') ? powerUpPrefab : brickPrefab;

                // After -90° Y rotation: local X -> world Z (depth), local Z -> world X (width)
                Brick brick = Instantiate(prefab, new Vector3(x, y, camPos.z + 10f), Quaternion.Euler(0f, -90f, 0f));
                if (!int.TryParse(cell.Replace("x", ""), out int health))
                    health = 1;
                health = Mathf.Clamp(health, 1, brickColors.Length);
                brick.Health = health;
                brick.BrickColor = brickColors[health - 1];
                brick.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", brickColors[health - 1]);
                brick.transform.localScale = new Vector3(
                    brickDepth / meshSize.x,
                    targetHeight / meshSize.y,
                    targetWidth / meshSize.z
                );
            }
        }
    }
}
