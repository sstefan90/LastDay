using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LastDay.Pathfinding
{
    public class SimplePathfinder : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Vector2 gridOrigin = new Vector2(-5, -3);
        [SerializeField] private Vector2 gridSize = new Vector2(10, 6);
        [SerializeField] private float cellSize = 0.5f;

        [Header("Obstacles")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float obstacleCheckRadius = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGrid;
        [SerializeField] private bool showPath;

        private bool[,] walkableGrid;
        private int gridWidth;
        private int gridHeight;
        private List<Vector2> lastPath;

        void Start()
        {
            StartCoroutine(DelayedBuildGrid());
        }

        private IEnumerator DelayedBuildGrid()
        {
            yield return null;
            BuildGrid();
        }

        [ContextMenu("Rebuild Grid Now")]
        public void BuildGrid()
        {
            gridWidth = Mathf.CeilToInt(gridSize.x / cellSize);
            gridHeight = Mathf.CeilToInt(gridSize.y / cellSize);
            walkableGrid = new bool[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 worldPos = GridToWorld(x, y);
                    Collider2D obstacle = Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer);
                    walkableGrid[x, y] = (obstacle == null);
                }
            }

            Debug.Log($"[Pathfinder] Grid built: {gridWidth}x{gridHeight} cells");
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            if (walkableGrid == null) BuildGrid();

            Vector2Int startGrid = WorldToGrid(start);
            Vector2Int endGrid = WorldToGrid(end);

            if (!IsValidGridPos(startGrid) || !IsValidGridPos(endGrid))
            {
                Debug.LogWarning("[Pathfinder] Start or end position outside grid");
                return null;
            }

            if (!walkableGrid[endGrid.x, endGrid.y])
            {
                endGrid = FindNearestWalkable(endGrid);
                if (endGrid.x < 0)
                {
                    Debug.LogWarning("[Pathfinder] No walkable cell near destination");
                    return null;
                }
            }

            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Vector2Int>();
            var nodeMap = new Dictionary<Vector2Int, PathNode>();

            var startNode = new PathNode(startGrid, null, 0, Heuristic(startGrid, endGrid));
            openSet.Add(startNode);
            nodeMap[startGrid] = startNode;

            while (openSet.Count > 0)
            {
                PathNode current = GetLowestFCost(openSet);

                if (current.Position == endGrid)
                {
                    lastPath = ReconstructPath(current);
                    return lastPath;
                }

                openSet.Remove(current);
                closedSet.Add(current.Position);

                foreach (var neighborPos in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos)) continue;
                    if (!IsValidGridPos(neighborPos) || !walkableGrid[neighborPos.x, neighborPos.y]) continue;

                    float tentativeG = current.G + cellSize;

                    if (!nodeMap.TryGetValue(neighborPos, out PathNode neighborNode))
                    {
                        neighborNode = new PathNode(neighborPos, current, tentativeG, Heuristic(neighborPos, endGrid));
                        nodeMap[neighborPos] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                    else if (tentativeG < neighborNode.G)
                    {
                        neighborNode.Parent = current;
                        neighborNode.G = tentativeG;
                        neighborNode.F = tentativeG + neighborNode.H;
                    }
                }
            }

            Debug.LogWarning("[Pathfinder] No path found");
            return null;
        }

        #region A* Internals

        private class PathNode
        {
            public Vector2Int Position;
            public PathNode Parent;
            public float G;
            public float H;
            public float F;

            public PathNode(Vector2Int pos, PathNode parent, float g, float h)
            {
                Position = pos;
                Parent = parent;
                G = g;
                H = h;
                F = g + h;
            }
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private PathNode GetLowestFCost(List<PathNode> nodes)
        {
            PathNode lowest = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].F < lowest.F)
                    lowest = nodes[i];
            }
            return lowest;
        }

        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            return new List<Vector2Int>
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };
        }

        private List<Vector2> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector2>();
            var current = endNode;

            while (current != null)
            {
                path.Add(GridToWorld(current.Position.x, current.Position.y));
                current = current.Parent;
            }

            path.Reverse();
            return SmoothPath(path);
        }

        private List<Vector2> SmoothPath(List<Vector2> path)
        {
            if (path.Count <= 2) return path;

            var smoothed = new List<Vector2> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 dir1 = (path[i] - path[i - 1]).normalized;
                Vector2 dir2 = (path[i + 1] - path[i]).normalized;

                if (Vector2.Dot(dir1, dir2) < 0.99f)
                    smoothed.Add(path[i]);
            }

            smoothed.Add(path[path.Count - 1]);
            return smoothed;
        }

        private Vector2Int FindNearestWalkable(Vector2Int pos)
        {
            int maxRadius = Mathf.Max(gridWidth, gridHeight);

            for (int radius = 1; radius < maxRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        Vector2Int check = new Vector2Int(pos.x + x, pos.y + y);
                        if (IsValidGridPos(check) && walkableGrid[check.x, check.y])
                            return check;
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }

        #endregion

        #region Coordinate Conversion

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
            return new Vector2Int(x, y);
        }

        public Vector2 GridToWorld(int x, int y)
        {
            return new Vector2(
                gridOrigin.x + (x + 0.5f) * cellSize,
                gridOrigin.y + (y + 0.5f) * cellSize
            );
        }

        public bool IsValidGridPos(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }

        #endregion

        #region Debug Visualization

        void OnDrawGizmos()
        {
            if (!showDebugGrid) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                (Vector3)(gridOrigin + gridSize * 0.5f),
                (Vector3)gridSize
            );

            if (walkableGrid == null) return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 worldPos = GridToWorld(x, y);
                    Gizmos.color = walkableGrid[x, y]
                        ? new Color(0, 1, 0, 0.2f)
                        : new Color(1, 0, 0, 0.2f);
                    Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.8f);
                }
            }

            if (showPath && lastPath != null && lastPath.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < lastPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(lastPath[i], lastPath[i + 1]);
                }
            }
        }

        #endregion
    }
}
