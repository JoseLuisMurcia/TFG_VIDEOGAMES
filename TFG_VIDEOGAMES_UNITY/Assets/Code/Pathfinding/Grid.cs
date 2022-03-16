using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    public List<Node> grid;
    [HideInInspector] public List<Vector3> debugNodes = new List<Vector3>();
    [HideInInspector] public List<Vector3> unionNodes = new List<Vector3>();
    [SerializeField] LayerMask roadMask;
    [SerializeField] GameObject allRoads;
    private List<Road> roads = new List<Road>();
    private List<Line> debugLines = new List<Line>();
    float distancePerNode = 2.5f;
    public static Grid instance;
    public int MaxSize
    {
        get
        {
            return grid.Count;
        }
    }
    private void Start()
    {
        instance = this;
        SetRoadsOnStart();
        CreateGrid();
    }

    private void SetRoadsOnStart()
    {
        foreach (Transform child in allRoads.transform)
        {
            Road road = child.GetComponent<Road>();
            roads.Add(road);
        }
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
                case TypeOfRoad.Split:
                    CreateNodesForSplit((Split)road);
                    break;
                case TypeOfRoad.Deviation:
                    CreateNodesForDeviation(road);
                    break;
                case TypeOfRoad.Intersection:
                    // No crea nodos, se encarga de conectar los nodos de sus conexiones
                    break;
                case TypeOfRoad.Roundabout:
                    CreateNodesForRoundabout((Roundabout)road);
                    break;
            }
        }

        // Connect the exit and entry nodes in the roads
        foreach (Road road in roads)
        {
            foreach (Node exitNode in road.exitNodes)
            {
                float bestDistance = Mathf.Infinity;
                Node bestEntryNode = null;
                foreach (Road connection in road.connections)
                {
                    foreach (Node neighbourEntryNode in connection.entryNodes)
                    {
                        float distance = Vector3.Distance(exitNode.worldPosition, neighbourEntryNode.worldPosition);
                        if (distance < bestDistance)
                        {
                            bestEntryNode = neighbourEntryNode;
                            bestDistance = distance;
                        }
                    }

                }
                if (bestEntryNode != null && bestDistance < 5f)
                {
                    // Perform connection
                    Vector3 unionNodePos = (exitNode.worldPosition + bestEntryNode.worldPosition) * 0.5f;
                    Node unionNode = new Node(unionNodePos, null);
                    exitNode.AddNeighbour(unionNode);
                    unionNode.AddNeighbour(bestEntryNode);
                    unionNodes.Add(unionNodePos);
                    grid.Add(unionNode);
                }
            }

            if (road.typeOfRoad == TypeOfRoad.Intersection)
            {
                ConnectRoadsThroughIntersection(road);
            }

        }
    }

    public Vector3 GetRandomPosInRoads()
    {
        Vector3 randomPos;
        int numRoads = roads.Count;
        int roadIndex = Random.Range(0, numRoads);
        Road selectedRoad = roads[roadIndex];
        randomPos = selectedRoad.transform.position;
        return randomPos;
    }

    private void ConnectRoadsThroughIntersection(Road road)
    {
        // maxDistance should be generated taking into account the bounds size
        float intersectionSize = road.boxColliderSize;
        float maxDistance = intersectionSize * 2f;

        // No solo hay que conectar la interseccion con las carreteras colindantes, sino crear conexiones entre las colindantes a través de la interseccion
        // Coger todos los exit nodes de cada conexion y tratar de conectarlos con el resto de conexiones a través de sus entries
        foreach (Road connecter in road.connections)
        {
            foreach (Node exit in connecter.exitNodes)
            {
                foreach (Road connected in road.connections)
                {
                    if (connecter != connected)
                    {
                        foreach (Node entry in connected.entryNodes)
                        {
                            float distance = Vector3.Distance(exit.worldPosition, entry.worldPosition);
                            if (distance < maxDistance)
                            {
                                Vector3 exitNodeForward = (exit.worldPosition - exit.previousNode.worldPosition).normalized;
                                Vector3 dirToMovePosition = (entry.worldPosition - exit.worldPosition).normalized;
                                //float dot = Vector3.Dot(exitNodeForward, dirToMovePosition);
                                float signedAngle = Vector3.SignedAngle(dirToMovePosition, exitNodeForward, Vector3.up);
                                float absoluteAngle = Mathf.Abs(signedAngle);
                                float minAngle = 5f;
                                // Conectar rectas 
                                if (absoluteAngle <= minAngle)
                                {
                                    // Perform connection
                                    Vector3 unionNodePos = (exit.worldPosition + entry.worldPosition) * 0.5f;
                                    Node unionNode = new Node(unionNodePos, null);
                                    exit.AddNeighbour(unionNode);
                                    unionNode.AddNeighbour(entry);
                                    unionNodes.Add(unionNodePos);
                                    grid.Add(unionNode);
                                }
                                minAngle = 20f;
                                float maxAngle = 60f;

                                // Conectar giros 
                                // Con el angle, sabemos si el nodo al que queremos ir está a la izquierda o a la derecha, con esto podremos crear un
                                // offset para un nodo intermedio, de forma que no pisen fuera de la carretera y sea más creible
                                if (absoluteAngle > minAngle && absoluteAngle < maxAngle)
                                {
                                    Vector2 dirToMove = new Vector2(dirToMovePosition.x, dirToMovePosition.z);
                                    float offsetInfluence = 1.1f;
                                    Vector3 unionNodePos = (exit.worldPosition + entry.worldPosition) * 0.5f;
                                    Vector2 perpendicularDir = Vector2.Perpendicular(dirToMove);
                                    Vector3 perpendicularDirection = new Vector3(perpendicularDir.x, 0, perpendicularDir.y);
                                    if (signedAngle > 0)
                                    {
                                        unionNodePos = unionNodePos - perpendicularDirection * offsetInfluence;
                                    }
                                    else
                                    {
                                        unionNodePos = unionNodePos + perpendicularDirection * offsetInfluence;
                                    }

                                    Node unionNode = new Node(unionNodePos, null);
                                    exit.AddNeighbour(unionNode);
                                    unionNode.AddNeighbour(entry);
                                    unionNodes.Add(unionNodePos);
                                    grid.Add(unionNode);

                                    //List<Pair> pairs = new List<Pair>();
                                    //pairs.Add(new Pair(exit, entry));
                                    //for (int i = 0; i < 2; i++)
                                    //{
                                    //    pairs = IterativeLinkCreation(pairs, signedAngle, dirToMove, road);
                                    //}
                                    //// Now create the smoothed path
                                    //foreach (Pair pair in pairs)
                                    //{
                                    //    Node nodeA = pair.pointA;
                                    //    Node nodeB = pair.pointB;
                                    //    nodeA.AddNeighbour(nodeB);
                                    //    if (nodeB != entry)
                                    //    {
                                    //        unionNodes.Add(nodeB.worldPosition);
                                    //        grid.Add(nodeB);
                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private List<Pair> IterativeLinkCreation(List<Pair> pairs, float signedAngle, Vector2 dirToMove, Road road)
    {
        List<Pair> newPairs = new List<Pair>();
        int numSections = pairs.Count + 1;
        float absoluteSignedAngle = Mathf.Abs(signedAngle);
        Node previousUnionNode = null;

        float offsetInfluence = 1f;

        foreach (Pair pair in pairs)
        {
            Node nodeA = pair.pointA;
            Node nodeB = pair.pointB;

            //offsetInfluence = Mathf.Clamp(offsetInfluence, 15f, 60f);
            Vector3 unionNodePos = (nodeA.worldPosition + nodeB.worldPosition) / numSections;
            Vector2 perpendicularDir = Vector2.Perpendicular(dirToMove);
            Vector3 perpendicularDirection = new Vector3(perpendicularDir.x, 0, perpendicularDir.y);
            if (signedAngle > 0)
            {
                unionNodePos = unionNodePos - perpendicularDirection * offsetInfluence;
            }
            else
            {
                unionNodePos = unionNodePos + perpendicularDirection * offsetInfluence;
            }
            Node unionNode = new Node(unionNodePos, road);
            Pair newPair1 = new Pair(nodeA, unionNode);
            newPairs.Add(newPair1);

            if (previousUnionNode != null)
            {
                Pair newPair = new Pair(previousUnionNode, unionNode);
                newPairs.Add(newPair);
            }
            previousUnionNode = unionNode;
        }
        int numOriginalPairs = pairs.Count;
        int numNewPairs = pairs.Count;
        Node lastNode = pairs[numOriginalPairs - 1].pointB;
        Pair lastPair = new Pair(previousUnionNode, lastNode);
        newPairs.Add(lastPair);
        return newPairs;
    }

    void OnDrawGizmos()
    {
        if (grid != null && displayGridGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = Color.white;
                Gizmos.color = (n.hasTrafficLightClose) ? Color.green : Gizmos.color;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (0.15f));

                foreach (Node neighbour in n.neighbours)
                {
                    Gizmos.color = Color.blue;
                    Vector3 yOffset = new Vector3(0, .1f, 0);
                    Gizmos.DrawLine(n.worldPosition + yOffset, neighbour.worldPosition + yOffset);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(n.worldPosition * .15f + neighbour.worldPosition * .85f + yOffset, .05f);
                }
            }

            foreach (Vector3 debugPos in debugNodes)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(debugPos, Vector3.one * (0.25f));
            }

            foreach (Vector3 unionPos in unionNodes)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(unionPos, Vector3.one * (.1f));
            }

            foreach (Road road in roads)
            {
                if (road.typeOfRoad == TypeOfRoad.Curve || road.typeOfRoad == TypeOfRoad.Roundabout)
                {
                    Gizmos.color = Color.white;
                    foreach (Line l in road.curveRoadLines)
                    {
                        l.DrawWithGizmos(1);
                    }
                }
            }
        }
    }

    #region Node creation

    private void CreateNodesForSplit(Split road)
    {
        List<Vector3> referencePoints = road.laneReferencePoints;
        int numNodes = referencePoints.Count;
        Vector3 startRefPoint1 = road.laneReferencePoints[0];
        Vector3 endPoint1 = road.laneReferencePoints[numNodes - 1];
        // Encontrar punto a una distancia X a partir de una direccion.
        Vector3 laneStraightDirection = road.laneDir;
        Vector2 perpDir = Vector2.Perpendicular(new Vector2(laneStraightDirection.x, laneStraightDirection.z));
        Vector3 perpendicularDirection = new Vector3(perpDir.x, 0, perpDir.y).normalized;

        Ray ray = new Ray(startRefPoint1, laneStraightDirection);
        float distance = Vector3.Cross(ray.direction, endPoint1 - ray.origin).magnitude;
        Vector3 endPoint2 = endPoint1 + perpendicularDirection * (distance * 2f);
        Vector3 startRefPoint2 = startRefPoint1;

        if (road.numDirection == NumDirection.TwoDirectional)
        {
            Vector3 copy = new Vector3(endPoint2.x, endPoint2.y, endPoint2.z);
            endPoint2 = new Vector3(startRefPoint2.x, startRefPoint2.y, startRefPoint2.z);
            startRefPoint2 = copy;
        }
        else if (road.invertPath && road.numDirection == NumDirection.OneDirectional)
        {
            Vector3 copy = new Vector3(endPoint2.x, endPoint2.y, endPoint2.z);
            endPoint2 = new Vector3(startRefPoint2.x, startRefPoint2.y, startRefPoint2.z);
            startRefPoint2 = copy;

            copy = new Vector3(endPoint1.x, endPoint1.y, endPoint1.z);
            endPoint1 = new Vector3(startRefPoint1.x, startRefPoint1.y, startRefPoint1.z);
            startRefPoint1 = copy;

            List<Vector3> newReferencePoints = new List<Vector3>(numNodes);
            for(int i=0; i < numNodes; i++)
            {
                newReferencePoints.Add(referencePoints[numNodes - i - 1]);
            }
            referencePoints = newReferencePoints;
        }

        Node entryNode1 = new Node(startRefPoint1, road);
        Node entryNode2 = new Node(startRefPoint2, road);
        Node exitNode1 = new Node(endPoint1, road);
        Node exitNode2 = new Node(endPoint2, road);
        Node previousNode1 = entryNode1;
        Node previousNode2 = entryNode2;

        road.entryNodes.Add(entryNode1);
        road.entryNodes.Add(entryNode2);
        road.exitNodes.Add(exitNode1);
        road.exitNodes.Add(exitNode2);
        grid.Add(entryNode1);
        grid.Add(entryNode2);
        road.lanes.Add(new Lane());
        road.lanes.Add(new Lane());
        debugNodes.Add(startRefPoint1);  
        debugNodes.Add(startRefPoint2);

        Vector3 newPoint2pos = Vector3.zero;
        for (int j = 1; j < numNodes - 1; j++)
        {
            Node newNode1 = new Node(referencePoints[j], road);
            if(road.numDirection == NumDirection.TwoDirectional)
            {
                distance = Vector3.Cross(ray.direction, referencePoints[numNodes - j - 1] - ray.origin).magnitude;
                newPoint2pos = referencePoints[numNodes - j - 1] + perpendicularDirection * (distance * 2f);

            }
            else
            {
                distance = Vector3.Cross(ray.direction, referencePoints[j] - ray.origin).magnitude;
                newPoint2pos = referencePoints[j] + perpendicularDirection * (distance * 2f);
            }
            Node newNode2 = new Node(newPoint2pos, road);
            previousNode1.AddNeighbour(newNode1);
            previousNode2.AddNeighbour(newNode2);
            previousNode1 = newNode1;
            previousNode2 = newNode2;
            grid.Add(newNode1);
            grid.Add(newNode2);
            road.lanes[0].nodes.Add(newNode1);
            road.lanes[1].nodes.Add(newNode2);
        }
        previousNode1.AddNeighbour(exitNode1);
        previousNode2.AddNeighbour(exitNode2);
        grid.Add(exitNode1);
        grid.Add(exitNode2);

    }

    private void CreateNodesForDeviation(Road road)
    {

    }
    private void CreateNodesForRoundabout(Roundabout roundabout)
    {
        int numberOfLanes = roundabout.numberOfLanes;
        List<Vector3> referencePoints = roundabout.laneReferencePoints;
        int numNodes = referencePoints.Count;

        // Create an empty object in the centre and base the direction of the lines of that
        List<Line> lines = CreateLinesForRoundabout(roundabout);

        float laneWidth = roundabout.laneWidth;

        // Calculate the offset points
        Vector3[][] lanePositions = new Vector3[numberOfLanes][];
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanePositions[i] = new Vector3[numNodes];
            for (int j = 0; j < numNodes; j++)
            {
                Line l = lines[j];
                //INVERTIR LINEDIR 
                Vector3 lineDir = new Vector3(1, 0, l.gradient).normalized;
                Vector3 lineCentre = new Vector3(l.pointOnLine_1.x, 0f, l.pointOnLine_1.y);
                Vector3 roundaboutCentre = roundabout.transform.position;
                float leftDistanceToCorner = Vector3.Distance(roundaboutCentre, lineCentre + lineDir * 2);
                float rightDistanceToCorner = Vector3.Distance(roundaboutCentre, lineCentre - lineDir * 2);
                if (leftDistanceToCorner > rightDistanceToCorner)
                {
                    lineDir = -lineDir;
                }
                if (numberOfLanes == 1)
                {
                    lanePositions[i][j] = referencePoints[j];
                }
                else if (numberOfLanes == 2)
                {
                    float distance = Mathf.Abs(laneWidth * .5f);
                    switch (i)
                    {
                        case 0:
                            lanePositions[i][j] = lineCentre - lineDir * distance;
                            break;
                        case 1:
                            lanePositions[i][j] = lineCentre + lineDir * distance;
                            break;
                    }

                }
            }
        }


        // With the lines and the offsets created, find points in the line matching the lane offset calculated before for each lane.
        for (int i = 0; i < numberOfLanes; i++)
        {
            Vector3 laneStartPoint = lanePositions[i][0];
            Vector3 laneEndPoint = lanePositions[i][numNodes - 1];
            Node entryNode = new Node(laneStartPoint, roundabout);
            Node exitNode = new Node(laneEndPoint, roundabout);
            exitNode.AddNeighbour(entryNode);
            grid.Add(entryNode);
            roundabout.lanes[i].nodes.Add(entryNode);
            Node previousNode = entryNode;

            for (int j = 1; j < numNodes - 1; j++)
            {
                Node newNode = new Node(lanePositions[i][j], roundabout);
                previousNode.AddNeighbour(newNode);
                previousNode = newNode;
                grid.Add(newNode);
                roundabout.lanes[i].nodes.Add(newNode);
            }
            previousNode.AddNeighbour(exitNode);
            roundabout.lanes[i].nodes.Add(exitNode);
            grid.Add(exitNode);
        }

        // Connect nodes between lanes
        ConnectNodesInRoad(roundabout);

        // Connect entry and exit nodes hehe

        List<Transform> entries = roundabout.entries;
        List<Transform> exits = roundabout.exits;

        foreach (Transform entry in entries)
        {
            debugNodes.Add(entry.position);

        }
        foreach (Transform entry in entries)
        {
            foreach (Lane lane in roundabout.lanes)
            {
                float bestDistance = Mathf.Infinity;
                Node bestNode = null;
                foreach (Node node in lane.nodes)
                {
                    float distance = Vector3.Distance(entry.position, node.worldPosition);
                    Vector3 targetDir = node.worldPosition - entry.position;
                    if (distance < bestDistance && Vector3.SignedAngle(targetDir, entry.forward, Vector3.up) > 0)
                    {
                        bestDistance = distance;
                        bestNode = node;
                    }
                }
                // Connect the best node to the entry
                Node entryNode = new Node(entry.position, roundabout);
                roundabout.entryNodes.Add(entryNode);
                entryNode.AddNeighbour(bestNode);
                grid.Add(entryNode);
            }
        }
        foreach (Transform exit in exits)
        {
            foreach (Lane lane in roundabout.lanes)
            {
                float bestDistance = Mathf.Infinity;
                Node bestNode = null;
                foreach (Node node in lane.nodes)
                {
                    float distance = Vector3.Distance(exit.position, node.worldPosition);
                    Vector3 targetDir = node.worldPosition - exit.position;
                    if (distance < bestDistance && Vector3.SignedAngle(targetDir, exit.forward, Vector3.up) < 0)
                    {
                        bestDistance = distance;
                        bestNode = node;
                    }
                }
                // Connect the best node to the exit
                Node exitNode = new Node(exit.position, roundabout);
                roundabout.exitNodes.Add(exitNode);
                bestNode.AddNeighbour(exitNode);
                grid.Add(exitNode);
            }
        }
    }
    private List<Line> CreateLinesForRoundabout(Roundabout roundabout)
    {
        List<Vector3> referencePoints = roundabout.laneReferencePoints;
        int numNodes = referencePoints.Count;
        Vector3 startRefPoint = roundabout.laneReferencePoints[0];
        Vector2 centre = V3ToV2(roundabout.transform.position);

        // Lines creation
        List<Line> lines = new List<Line>(numNodes);
        for (int j = 0; j < numNodes; j++)
        {
            Vector2 currentPoint = V3ToV2(referencePoints[j]);
            Vector2 dirToCurrentPoint = (currentPoint - centre).normalized;
            dirToCurrentPoint = Vector2.Perpendicular(dirToCurrentPoint);
            Vector2 perpendicularPointToLine = currentPoint - dirToCurrentPoint * 1f;

            lines.Add(new Line(currentPoint, perpendicularPointToLine));
            //debugLines.Add(new Line(currentPoint, perpendicularPointToLine));
        }
        roundabout.curveRoadLines = lines;
        return lines;
    }
    private void CreateNodesForCurve(Road road)
    {
        int numberOfLanes = road.numberOfLanes;
        List<Vector3> referencePoints = road.laneReferencePoints;
        int numNodes = referencePoints.Count;
        Vector3 startRefPoint = road.laneReferencePoints[0];

        // Lines creation
        List<Line> lines = CreateLinesForRoadPoints(road);

        Vector3 leftBottom = road.leftBottom.position;
        float distanceBetweenCornerAndStart = Vector3.Distance(leftBottom, startRefPoint);

        // Calculate the offset points
        Vector3[][] lanePositions = new Vector3[numberOfLanes][];
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanePositions[i] = new Vector3[numNodes];
            for (int j = 0; j < numNodes; j++)
            {
                Line l = lines[j];
                //INVERTIR LINEDIR 
                Vector3 lineDir = new Vector3(1, 0, l.gradient).normalized;
                Vector3 lineCentre = new Vector3(l.pointOnLine_1.x, 0f, l.pointOnLine_1.y);

                float leftDistanceToCorner = Vector3.Distance(leftBottom, lineCentre + lineDir * 2);
                float rightDistanceToCorner = Vector3.Distance(leftBottom, lineCentre - lineDir * 2);
                if (leftDistanceToCorner > rightDistanceToCorner)
                {
                    lineDir = -lineDir;
                }
                if (numberOfLanes == 1)
                {
                    lanePositions[i][j] = referencePoints[j];
                }
                else if (numberOfLanes == 2)
                {
                    float distance = Mathf.Abs(distanceBetweenCornerAndStart * .5f);
                    switch (i)
                    {
                        case 0:
                            lanePositions[i][j] = lineCentre - lineDir * distance;
                            break;
                        case 1:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                lanePositions[i][j] = lineCentre + lineDir * distance;
                            }
                            else
                            {
                                int inverseJ = numNodes - j - 1;
                                lanePositions[i][inverseJ] = lineCentre + lineDir * distance;
                            }
                            break;
                    }

                }
                else if (numberOfLanes == 4)
                {
                    float distance = distanceBetweenCornerAndStart;
                    int inverseJ = numNodes - j - 1;
                    switch (i)
                    {
                        case 0:
                            lanePositions[i][j] = lineCentre - lineDir * distance * .8f;
                            break;
                        case 1:
                            lanePositions[i][j] = lineCentre - lineDir * distance * .3f;
                            break;
                        case 2:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                lanePositions[i][j] = lineCentre + lineDir * distance * .3f;
                            }
                            else
                            {
                                lanePositions[i][inverseJ] = lineCentre + lineDir * distance * .3f;
                            }
                            break;
                        case 3:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                lanePositions[i][j] = lineCentre + lineDir * distance * .8f;
                            }
                            else
                            {
                                lanePositions[i][inverseJ] = lineCentre + lineDir * distance * .8f;
                            }
                            break;
                    }
                }
            }
        }

        // With the lines and the offsets created, find points in the line matching the lane offset calculated before for each lane.
        for (int i = 0; i < numberOfLanes; i++)
        {
            Vector3 laneStartPoint = lanePositions[i][0];
            Vector3 laneEndPoint = lanePositions[i][numNodes - 1];
            debugNodes.Add(laneStartPoint);
            Node entryNode = new Node(laneStartPoint, road);
            Node exitNode = new Node(laneEndPoint, road);
            grid.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            road.entryNodes.Add(entryNode);
            road.exitNodes.Add(exitNode);
            Node previousNode = entryNode;

            for (int j = 1; j < numNodes - 1; j++)
            {
                Node newNode = new Node(lanePositions[i][j], road);
                previousNode.AddNeighbour(newNode);
                previousNode = newNode;
                grid.Add(newNode);
                road.lanes[i].nodes.Add(newNode);
            }
            previousNode.AddNeighbour(exitNode);
            road.lanes[i].nodes.Add(exitNode);
            grid.Add(exitNode);
        }
        ConnectNodesInRoad(road);
    }

    private List<Line> CreateLinesForRoadPoints(Road road)
    {
        List<Vector3> referencePoints = road.laneReferencePoints;
        int numNodes = referencePoints.Count;
        Vector3 startRefPoint = road.laneReferencePoints[0];

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
            Vector2 perpendicularPointToLine = previousPoint - dirToCurrentPoint * 1f;
            lines.Add(new Line(currentPoint, perpendicularPointToLine));
            debugLines.Add(new Line(currentPoint, perpendicularPointToLine));
            previousPoint = currentPoint;
        }
        road.curveRoadLines = lines;
        return lines;
    }
    private void ConnectNodesInRoad(Road road)
    {
        int numberOfLanes = road.lanes.Count;
        int numNodes = road.lanes[0].nodes.Count;
        // Iterar sobre los carriles para conectar los nodos entre carriles. Check la bidireccionalidad
        if (numberOfLanes == 2 && NumDirection.OneDirectional == road.numDirection)
        {
            List<Node> lane1Nodes = road.lanes[0].nodes;
            List<Node> lane2Nodes = road.lanes[1].nodes;
            for (int i = 0; i < numNodes - 1; i++)
            {
                lane1Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                lane2Nodes[i].AddNeighbour(lane1Nodes[i + 1]);
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
                for (int i = 0; i < numNodes - 1; i++)
                {
                    lane1Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                    lane2Nodes[i].AddNeighbour(lane1Nodes[i + 1]);
                    lane2Nodes[i].AddNeighbour(lane3Nodes[i + 1]);
                    lane3Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                    lane3Nodes[i].AddNeighbour(lane4Nodes[i + 1]);
                    lane4Nodes[i].AddNeighbour(lane3Nodes[i + 1]);
                }
            }
            else
            {
                for (int i = 0; i < numNodes - 1; i++)
                {
                    lane1Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                    lane2Nodes[i].AddNeighbour(lane1Nodes[i + 1]);
                }
                for (int i = 0; i < numNodes - 1; i++)
                {
                    lane3Nodes[i].AddNeighbour(lane4Nodes[i + 1]);
                    lane4Nodes[i].AddNeighbour(lane3Nodes[i + 1]);
                }
            }
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

            startPoints[1] = (startRefPoint + rightBottom) * 0.5f;
            endPoints[1] = GetOppositeVector(endPoints[0], endRefPoint);
        }
        else // caso 4 carriles
        {
            Vector3 leftHalf = (startRefPoint + leftBottom) * 0.5f;
            Vector3 leftHalfEnd = GetVectorWithSameDistanceAsSources(leftHalf, startRefPoint, endRefPoint);
            startPoints[0] = (leftHalf + leftBottom) * 0.5f;
            endPoints[0] = GetVectorWithSameDistanceAsSources(startPoints[0], leftHalf, leftHalfEnd);

            startPoints[1] = (startRefPoint + leftHalf) * 0.5f;
            endPoints[1] = GetVectorWithSameDistanceAsSources(leftHalf, startPoints[1], leftHalfEnd);
            endPoints[1] = GetOppositeVector(endPoints[0], leftHalfEnd);

            Vector3 rightBottom = GetOppositeVector(leftBottom, startRefPoint);
            Vector3 rightHalf = (startRefPoint + rightBottom) * 0.5f;
            Vector3 rightHalfEnd = GetVectorWithSameDistanceAsSources(rightHalf, startRefPoint, endRefPoint);


            startPoints[2] = (startRefPoint + rightHalf) * 0.5f;
            endPoints[2] = (endRefPoint + rightHalfEnd) * 0.5f;

            startPoints[3] = (rightHalf + rightBottom) * 0.5f;
            endPoints[3] = GetOppositeVector(endPoints[2], rightHalfEnd);
        }

        if (road.invertPath)
        {
            Vector3[] invertedStartPoints = new Vector3[numberOfLanes];
            Vector3[] invertedEndPoints = new Vector3[numberOfLanes];

            for (int i = 0; i < numberOfLanes; i++)
            {
                int invertedIndex = numberOfLanes - i - 1;
                invertedStartPoints[invertedIndex] = startPoints[i];
                invertedEndPoints[invertedIndex] = endPoints[i];
            }
            startPoints = invertedStartPoints;
            endPoints = invertedEndPoints;
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
            road.entryNodes.Add(entryNode);
            road.exitNodes.Add(exitNode);
            debugNodes.Add(entryNode.worldPosition);

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
                    previousNode.AddNeighbour(newNode);
                    grid.Add(newNode);
                    road.lanes[i].nodes.Add(newNode);
                    previousNode = newNode;
                }
                previousNode.AddNeighbour(exitNode);
            }
            else
            {
                entryNode.AddNeighbour(exitNode);
            }
            road.lanes[i].nodes.Add(exitNode);
            grid.Add(exitNode);
        }
        ConnectNodesInRoad(road);
    }

    #endregion

    #region Auxiliar methods
    private Vector2 V3ToV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
    }

    private Vector2 V2ToV3(Vector2 v2)
    {
        return new Vector3(v2.x, 0f, v2.y);
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

    #endregion
}

public class Pair
{
    public Node pointA;
    public Node pointB;

    public Pair(Node _pointA, Node _pointB)
    {
        pointA = _pointA;
        pointB = _pointB;
    }
}