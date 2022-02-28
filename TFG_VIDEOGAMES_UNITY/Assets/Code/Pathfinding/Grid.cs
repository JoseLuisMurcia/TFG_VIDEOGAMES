using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] LayerMask unwalkableMask;
    [SerializeField] bool displayGridGizmos;
    [SerializeField] Vector2 gridWorldSize;
    [SerializeField] float nodeRadius;
    public TerrainType[] walkableRegions;
    LayerMask walkableMask;
    public int obstacleProximityPenalty = 10;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    Node[,] grid;
    [SerializeField] TrafficLight[] trafficLights;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                int movementPenalty = 0;

                // Raycast code to find the layer
                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;

                Node node = new Node(walkable, worldPoint, x, y);
                if (Physics.Raycast(ray, out hit, 100, walkableMask))
                {
                    GameObject _gameObject = hit.collider.gameObject;
                    walkableRegionsDictionary.TryGetValue(_gameObject.layer, out movementPenalty);

                    Road road = _gameObject.GetComponent<Road>();
                    // We have hit a road, set its type to the node
                    if (road != null)
                    {
                        node.isRoad = true;
                        node.typeOfRoad = road.typeOfRoad;
                        node.road = road;
                    }
                }
                if (!walkable)
                {
                    node.isRoad = false;
                    movementPenalty += obstacleProximityPenalty;
                }
                node.movementPenalty = movementPenalty;
                grid[x, y] = node;
            }
        }
        BlurPenaltyMap(1);
    }


    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }

    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            for (int x = 1; x < gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > penaltyMax)
                {
                    penaltyMax = blurredPenalty;
                }
                if (blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }
            }
        }

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
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public List<Node> path;
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        if (grid != null && displayGridGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;
                Gizmos.color = (n.hasTrafficLightClose) ? Color.green : Gizmos.color;


                //switch (n.typeOfRoad)
                //{
                //    case TypeOfRoad.Right:
                //        Gizmos.color = Color.blue;
                //        break;
                //    case TypeOfRoad.DownToRight:
                //        Gizmos.color = Color.magenta;
                //        break;
                //    case TypeOfRoad.UpToRight:
                //        Gizmos.color = Color.black;
                //        break;
                //    case TypeOfRoad.UpToLeft:
                //        Gizmos.color = Color.cyan;
                //        break;
                //    case TypeOfRoad.DownToLeft:
                //        Gizmos.color = Color.yellow;
                //        break;
                //    case TypeOfRoad.Left:
                //        Gizmos.color = Color.green;
                //        break;
                //    case TypeOfRoad.Down:
                //        Gizmos.color = Color.gray;
                //        break;
                //    case TypeOfRoad.Up:
                //        Gizmos.color = Color.white;
                //        break;
                //    case TypeOfRoad.None:
                //        Gizmos.color = Color.red;
                //        break;
                //}

                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .15f));
            }
        }
    }


}

[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    public int terrainPenalty;
}
