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
    float distancePerNode = 2;
    float nodeDiameter;

    private void Start()
    {
        nodeDiameter = nodeRadius * 2;
        CreateGridBasedOnRoads();
    }


    void CreateGridBasedOnRoads()
    {
        grid = new List<Node>();
        // Create all the nodes in a road
        foreach (Road road in roads)
        {
            // If varios carriles, iterar sobre las pos de los carriles e ir creando nodos de principio a final e ir conectándolos.
            // Debo tener en cuenta el tamaño de la carretera para eso, crear más o menos nodos.
            Vector3 worldPoint = road.transform.position;

            switch (road.kindOfRoad)
            {
                case (KindOfRoad.Straight):
                    for (int i=0; i < road.lanes.Count; i++)
                    {
                        GameObject entry = road.laneGameObjects[i].transform.Find("EntryNode").gameObject;
                        GameObject exit = road.laneGameObjects[i].transform.Find("ExitNode").gameObject;
                        Node entryNode = new Node(true, entry.transform.position, 0, 0);
                        Node exitNode = new Node(true, exit.transform.position, 0, 0);

                        // Calculate all the possible nodes that could fit in a reasonable distance
                        float distance = Vector3.Distance(entryNode.worldPosition, exitNode.worldPosition);
                        float xDistance = entryNode.worldPosition.x - exitNode.worldPosition.x;
                        int totalNodesInRoad = Mathf.FloorToInt(distance / distancePerNode);
                        int nodesToAdd = totalNodesInRoad - 2;
                        grid.Add(entryNode);
                        if (nodesToAdd > 0)
                        {
                            Node previousNode = entryNode;
                            for (int j = 1; j <= nodesToAdd; j++)
                            {
                                float multiplier = j / ((float)nodesToAdd + 1f);
                                Vector3 newNodePos;
                                if (xDistance == 0)
                                {
                                    newNodePos = new Vector3(entryNode.worldPosition.x, entryNode.worldPosition.y, entryNode.worldPosition.z + distance * multiplier);
                                }
                                else
                                {
                                    newNodePos = new Vector3(entryNode.worldPosition.x + distance * multiplier, entryNode.worldPosition.y, entryNode.worldPosition.z);
                                }
                                Node newNode = new Node(true, newNodePos, 0, 0);
                                previousNode.neighbours.Add(newNode);
                                grid.Add(newNode);
                                previousNode = newNode;
                            }
                        }
                        else
                        {
                            entryNode.neighbours.Add(exitNode);
                            exitNode.neighbours.Add(entryNode);
                        }
                        grid.Add(exitNode);


                        // Si la carretera tiene varios carriles y son del mismo sentido tengo que establecer conexiones entre nodos vecinos.
                        // Iterar sobre los nodos de un carril y añadir como vecino al nodo i del carril 1 al nodo i+1 del carril 2.
                    }
                    break;
                case (KindOfRoad.BendSquare):
                    break;
                case KindOfRoad.End:
                    for (int i = 0; i < road.lanes.Count; i++)
                    {
                        GameObject entry = road.laneGameObjects[i].transform.Find("EntryNode").gameObject;
                        GameObject exit = road.laneGameObjects[i].transform.Find("ExitNode").gameObject;
                        Node entryNode = new Node(true, entry.transform.position, 0, 0);
                        Node exitNode = new Node(true, exit.transform.position, 0, 0);

                    }
                    break;
                case KindOfRoad.Intersection:
                    break;
            }
        }

        // Connect the exit and entry nodes in the roads
        foreach(Road road in roads)
        {
            switch (road.kindOfRoad)
            {
                case (KindOfRoad.Straight):
                    break;
                case (KindOfRoad.BendSquare):
                    break;
                case KindOfRoad.End:
                    break;
                case KindOfRoad.Intersection:
                    break;
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