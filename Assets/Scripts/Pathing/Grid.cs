using UnityEngine;
using System.Collections.Generic;

public class Grid : MonoBehaviour
{
    [SerializeField] GameObject tilePrefab;
    [SerializeField] int iterations = 500; // BFS/Dijkstra iteration limit
    [SerializeField] bool useDijkstra = false;

    // Constants for tile types
    const int AIR = 0;
    const int ROCK = 1;
    const int WATER = 2;
    const int GRASS = 3;
    const int TILE_TYPE_COUNT = 4;

    // Grid size
    const int rows = 10;
    const int cols = 20;

    // Layout of the grid
    int[,] tiles =
    {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
    };

    // Rendering references
    List<List<GameObject>> tileObjects = new List<List<GameObject>>();

    // Path follower variables
    GameObject pathFollower;
    List<Vector3> pathPoints = new List<Vector3>();
    int pathCurr = 0;
    int pathNext = 1;
    float pathT = 0.0f;

    void Start()
    {
        // Instantiate the grid visually
        float y = 9.5f;
        for (int r = 0; r < rows; r++)
        {
            List<GameObject> rowObjects = new List<GameObject>();
            float x = 0.5f;
            for (int c = 0; c < cols; c++)
            {
                GameObject tile = Instantiate(tilePrefab);
                tile.transform.position = new Vector3(x, y, 0f);
                rowObjects.Add(tile);
                x += 1.0f;
            }
            tileObjects.Add(rowObjects);
            y -= 1.0f;
        }

        // Create a path follower tile
        pathFollower = Instantiate(tilePrefab);
        pathFollower.GetComponent<SpriteRenderer>().color = Color.red;
    }

    void Update()
    {
        // Redraw the grid in its default colors each frame
        DrawTiles();

        // Define a start and end cell for BFS or Dijkstra
        Cell start = new Cell { row = 7, col = 3 };
        Cell end = new Cell { row = 2, col = 16 };

        // Use BFS or Dijkstra
        List<Cell> path = useDijkstra
            ? Pathing.Dijkstra(start, end, tiles, iterations, this)
            : Pathing.FloodFill(start, end, tiles, iterations, this);

        // Draw the path in cyan
        foreach (Cell cell in path)
            DrawCell(cell, Color.cyan);

        // Draw start in green and end in red
        DrawCell(start, Color.green);
        DrawCell(end, Color.red);

        // Convert the BFS path from Cells to world positions
        pathPoints.Clear();
        foreach (Cell c in path)
        {
            pathPoints.Add(GridToWorld(c));
        }

        // Move the path follower along the BFS path
        if (pathPoints.Count >= 2)
        {
            pathT += Time.deltaTime;
            if (pathT >= 1.0f)
            {
                pathT = 0.0f;
                pathCurr = pathNext;
                pathNext++;

                // If we've reached or exceeded the last point, clamp or loop
                if (pathNext >= pathPoints.Count)
                {
                    // Option 1: Clamp at the end
                    pathNext = pathPoints.Count - 1;

                    // Option 2: Loop back to the start
                    // pathNext = 0;
                    // pathCurr = 0;
                }
            }

            // Lerp between pathCurr and pathNext
            pathFollower.transform.position = Vector3.Lerp(
                pathPoints[pathCurr],
                pathPoints[pathNext],
                pathT
            );
        }

        // Highlight the tile under the mouse (magenta)
        DrawMouseTiles();
    }

    /// <summary>
    /// Colors a single cell in the grid.
    /// </summary>
    public void DrawCell(Cell cell, Color color)
    {
        GameObject obj = tileObjects[cell.row][cell.col];
        obj.GetComponent<SpriteRenderer>().color = color;
    }

    /// <summary>
    /// Returns the cost of moving onto a tile of the given type.
    /// Used by Dijkstra.
    /// </summary>
    public float TileCost(int type)
    {
        float[] costs = new float[TILE_TYPE_COUNT];
        costs[AIR] = 1.0f;
        costs[ROCK] = 100.0f;
        costs[WATER] = 25.0f;
        costs[GRASS] = 10.0f;
        return costs[type];
    }

    /// <summary>
    /// Returns a color for each tile type.
    /// </summary>
    public Color TileColor(int type)
    {
        Color[] colors = new Color[TILE_TYPE_COUNT];
        colors[AIR] = Color.white;
        colors[ROCK] = Color.gray;
        colors[WATER] = Color.blue;
        colors[GRASS] = Color.green;
        return colors[type];
    }

    /// <summary>
    /// Returns the tile type for a given cell.
    /// </summary>
    public int TileType(Cell cell)
    {
        return tiles[cell.row, cell.col];
    }

    /// <summary>
    /// If we want to debug-draw BFS expansions, return true.
    /// </summary>
    public bool IsDebugDraw()
    {
        return true;
    }

    /// <summary>
    /// Colors the entire grid in its default tile colors.
    /// </summary>
    void DrawTiles()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Cell cell = new Cell { row = r, col = c };
                int type = tiles[r, c];
                DrawCell(cell, TileColor(type));
            }
        }
    }

    /// <summary>
    /// Colors the tile under the mouse in magenta (for debug).
    /// </summary>
    void DrawMouseTiles()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main Camera is missing or not tagged as 'MainCamera'!");
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Cell mouseCell = WorldToGrid(mousePos);

        if (!Cell.Equals(mouseCell, Cell.Invalid()))
        {
            DrawCell(mouseCell, Color.magenta);
        }
    }

    /// <summary>
    /// Converts world-space coordinates to a grid cell.
    /// row 0 is the top row in your code, so we do (rows - 1 - (int)pos.y).
    /// </summary>
    Cell WorldToGrid(Vector3 pos)
    {
        if (pos.x < 0f || pos.x > cols || pos.y < 0f || pos.y > rows)
            return Cell.Invalid();

        Cell cell = new Cell();
        cell.col = (int)pos.x;
        cell.row = (rows - 1) - (int)pos.y;
        return cell;
    }

    /// <summary>
    /// Converts a grid cell back to world-space coordinates
    /// so we can position a tile or follower at (x+0.5, y+0.5).
    /// </summary>
    Vector3 GridToWorld(Cell cell)
    {
        float x = cell.col + 0.5f;
        float y = (rows - 1 - cell.row) + 0.5f;
        return new Vector3(x, y, 0f);
    }
}
