using System.Linq;
using UnityEngine;

public class CSVLoader : MonoBehaviour, ILevelLoader
{
    [SerializeField] private TextAsset level;
    [SerializeField] private Brick brickPrefab;
    [SerializeField] private Brick powerUpPrefab;
    [SerializeField][Range(0f, 0.5f)] private float spacingRatio = 0.05f;
    [SerializeField][Range(0f, 1f)] private float levelHeightRatio = 0.5f;
    [SerializeField] private float brickAspectRatio = 3f;
    [SerializeField][Range(0.1f, 2f)] private float scaleFactor = 1f;
    [SerializeField] private float topOffset = 1f;
    [SerializeField] private float brickDepth = 0.3f;
    [SerializeField] private MeshFilter[] meshes;
    [SerializeField] private Color[] brickColors;

    private string[][] grid;

    private void Start()
    {
        LoadLevel();
    }

    public void ReadFile()
    {
        string[] rows = level.text.Trim().Split('\n');

        grid = new string[rows.Length][];

        for (int i = 0; i < rows.Length; i++)
        {
            grid[i] = rows[i].Trim().Split(';');
        }
    }

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
                int health = 1;

                
                
                int.TryParse(cell.Replace("x", ""), out health);
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
