using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridV2 : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    public List<Node> grid;
    [SerializeField] TrafficLight[] trafficLights;
    [SerializeField] List<Road> roads = new List<Road>();
    private int numberOfNodes;
    float distancePerNode = 1;

    private void Start()
    {
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

            switch (road.kindOfRoad)
            {
                case (KindOfRoad.Straight):
                    Vector3 leftBottomPos = road.leftBottom.position;
                    int numberOfLanes = road.numberOfLanes;
                    Vector3 startRefPoint = road.laneReferencePoints[0];
                    Vector3 endRefPoint = road.laneReferencePoints[1];
                    // Swap if incorrect
                    if(Vector3.Distance(startRefPoint, leftBottomPos) > Vector3.Distance(endRefPoint, leftBottomPos))
                    {
                        Vector3 copy = new Vector3(startRefPoint.x, startRefPoint.y, startRefPoint.z);
                        startRefPoint = endRefPoint;
                        endRefPoint = copy;
                    }
                    Vector3[] startPoints = new Vector3[numberOfLanes];
                    Vector3[] endPoints = new Vector3[numberOfLanes];

                    if (numberOfLanes == 1) // caso de que hay un carril, fijas la posicion y a generar nodos desde ahí hasta el final.
                    {
                        startPoints[0] = startRefPoint;
                        endPoints[0] = endRefPoint;
                    }
                    else if (numberOfLanes == 2) // caso 2 carriles, a partir del punto central bajo sacas el punto de partida que es el punto medio entre el central bajo y el izquierdo bajo y a generar
                    {
                        // Distancia entre left y center pero invertida
                        startPoints[0] = (startRefPoint + leftBottomPos) * 0.5f;
                        float distanceX = Mathf.Abs(startPoints[0].x - startRefPoint.x);
                        float distanceZ = Mathf.Abs(startPoints[0].z - startRefPoint.z);
                        endPoints[0] = endRefPoint + new Vector3(distanceX, 0, distanceZ);

                        Vector3 difference = leftBottomPos - startRefPoint;
                        Vector3 rightBottomPos = startRefPoint - difference;
                        startPoints[1] = (startRefPoint + rightBottomPos) * 0.5f;
                        distanceX = Mathf.Abs(startPoints[1].x - startRefPoint.x);
                        distanceZ = Mathf.Abs(startPoints[1].z - startRefPoint.z);
                        endPoints[1] = endRefPoint - new Vector3(distanceX, 0, distanceZ);
                    }
                    else // caso 4 carriles
                    {
                        startPoints[0] = startRefPoint - leftBottomPos;
                        endPoints[0] = endRefPoint;

                        startPoints[1] = startRefPoint - leftBottomPos;
                        endPoints[1] = endRefPoint;

                        startPoints[2] = startRefPoint - leftBottomPos;
                        endPoints[2] = endRefPoint;

                        startPoints[3] = startRefPoint - leftBottomPos;
                        endPoints[3] = endRefPoint;
                    }

                    for (int i = 0; i < road.numberOfLanes; i++)
                    {
                        Node entryNode = new Node(true, startPoints[i], 0, 0);
                        Node exitNode = new Node(true, endPoints[i], 0, 0);

                        // Calculate all the possible nodes that could fit in a reasonable distance
                        float distance = Vector3.Distance(entryNode.worldPosition, exitNode.worldPosition);
                        float xDistance = Mathf.Abs(entryNode.worldPosition.x - exitNode.worldPosition.x);
                        float zDistance = Mathf.Abs(entryNode.worldPosition.z - exitNode.worldPosition.z);
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
                                newNodePos = new Vector3(entryNode.worldPosition.x + xDistance * multiplier, entryNode.worldPosition.y, entryNode.worldPosition.z + zDistance * multiplier);
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
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (0.25f));
            }
        }
    }


}