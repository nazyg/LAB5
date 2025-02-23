using System.Collections.Generic;
using UnityEngine;
using Utils; // If you have a PriorityQueue in a Utils namespace

public struct Cell
{
    public int row;
    public int col;

    public static bool Equals(Cell a, Cell b)
    {
        return a.row == b.row && a.col == b.col;
    }

    public static Cell Invalid()
    {
        return new Cell { row = -1, col = -1 };
    }
}

public struct Node
{
    public Cell curr;   // Current cell
    public Cell prev;   // Parent cell (where we came from)
    public float cost;  // Used in Dijkstra
}

public static class Pathing
{
    /// <summary>
    /// FloodFill (BFS) pathfinding. Iteration-limited for debug visualization.
    /// </summary>
    public static List<Cell> FloodFill(Cell start, Cell end, int[,] tiles, int iterations, Grid grid)
    {
        int rows = tiles.GetLength(0);
        int cols = tiles.GetLength(1);

        // Mark blocked cells (e.g. tile==1 is ROCK)
        bool[,] closed = new bool[rows, cols];
        Node[,] nodes = new Node[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // If tile is 1 => ROCK => block it
                closed[r, c] = (tiles[r, c] == 1);
                nodes[r, c].curr = new Cell { row = r, col = c };
                nodes[r, c].prev = Cell.Invalid();
            }
        }

        Queue<Cell> open = new Queue<Cell>();
        open.Enqueue(start);

        bool found = false;
        List<Cell> debugCells = new List<Cell>();

        // Limit expansions to 'iterations' for step-by-step visualization
        for (int i = 0; i < iterations; i++)
        {
            if (open.Count == 0)
                break;

            Cell front = open.Dequeue();

            // For debug: track which cells we've explored
            if (grid.IsDebugDraw() && !closed[front.row, front.col])
                debugCells.Add(front);

            // Mark this cell as visited
            closed[front.row, front.col] = true;

            // If we've reached the goal, stop
            if (Cell.Equals(front, end))
            {
                found = true;
                break;
            }

            // Enqueue neighbors
            foreach (Cell adj in Adjacent(front, rows, cols))
            {
                if (!closed[adj.row, adj.col])
                {
                    open.Enqueue(adj);
                    nodes[adj.row, adj.col].prev = front;
                }
            }
        }

        if (!found)
        {
            // Color all visited cells magenta if we didn't reach the end
            foreach (Cell cell in debugCells)
                grid.DrawCell(cell, Color.magenta);
            return new List<Cell>();
        }

        // Retrace path from end to start
        return Retrace(nodes, start, end);
    }

    /// <summary>
    /// Dijkstra's Algorithm using a priority queue (for weighted costs).
    /// </summary>
    public static List<Cell> Dijkstra(Cell start, Cell end, int[,] tiles, int iterations, Grid grid)
    {
        int rows = tiles.GetLength(0);
        int cols = tiles.GetLength(1);

        Node[,] nodes = new Node[rows, cols];

        // Initialize each cell's cost to max, except the start
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                nodes[r, c].curr = new Cell { row = r, col = c };
                nodes[r, c].prev = Cell.Invalid();
                nodes[r, c].cost = float.MaxValue;
            }
        }

        PriorityQueue<Cell, float> open = new PriorityQueue<Cell, float>();
        open.Enqueue(start, 0.0f);
        nodes[start.row, start.col].cost = 0.0f;

        bool found = false;
        HashSet<Cell> debugCells = new HashSet<Cell>();

        // Similar iteration limit for debug visualization
        for (int i = 0; i < iterations; i++)
        {
            if (open.Count == 0)
                break;

            Cell front = open.Dequeue();
            if (Cell.Equals(front, end))
            {
                found = true;
                break;
            }

            if (grid.IsDebugDraw())
                debugCells.Add(front);

            // Explore neighbors
            foreach (Cell adj in Adjacent(front, rows, cols))
            {
                // Current path cost + tile cost
                float newCost = nodes[front.row, front.col].cost + grid.TileCost(grid.TileType(adj));
                float oldCost = nodes[adj.row, adj.col].cost;

                if (newCost < oldCost)
                {
                    nodes[adj.row, adj.col].cost = newCost;
                    nodes[adj.row, adj.col].prev = front;
                    open.Enqueue(adj, newCost);
                }
            }
        }

        if (!found)
        {
            // Color visited cells magenta if no path to end
            foreach (Cell cell in debugCells)
                grid.DrawCell(cell, Color.magenta);
            return new List<Cell>();
        }

        return Retrace(nodes, start, end);
    }

    /// <summary>
    /// Retrace path from end to start using stored parent pointers.
    /// </summary>
    static List<Cell> Retrace(Node[,] nodes, Cell start, Cell end)
    {
        List<Cell> path = new List<Cell>();
        Cell curr = end;

        // Work backwards from end to start
        while (!Cell.Equals(curr, start))
        {
            path.Add(curr);
            Cell prev = nodes[curr.row, curr.col].prev;
            if (Cell.Equals(prev, Cell.Invalid()))
            {
                // Means there's no valid path
                break;
            }
            curr = prev;
        }

        // Add the start at the end
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Returns the four cardinal neighbors of a cell if they're in bounds.
    /// </summary>
    public static List<Cell> Adjacent(Cell cell, int rows, int cols)
    {
        List<Cell> cells = new List<Cell>();

        Cell left = new Cell { row = cell.row, col = cell.col - 1 };
        Cell right = new Cell { row = cell.row, col = cell.col + 1 };
        Cell up = new Cell { row = cell.row - 1, col = cell.col };
        Cell down = new Cell { row = cell.row + 1, col = cell.col };

        if (left.col >= 0) cells.Add(left);
        if (right.col < cols) cells.Add(right);
        if (up.row >= 0) cells.Add(up);
        if (down.row < rows) cells.Add(down);

        return cells;
    }
}
