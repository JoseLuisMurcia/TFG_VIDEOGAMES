using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    public List<Node> grid;
    public List<Vector3> debugNodes = new List<Vector3>();
    [SerializeField] TrafficLight[] trafficLights;
    [SerializeField] LayerMask roadMask;
    [SerializeField] List<Road> roads = new List<Road>();
    float distancePerNode = 2f;

    float turnDst = 0.7f;
    public int MaxSize
    {
        get
        {
            return grid.Count;
        }
    }
    private void Start()
    {
        CreateGrid();
    }


    void CreateGrid()
    {
        grid = new List<Node>();
        // Create all the nodes in a road
        foreach (Road road in roads)
        {
            // If varios carriles, iterar sobre las pos de los carriles e ir creando nodos de principio a final e ir conectándolos.
            // Debo tener en cuenta el tamaño de la carretera para eso, crear más o menos nodos.
            switch (road.typeOfRoad)
            {
                case TypeOfRoad.Straight:
                    CreateNodesForStraightRoad(road);
                    break;
                case TypeOfRoad.Curve:
                    CreateNodesForCurve(road);
                    break;
                case TypeOfRoad.BendSquare:
                    break;
                case TypeOfRoad.End:
                    break;
                case TypeOfRoad.Intersection:
                    break;
            }
        }

        // Connect the exit and entry nodes in the roads
        foreach (Road road in roads)
        {
            switch (road.typeOfRoad)
            {
                case TypeOfRoad.Straight:
                    break;
                case TypeOfRoad.BendSquare:
                    break;
                case TypeOfRoad.End:
                    break;
                case TypeOfRoad.Intersection:
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

            foreach (Vector3 debugPos in debugNodes)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(debugPos, Vector3.one * (0.25f));
            }

            foreach (Road road in roads)
            {
                Gizmos.color = Color.white;
                if (road.typeOfRoad == TypeOfRoad.Curve)
                {
                    foreach (Line l in road.curveRoadLines)
                    {
                        l.DrawWithGizmos(1);
                    }
                }
            }
        }
    }

    private void CreateNodesForCurve(Road road)
    {
        int numberOfLanes = road.numberOfLanes;
        List<Vector3> referencePoints = road.laneReferencePoints;
        int numNodes = referencePoints.Count;
        Vector3 startRefPoint = road.laneReferencePoints[0];
        Vector3 endRefPoint = road.laneReferencePoints[road.laneReferencePoints.Count - 1];


        // Lines creation
        List<Line> lines = new List<Line>(numNodes);
        Vector2 previousPoint = V3ToV2(startRefPoint);
        for (int j = 0; j < numNodes; j++)
        {
            Vector2 currentPoint = V3ToV2(referencePoints[j]);
            Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
            if (j == 0)
            {
                dirToCurrentPoint = (V3ToV2(referencePoints[j + 1]) - currentPoint).normalized;
            }
            Vector2 perpendicularPointToLine = previousPoint - dirToCurrentPoint * turnDst;
            lines.Add(new Line(currentPoint, perpendicularPointToLine));
            previousPoint = currentPoint;
        }
        road.curveRoadLines = lines;

        Vector3 leftBottom = road.leftBottom.position;
        float distanceBetweenCornerAndStart = Vector3.Distance(leftBottom, startRefPoint);

        Vector3[] laneOffsets = new Vector3[0];

        // With the lines and the offsets created, find points in the line matching the lane offset calculated before for each lane.
        for (int i = 0; i < numberOfLanes; i++)
        {
            Node entryNode = new Node(startRefPoint, road);
            Node exitNode = new Node(endRefPoint, road);

            debugNodes.Add(startRefPoint);
            debugNodes.Add(endRefPoint);
            grid.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            Node previousNode = entryNode;
            for (int j = 1; j < numNodes - 1; j++)
            {
                // Calculate the offset points
                // Aplicar el 0.5 y -0.5 de esta distancia si hay dos carriles al punto a encontrar
                // Aplicar el 0.75, 0.25, -0.25, y 0.75 de esta distancia si hay 4 carriles
                if (numberOfLanes == 1)
                {
                    laneOffsets = new Vector3[1];
                    laneOffsets[0] = Vector3.zero;
                }
                else if (numberOfLanes == 2)
                {
                    float distance = 0f;
                    switch (j)
                    {
                        case 1:
                            distance = -0.5f * distanceBetweenCornerAndStart;
                            break;
                        case 2:
                            distance = 0.5f * distanceBetweenCornerAndStart;
                            break;
                    }

                    laneOffsets = new Vector3[2];
                    Line l = lines[j];
                    Vector3 lineDir = new Vector3(1, 0, l.gradient).normalized;
                    Vector3 lineCentre = new Vector3(l.pointOnLine_1.x, 0.2f, l.pointOnLine_1.y);
                    Vector3 from = lineCentre - lineDir * distance / 2f;
                    Vector3 to = lineCentre + lineDir * distance / 2f;
                    laneOffsets[j] = from;
                }
                else if (numberOfLanes == 4)
                {
                    float distance = 0f;
                    switch (j)
                    {
                        case 1:
                            distance = -0.75f * distanceBetweenCornerAndStart;
                            break;
                        case 2:
                            distance = -0.25f * distanceBetweenCornerAndStart;
                            break;
                        case 3:
                            distance = 0.25f * distanceBetweenCornerAndStart;
                            break;
                        case 4:
                            distance = 0.75f * distanceBetweenCornerAndStart;
                            break;
                    }
                    laneOffsets = new Vector3[4];
                }

                Vector3 pointInLine = referencePoints[j] + laneOffsets[i];
                Node newNode = new Node(pointInLine, road);
                previousNode.neighbours.Add(newNode);
                previousNode = newNode;
                grid.Add(newNode);
                road.lanes[i].nodes.Add(newNode);
            }
            previousNode.neighbours.Add(exitNode);
            road.lanes[i].nodes.Add(exitNode);
            grid.Add(exitNode);
        }

    }
    private Vector2 V3ToV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
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
                entryNode = new Node(endPoints[i], road);
                exitNode = new Node(startPoints[i], road);
            }
            else
            {
                entryNode = new Node(startPoints[i], road);
                exitNode = new Node(endPoints[i], road);
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
                    Node newNode = new Node(newNodePos, road);
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

    public Node FindStartNode(Vector3 worldPoint, Vector3 carForward)
    {
        Node closestNode = new Node(Vector3.zero, null);
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
        Node closestNode = new Node(Vector3.zero, null);
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
}