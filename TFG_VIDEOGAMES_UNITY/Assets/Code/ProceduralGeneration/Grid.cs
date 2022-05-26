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
        public Node[,] nodesGrid;

        float nodeDiameter;
        public int gridSizeX, gridSizeY;
        public static Grid instance;
        private void Awake()
        {
            instance = this;
        }
        void Start()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
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
            nodesGrid = new Node[gridSizeX, gridSizeY];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    nodesGrid[x, y] = new Node(worldPoint, x, y);
                }
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            if (nodesGrid != null)
            {
                foreach (Node n in nodesGrid)
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
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
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

        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

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
                        neighbours.Add(nodesGrid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public List<Node> GetNeighboursInLine(Node node)
        {
            List<Node> neighbours = new List<Node>();

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
                        if(x == 0 && y != 0 || x != 0 && y == 0)
                            neighbours.Add(nodesGrid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public void Reset()
        {
            CreateGrid();
        }
    }

    public class Node : IHeapItem<Node>
    {
        public Vector3 worldPosition;
        public float gCost;
        public float hCost;
        public Node parent;
        int heapIndex;
        public bool occupied = false;
        public Usage usage = Usage.empty;
        public int gridX, gridY;
        public Node(Vector3 _worldPos, int _gridX, int _gridY)
        {
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
        }

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
    }

    public enum Usage
    {
        empty,
        road,
        point,
        decoration
    }
}

