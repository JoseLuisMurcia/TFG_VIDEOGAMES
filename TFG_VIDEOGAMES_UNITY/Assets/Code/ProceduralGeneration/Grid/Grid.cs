using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Grid : MonoBehaviour
    {
        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize;
        public float nodeRadius;
        public GridNode[,] nodesGrid;

        public float nodeDiameter;
        public int gridSizeX, gridSizeY;
        public static Grid Instance;

        [SerializeField] DebugMode debugMode;
        [HideInInspector] public VoronoiGeneration voronoiGenerator;
        private void Awake()
        {
            Instance = this;
            voronoiGenerator = GetComponent<VoronoiGeneration>();
        }
        void Start()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            voronoiGenerator.SetupVoronoi(gridSizeX);
            CreateGrid();
        }
        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }
        void CreateGrid()
        {
            nodesGrid = new GridNode[gridSizeX, gridSizeY];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
            voronoiGenerator.worldBottomLeft = worldBottomLeft;
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    GridNode currentNode = new GridNode(worldPoint, x, y);
                    nodesGrid[x, y] = currentNode;
                    voronoiGenerator.SetRegions(x, y, currentNode);
                }
            }
            voronoiGenerator.Cleanup();
            voronoiGenerator.SetNeighbourRegions();
            voronoiGenerator.SetCentres();
            voronoiGenerator.LloydRelaxation();
        }

        void OnDrawGizmos()
        {
            if (debugMode == DebugMode.Disabled)
                return;
            if (nodesGrid != null)
            {
                Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

                if (debugMode == DebugMode.Allocation)
                {
                    foreach (GridNode n in nodesGrid)
                    {
                        switch (n.usage)
                        {
                            case Usage.empty:
                                Gizmos.color = Color.white;
                                break;
                            case Usage.road:
                                Gizmos.color = Color.magenta;
                                break;
                            case Usage.point:
                                Gizmos.color = Color.red;
                                break;
                            case Usage.decoration:
                                Gizmos.color = Color.gray;
                                break;
                            default:
                                break;
                        }
                        //Gizmos.color = n.isRoundabout ? Color.green : Gizmos.color;
                        Gizmos.color = n.occupied ? Color.green : Gizmos.color;
                        Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
                    }
                }
            }
        }

        public bool OutOfGrid(int posX, int posY)
        {
            if (posX >= gridSizeX || posY >= gridSizeY)
                return true;
            if (posX < 0 || posY < 0)
                return true;

            return false;
        }

        public List<GridNode> GetNeighboursForVoronoi(GridNode node)
        {
            List<Vector2Int> offsets = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1) };
            List<GridNode> neighbours = new List<GridNode>();

            foreach(Vector2Int offset in offsets)
            {
                int checkX = node.gridX + offset.x;
                int checkY = node.gridY + offset.y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    if (nodesGrid[checkX, checkY] != null)
                    {
                        neighbours.Add(nodesGrid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public List<GridNode> GetNeighbours(GridNode node)
        {
            List<GridNode> neighbours = new List<GridNode>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        if (nodesGrid[checkX, checkY] != null)
                        {
                            neighbours.Add(nodesGrid[checkX, checkY]);
                        }
                    }
                }
            }

            return neighbours;
        }

        public List<GridNode> GetNeighboursInLine(GridNode node)
        {
            List<GridNode> neighbours = new List<GridNode>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        if (x == 0 && y != 0 || x != 0 && y == 0)
                            neighbours.Add(nodesGrid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public void Reset()
        {
            voronoiGenerator.SetupVoronoi(gridSizeX);
            CreateGrid();
        }
    }




    public enum Usage
    {
        empty,
        road,
        point,
        decoration
    }

    enum DebugMode
    {
        Allocation,
        Disabled
    }
}

