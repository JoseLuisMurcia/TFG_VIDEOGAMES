using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridV2 : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    public List<Node> grid;
    public List<Vector3> debugNodes = new List<Vector3>();
    [SerializeField] TrafficLight[] trafficLights;
    [SerializeField] LayerMask roadMask;
    [SerializeField] List<Road> roads = new List<Road>();
    float distancePerNode = 2f;

    public int MaxSize
    {
        get
        {
            return grid.Count;
        }
    }
    private void Start()
    {
        CreateGridBasedOnRoads();
    }

    public Node FindStartNode(Vector3 worldPoint, Vector3 carForward)
    {
        Node closestNode = new Node(true, Vector3.zero);
        float bestDistance = float.PositiveInfinity;
        RaycastHit hit;
        Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            GameObject _gameObject = hit.collider.gameObject;

            Road road = _gameObject.GetComponent<Road>();
            if (road)
            {
                foreach (Lane lane in road.lanes)
                {
                    foreach (Node node in lane.nodes)
                    {
                        float currentDistance = Vector3.Distance(node.worldPosition, worldPoint);
                        if (currentDistance <= bestDistance)
                        {
                            Vector3 dirToMovePosition = (node.worldPosition - worldPoint).normalized;
                            float dot = Vector3.Dot(carForward, dirToMovePosition);
                            if (dot > 0)
                            {
                                closestNode = node;
                                bestDistance = currentDistance;
                            }
                        }
                    }
                }
            }
        }
        return closestNode;
    }

    public Node FindEndNode(Vector3 worldPoint)
    {
        Node closestNode = new Node(true, Vector3.zero);
        float bestDistance = float.PositiveInfinity;
        RaycastHit hit;
        Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            GameObject _gameObject = hit.collider.gameObject;
            Road road = _gameObject.GetComponent<Road>();
            if (road)
            {
                foreach (Lane lane in road.lanes)
                {
                    foreach (Node node in lane.nodes)
                    {
                        float currentDistance = Vector3.Distance(node.worldPosition, worldPoint);
                        if (currentDistance <= bestDistance)
                        {
                            closestNode = node;
                            bestDistance = currentDistance;
                        }
                    }
                }
            }
        }
        return closestNode;
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
                    CreateNodesForStraightRoad(road);
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
        foreach (Road road in roads)
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




    void OnDrawGizmos()
    {
        if (grid != null && displayGridGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = Color.white;
                Gizmos.color = (n.hasTrafficLightClose) ? Color.green : Gizmos.color;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (0.25f));

                foreach (Node neighbour in n.neighbours)
                {
                    Gizmos.color = Color.blue;
                    Vector3 yOffset = new Vector3(0, .1f, 0);
                    Gizmos.DrawLine(n.worldPosition + yOffset, neighbour.worldPosition + yOffset);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(n.worldPosition * .2f + neighbour.worldPosition * .8f + yOffset, .1f);
                }
            }

            //foreach(Vector3 debugPos in debugNodes)
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawCube(debugPos, Vector3.one * (0.25f));
            //}
        }
    }


    private void CreateNodesForStraightRoad(Road road)
    {
        Vector3 leftBottom = road.leftBottom.position;
        int numberOfLanes = road.numberOfLanes;
        Vector3 startRefPoint = road.laneReferencePoints[0];
        Vector3 endRefPoint = road.laneReferencePoints[1];
        // Swap if incorrect
        if (Vector3.Distance(startRefPoint, leftBottom) > Vector3.Distance(endRefPoint, leftBottom))
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
            startPoints[0] = (startRefPoint + leftBottom) * 0.5f;
            endPoints[0] = GetVectorWithSameDistanceAsSources(startPoints[0], startRefPoint, endRefPoint);

            Vector3 rightBottom = GetOppositeVector(leftBottom, startRefPoint);
            debugNodes.Add(rightBottom);
            debugNodes.Add(leftBottom);
            startPoints[1] = (startRefPoint + rightBottom) * 0.5f;
            endPoints[1] = GetOppositeVector(endPoints[0], endRefPoint);
        }
        else // caso 4 carriles
        {
            Vector3 leftHalf = (startRefPoint + leftBottom) * 0.5f;
            Vector3 leftHalfEnd = GetVectorWithSameDistanceAsSources(leftHalf, startRefPoint, endRefPoint);
            startPoints[0] = (leftHalf + leftBottom) * 0.5f;
            endPoints[0] = GetVectorWithSameDistanceAsSources(startPoints[0], leftHalf, leftHalfEnd);
            debugNodes.Add(leftBottom);
            debugNodes.Add(leftHalf);
            startPoints[1] = (startRefPoint + leftHalf) * 0.5f;
            endPoints[1] = GetVectorWithSameDistanceAsSources(leftHalf, startPoints[1], leftHalfEnd);
            endPoints[1] = GetOppositeVector(endPoints[0], leftHalfEnd);

            Vector3 rightBottom = GetOppositeVector(leftBottom, startRefPoint);
            Vector3 rightHalf = (startRefPoint + rightBottom) * 0.5f;
            Vector3 rightHalfEnd = GetVectorWithSameDistanceAsSources(rightHalf, startRefPoint, endRefPoint);
            debugNodes.Add(rightBottom);
            debugNodes.Add(rightHalf);

            startPoints[2] = (startRefPoint + rightHalf) * 0.5f;
            endPoints[2] = (endRefPoint + rightHalfEnd) * 0.5f;

            startPoints[3] = (rightHalf + rightBottom) * 0.5f;
            endPoints[3] = GetOppositeVector(endPoints[2], rightHalfEnd);
        }

        for (int i = 0; i < numberOfLanes; i++)
        {
            Node entryNode;
            Node exitNode;
            if (road.numDirection == NumDirection.TwoDirectional && i < numberOfLanes / 2 && numberOfLanes > 1)
            {
                entryNode = new Node(true, endPoints[i]);
                exitNode = new Node(true, startPoints[i]);
            }
            else
            {
                entryNode = new Node(true, startPoints[i]);
                exitNode = new Node(true, endPoints[i]);
            }


            // Calculate all the possible nodes that could fit in a reasonable distance
            float distance = Vector3.Distance(entryNode.worldPosition, exitNode.worldPosition);
            float xDistance = GetDistanceToReach(entryNode.worldPosition.x, exitNode.worldPosition.x);
            float zDistance = GetDistanceToReach(entryNode.worldPosition.z, exitNode.worldPosition.z);
            int totalNodesInRoad = Mathf.FloorToInt(distance / distancePerNode);
            int nodesToAdd = totalNodesInRoad - 2;
            grid.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            if (nodesToAdd > 0)
            {
                Node previousNode = entryNode;
                for (int j = 1; j <= nodesToAdd; j++)
                {
                    float multiplier = j / ((float)nodesToAdd + 1f);
                    Vector3 newNodePos;
                    newNodePos = new Vector3(entryNode.worldPosition.x + xDistance * multiplier, entryNode.worldPosition.y, entryNode.worldPosition.z + zDistance * multiplier);
                    Node newNode = new Node(true, newNodePos);
                    previousNode.neighbours.Add(newNode);
                    grid.Add(newNode);
                    road.lanes[i].nodes.Add(newNode);
                    previousNode = newNode;
                }
                previousNode.neighbours.Add(exitNode);
            }
            else
            {
                entryNode.neighbours.Add(exitNode);
            }
            road.lanes[i].nodes.Add(exitNode);
            grid.Add(exitNode);
        }

        int nodesPerLane = road.lanes[0].nodes.Count;
        // Iterar sobre los carriles para conectar los nodos entre carriles. Check la bidireccionalidad
        if (numberOfLanes == 2 && NumDirection.OneDirectional == road.numDirection)
        {
            List<Node> lane1Nodes = road.lanes[0].nodes;
            List<Node> lane2Nodes = road.lanes[1].nodes;
            for (int i = 0; i < nodesPerLane - 1; i++)
            {
                lane1Nodes[i].neighbours.Add(lane2Nodes[i + 1]);
                lane2Nodes[i].neighbours.Add(lane1Nodes[i + 1]);
            }
        }
        else if (numberOfLanes == 4) // 4 lanes
        {
            List<Node> lane1Nodes = road.lanes[0].nodes;
            List<Node> lane2Nodes = road.lanes[1].nodes;
            List<Node> lane3Nodes = road.lanes[2].nodes;
            List<Node> lane4Nodes = road.lanes[3].nodes;

            if (road.numDirection == NumDirection.OneDirectional)
            {
                for (int i = 0; i < nodesPerLane - 1; i++)
                {
                    lane1Nodes[i].neighbours.Add(lane2Nodes[i + 1]);
                    lane2Nodes[i].neighbours.Add(lane1Nodes[i + 1]);
                    lane2Nodes[i].neighbours.Add(lane3Nodes[i + 1]);
                    lane3Nodes[i].neighbours.Add(lane2Nodes[i + 1]);
                    lane3Nodes[i].neighbours.Add(lane4Nodes[i + 1]);
                    lane4Nodes[i].neighbours.Add(lane3Nodes[i + 1]);
                }
            }
            else
            {
                for (int i = 0; i < nodesPerLane - 1; i++)
                {
                    lane1Nodes[i].neighbours.Add(lane2Nodes[i + 1]);
                    lane2Nodes[i].neighbours.Add(lane1Nodes[i + 1]);
                }
                for (int i = 0; i < nodesPerLane - 1; i++)
                {
                    lane3Nodes[i].neighbours.Add(lane4Nodes[i + 1]);
                    lane4Nodes[i].neighbours.Add(lane3Nodes[i + 1]);
                }
            }
        }


    }
    private Vector3 GetOppositeVector(Vector3 origin, Vector3 anchor)
    {
        float distanceX = GetDistanceToReach(origin.x, anchor.x);
        float distanceY = GetDistanceToReach(origin.y, anchor.y);
        float distanceZ = GetDistanceToReach(origin.z, anchor.z);
        Vector3 oppositeVector = anchor + new Vector3(distanceX, distanceY, distanceZ);
        return oppositeVector;
    }

    private Vector3 GetVectorWithSameDistanceAsSources(Vector3 src1, Vector3 src2, Vector3 dst)
    {
        float distanceX = src1.x - src2.x;
        float distanceY = src1.y - src2.y;
        float distanceZ = src1.z - src2.z;
        Vector3 sameDistanceVector = dst + new Vector3(distanceX, distanceY, distanceZ);
        return sameDistanceVector;
    }

    private float GetDistanceToReach(float src, float dst)
    {
        float distanceToApply = 0;
        float substractionResult = src - dst;
        if (substractionResult == 0)
        {
            distanceToApply = 0;
        }
        else if (src < dst)
        {
            distanceToApply = -substractionResult;
        }
        else if (src > dst)
        {
            distanceToApply = -substractionResult;
        }
        return distanceToApply;
    }
}