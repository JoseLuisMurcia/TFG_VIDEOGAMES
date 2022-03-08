using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridV2 : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    [SerializeField] float nodeRadius;
    public List<Node> grid;
    [SerializeField] TrafficLight[] trafficLights;
    [SerializeField] List<Road> roads = new List<Road>();
    private int numberOfNodes;
    float nodeDiameter;

    private void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        CreateGridBasedOnRoads();
    }


    void CreateGridBasedOnRoads()
    {
        grid = new List<Node>();
        foreach (Road road in roads)
        {
            // If varios carriles, iterar sobre las pos de los carriles e ir creando nodos de principio a final e ir conectándolos.
            // Debo tener en cuenta el tamaño de la carretera para eso, crear más o menos nodos.
            Vector3 worldPoint = road.transform.position;
            Node node = new Node(true, worldPoint, 0, 0);
            grid.Add(node);
            foreach(Road connection in road.connections)
            {
                Node neighbour = new Node(true, connection.transform.position, 0, 0);
                node.neighbours.Add(neighbour);
            }
        }
    }


    public int MaxSize
    {
        get
        {
            return numberOfNodes;
        }
    }
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        //for (int x = -1; x <= 1; x++)
        //{
        //    for (int y = -1; y <= 1; y++)
        //    {
        //        if (x == 0 && y == 0)
        //            continue;

        //        int checkX = node.gridX + x;
        //        int checkY = node.gridY + y;

        //        if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
        //        {
        //            neighbours.Add(grid[checkX, checkY]);
        //        }
        //    }
        //}

        return neighbours;
    }

    void OnDrawGizmos()
    {
        if (grid != null && displayGridGizmos)
        {
            foreach (Node n in grid)
            {
                //Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = Color.white;
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