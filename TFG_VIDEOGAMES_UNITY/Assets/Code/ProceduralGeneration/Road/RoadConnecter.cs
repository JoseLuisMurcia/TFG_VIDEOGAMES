using PG;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class RoadConnecter : MonoBehaviour
{
    [SerializeField] bool displayGridGizmos;
    private List<Road> roads = new List<Road>();
    private List<Node> nodes = new List<Node>();

    [SerializeField] LayerMask roadMask;
    [HideInInspector] public List<Node> unionNodes = new List<Node>();
    float distancePerNode = 2.5f;
    public static RoadConnecter Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            //DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public int MaxSize
    {
        get
        {
            return nodes.Count;
        }
    }
    public async Task ConnectRoads(List<GameObject> _roadsGameObject)
    {
        // Create all the nodes in a road
        foreach (GameObject roadGameObject in _roadsGameObject)
        {
            Road road = roadGameObject.GetComponent<Road>();
            if (road == null) continue;

            roads.Add(road);
            roadGameObject.SetActive(true);
            road.procedural = true;
            road.numberOfLanes = 2;
            road.numDirection = NumDirection.TwoDirectional;
            road.StartCoroutine(road.Restart());
        }
        await StartConnecting();
    }
    private Task StartConnecting()
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(StartConnectingCoroutine(tcs));
        return tcs.Task;
    }
    private IEnumerator StartConnectingCoroutine(TaskCompletionSource<bool> tcs)
    {
        yield return new WaitForSeconds(2f);
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
                    CreateNodesForDeviation((Deviation)road);
                    break;
                case TypeOfRoad.Intersection:
                    // No crea nodos, se encarga de conectar los nodos de sus conexiones
                    break;
                case TypeOfRoad.Roundabout:
                    CreateNodesForRoundabout((Roundabout)road);
                    break;
                case TypeOfRoad.Slant:
                    CreateNodesForSlant(road);
                    break;
                case TypeOfRoad.Bridge:
                    CreateNodesForBridge((Bridge)road);
                    break;
                case TypeOfRoad.StraightSlant:
                    CreateNodesForStraightRoad(road);
                    break;
            }
        }
        CreateConnectionsForRoundabouts();
        ConnectAllRoads();
        PostProcessNodes();
        tcs.SetResult(true);
    }
    private void PostProcessNodes()
    {
        foreach (Node node in nodes)
            node.SetLaneSide();
        foreach (Node node in unionNodes)
        {
            if (node.neighbours[0].laneSide == LaneSide.Left)
                node.laneSide = LaneSide.Left;
            else if (node.neighbours[0].laneSide == LaneSide.Right)
                node.laneSide = LaneSide.Right;
            else
                node.laneSide = LaneSide.None;
        }
        foreach (Road road in roads)
        {
            if (road.typeOfRoad == TypeOfRoad.Bridge)
            {
                road.lanes.Clear();
                Bridge bridge = (Bridge)road;
                foreach (Lane lowerLane in bridge.lowerLanes)
                {
                    bridge.lanes.Add(lowerLane);
                }
                foreach (Lane upperLane in bridge.upperLanes)
                {
                    bridge.lanes.Add(upperLane);
                }
            }
        }
    }
    private void CreateConnectionsForRoundabouts()
    {
        foreach (Road road in roads)
        {
            if (road.typeOfRoad == TypeOfRoad.Roundabout)
            {
                Roundabout roundabout = (Roundabout)road;
                foreach (Road connection in roundabout.connections)
                {
                    // Find the correct entries and exits
                    bool entryNotFound = true;
                    int i = 0;
                    int entryId = -1;
                    Vector3 middlePos = Vector3.zero;
                    while (entryNotFound && i < 4)
                    {
                        middlePos = (roundabout.entries[i].position + roundabout.exits[i].position) * .5f;
                        Vector3 dir = (middlePos - roundabout.transform.position).normalized;
                        Vector3 rayPos = middlePos + dir * 2f;
                        Vector3 rayOrigin = rayPos + Vector3.up * 25f;
                        Ray ray = new Ray(rayOrigin, Vector3.down);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, 55f, roadMask))
                        {
                            Road hitRoad = hit.collider.gameObject.GetComponent<Road>();
                            if (road != null && hitRoad == connection)
                            {
                                entryId = i;
                                entryNotFound = false;
                            }
                        }
                        i++;
                    }
                    // Now the entry and exit to the connection has been found
                    Transform entryTransform = null;
                    Transform exitTransform = null;
                    try
                    {
                        entryTransform = roundabout.entries[entryId];
                        exitTransform = roundabout.exits[entryId];
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("no entryId: " + ex.Message);
                        i = 0;
                        while (entryNotFound && i < 4)
                        {
                            middlePos = (roundabout.entries[i].position + roundabout.exits[i].position) * .5f;
                            Vector3 dir = (middlePos - roundabout.transform.position).normalized;
                            Vector3 rayPos = middlePos + dir * 2f;
                            Vector3 rayOrigin = rayPos + Vector3.up * 25f;
                            Ray ray = new Ray(rayOrigin, Vector3.down);
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit, 55f, roadMask))
                            {
                                Road hitRoad = hit.collider.gameObject.GetComponent<Road>();
                                Debug.DrawRay(rayPos + Vector3.up * 50, Vector3.down * 55f, Color.magenta, 30f);
                                if (road != null && hitRoad == connection)
                                {
                                    entryId = i;
                                    entryNotFound = false;
                                }
                            }
                            i++;
                        }
                    }
                   
                    Vector3 entry = entryTransform.position;
                    Vector3 exit = exitTransform.position;

                    if (connection.numberOfLanes == 1)
                    {
                        // Ahora hay que crear un Entry o un Exit node en funcion de si el nodo más cercano de esa carretera es entry o exit.
                        Node closestNodeFromConnection = GetClosestNodeToRoad(middlePos, connection);
                        bool createExit = false;
                        // El nodo vecino un entry y tenemos que crear unicamente un exit en la rotonda
                        if (connection.entryNodes.Contains(closestNodeFromConnection))
                        {
                            createExit = true;
                        }

                        float angleThreshold = 5f;
                        if (createExit)
                        {
                            Node exitNode = new Node(middlePos, roundabout);
                            roundabout.exitNodes.Add(exitNode);
                            nodes.Add(exitNode);

                            foreach (Lane lane in roundabout.lanes)
                            {
                                float bestDistance = Mathf.Infinity;
                                Node bestNode = null;
                                foreach (Node node in lane.nodes)
                                {
                                    float distance = Vector3.Distance(middlePos, node.worldPosition);
                                    Vector3 targetDir = node.worldPosition - middlePos;
                                    if (distance < bestDistance && Vector3.SignedAngle(exitTransform.forward, targetDir, Vector3.up) < -angleThreshold)
                                    {
                                        bestDistance = distance;
                                        bestNode = node;
                                    }
                                }
                                // Connect the best node to the exit
                                bestNode.AddNeighbour(exitNode);

                            }

                        }
                        else
                        {
                            Node entryNode = new Node(middlePos, roundabout);
                            roundabout.entryNodes.Add(entryNode);
                            nodes.Add(entryNode);

                            foreach (Lane lane in roundabout.lanes)
                            {
                                float bestDistance = Mathf.Infinity;
                                Node bestNode = null;
                                foreach (Node node in lane.nodes)
                                {
                                    float distance = Vector3.Distance(middlePos, node.worldPosition);
                                    Vector3 targetDir = node.worldPosition - middlePos;
                                    if (distance < bestDistance && Vector3.SignedAngle(entryTransform.forward, targetDir, Vector3.up) > angleThreshold)
                                    {
                                        bestDistance = distance;
                                        bestNode = node;
                                    }
                                }
                                // Connect the best node to the entry
                                entryNode.AddNeighbour(bestNode);

                            }
                        }



                    }
                    else
                    {
                        float angleThreshold = 2f;
                        if (connection.numDirection == NumDirection.OneDirectional) // 2 lanes and one direction
                        {
                            // Ahora hay que crear un Entry o un Exit node en funcion de si el nodo más cercano de esa carretera es entry o exit.
                            Node closestNodeFromConnection = GetClosestNodeToRoad(middlePos, connection);
                            bool createExit = false;
                            // El nodo vecino un entry y tenemos que crear unicamente un exit en la rotonda
                            if (connection.entryNodes.Contains(closestNodeFromConnection))
                            {
                                createExit = true;
                            }
                            if (createExit)
                            {
                                Node exitNode1 = new Node(exit, roundabout);
                                Node exitNode2 = new Node(entry, roundabout);
                                List<Node> exitNodes = new List<Node> { exitNode1, exitNode2 };

                                foreach (Node exitNode in exitNodes)
                                {
                                    nodes.Add(exitNode);
                                    roundabout.exitNodes.Add(exitNode);
                                    foreach (Lane lane in roundabout.lanes)
                                    {
                                        float bestDistance = Mathf.Infinity;
                                        Node bestNode = null;
                                        foreach (Node node in lane.nodes)
                                        {
                                            float distance = Vector3.Distance(exitNode.worldPosition, node.worldPosition);
                                            Vector3 targetDir = node.worldPosition - exitNode.worldPosition;
                                            if (distance < bestDistance && Vector3.SignedAngle(exitTransform.forward, targetDir, Vector3.up) < -angleThreshold)
                                            {
                                                bestDistance = distance;
                                                bestNode = node;
                                            }
                                        }
                                        // Connect the best node to the exit
                                        bestNode.AddNeighbour(exitNode);
                                    }
                                }
                            }
                            else
                            {
                                Node entryNode1 = new Node(exit, roundabout);
                                Node entryNode2 = new Node(entry, roundabout);
                                List<Node> entryNodes = new List<Node> { entryNode1, entryNode2 };

                                foreach (Node entryNode in entryNodes)
                                {
                                    nodes.Add(entryNode);
                                    roundabout.entryNodes.Add(entryNode);
                                    foreach (Lane lane in roundabout.lanes)
                                    {
                                        float bestDistance = Mathf.Infinity;
                                        Node bestNode = null;
                                        foreach (Node node in lane.nodes)
                                        {
                                            float distance = Vector3.Distance(entryNode.worldPosition, node.worldPosition);
                                            Vector3 targetDir = node.worldPosition - entryNode.worldPosition;
                                            Vector3 forward = entryNode.worldPosition == entryTransform.position ? entryTransform.forward : exitTransform.forward;
                                            if (distance < bestDistance && Vector3.SignedAngle(entryTransform.forward, targetDir, Vector3.up) > angleThreshold)
                                            {
                                                bestDistance = distance;
                                                bestNode = node;
                                            }
                                        }
                                        // Connect the best node to the entry
                                        entryNode.AddNeighbour(bestNode);

                                    }
                                }

                            }
                        }
                        else // 2 lanes and both directions
                        {
                            Node exitNode = new Node(exit, roundabout);
                            nodes.Add(exitNode);
                            roundabout.exitNodes.Add(exitNode);
                            foreach (Lane lane in roundabout.lanes)
                            {
                                float bestDistance = Mathf.Infinity;
                                Node bestNode = null;
                                foreach (Node node in lane.nodes)
                                {
                                    float distance = Vector3.Distance(exitNode.worldPosition, node.worldPosition);
                                    Vector3 targetDir = node.worldPosition - exitNode.worldPosition;
                                    if (distance < bestDistance && Vector3.SignedAngle(exitTransform.forward, targetDir, Vector3.up) < -angleThreshold)
                                    {
                                        bestDistance = distance;
                                        bestNode = node;
                                    }
                                }
                                // Connect the best node to the exit
                                bestNode.AddNeighbour(exitNode);
                            }

                            Node entryNode = new Node(entry, roundabout);
                            nodes.Add(entryNode);
                            roundabout.entryNodes.Add(entryNode);
                            foreach (Lane lane in roundabout.lanes)
                            {
                                float bestDistance = Mathf.Infinity;
                                Node bestNode = null;
                                foreach (Node node in lane.nodes)
                                {
                                    float distance = Vector3.Distance(entryNode.worldPosition, node.worldPosition);
                                    Vector3 targetDir = node.worldPosition - entryNode.worldPosition;
                                    if (distance < bestDistance && Vector3.SignedAngle(entryTransform.forward, targetDir, Vector3.up) > angleThreshold)
                                    {
                                        bestDistance = distance;
                                        bestNode = node;
                                    }
                                }
                                // Connect the best node to the entry
                                entryNode.AddNeighbour(bestNode);

                            }
                        }
                    }

                }
            }
        }
    }
    private Node GetClosestNodeToRoad(Vector3 position, Road connection)
    {
        Node entryNode = connection.entryNodes[0];
        Node exitNode = connection.exitNodes[0];
        if (Vector3.Distance(entryNode.worldPosition, position) > Vector3.Distance(exitNode.worldPosition, position))
        {
            return exitNode;
        }
        return entryNode;
    }
    private void PerformSpecialConnection(float distance, float dot, Node exitNode, Node entryNode, Road road)
    {
        if (distance < 3f && dot > 0)
        {
            Vector3 unionNodePos = (exitNode.worldPosition + entryNode.worldPosition) * 0.5f;
            Node unionNode = new Node(unionNodePos, road);
            foreach (Lane lane in road.lanes)
            {
                if (lane.nodes.Contains(exitNode))
                {
                    lane.nodes.Add(unionNode);
                    break;
                }
            }
            exitNode.AddNeighbour(unionNode);
            unionNode.AddNeighbour(entryNode);
            unionNodes.Add(unionNode);
            nodes.Add(unionNode);
        }
    }
    private void ConnectAllRoads()
    {
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
                        // Check dot, you cant connect an exit node to an entry that's behind
                        Vector3 exitNodeForward = (exitNode.worldPosition - exitNode.previousNode.worldPosition).normalized;
                        Vector3 dirToMovePosition = (neighbourEntryNode.worldPosition - exitNode.worldPosition).normalized;
                        float dot = Vector3.Dot(exitNodeForward, dirToMovePosition);
                        if (connection.typeOfRoad == TypeOfRoad.Deviation)
                        {
                            PerformSpecialConnection(distance, dot, exitNode, neighbourEntryNode, connection);
                        }
                        if (distance < bestDistance && dot > 0)
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
                    Node unionNode = new Node(unionNodePos, road);
                    foreach (Lane lane in road.lanes)
                    {
                        if (lane.nodes.Contains(exitNode))
                        {
                            lane.nodes.Add(unionNode);
                            break;
                        }
                    }
                    exitNode.AddNeighbour(unionNode);
                    unionNode.AddNeighbour(bestEntryNode);
                    unionNodes.Add(unionNode);
                    nodes.Add(unionNode);
                }
            }

            if (road.typeOfRoad == TypeOfRoad.Intersection)
            {
                ConnectRoadsThroughIntersection(road);
            }

        }
    }
    private void ConnectRoadsThroughIntersection(Road road)
    {
        // maxDistance should be generated taking into account the bounds size
        float intersectionSize = road.boxColliderSize;
        float maxDistance = intersectionSize * 1.5f;

        // No solo hay que conectar la interseccion con las carreteras colindantes, sino crear conexiones entre las colindantes a través de la interseccion
        // Coger todos los exit nodes de cada conexion y tratar de conectarlos con el resto de conexiones a través de sus entries
        road.numberOfLanes = 0;
        road.lanes = new List<Lane>();
        foreach (Road connecter in road.connections)
        {
            if (connecter.numberOfLanes >= 2)
            {
                road.numberOfLanes = 2;
                for (int i = 0; i < 2; i++)
                    road.lanes.Add(new Lane());
                break;
            }
        }
        if (road.numberOfLanes == 0)
        {
            road.numberOfLanes = 1;
            road.lanes = new List<Lane>();
            road.lanes.Add(new Lane());
        }

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
                                float signedAngle = Vector3.SignedAngle(exitNodeForward, dirToMovePosition, Vector3.up);
                                float absoluteAngle = Mathf.Abs(signedAngle);
                                float minAngle = 10f;
                                // Conectar rectas  // Aqui HAY QUE HACER ALGO COPON
                                if (absoluteAngle <= minAngle)
                                {
                                    // Perform connection
                                    Vector3 unionNodePos = (exit.worldPosition + entry.worldPosition) * 0.5f;
                                    Node unionNode = new Node(unionNodePos, road);
                                    exit.AddNeighbour(unionNode);
                                    unionNode.AddNeighbour(entry);
                                    if (connecter.numberOfLanes >= 2 && connecter.numDirection == NumDirection.OneDirectional && connected.numberOfLanes >= 2 && connected.numDirection == NumDirection.OneDirectional)
                                    {
                                        if (connecter.lanes[0].nodes.Contains(exit))
                                        {
                                            road.lanes[0].nodes.Add(unionNode);
                                        }
                                        else
                                        {
                                            road.lanes[1].nodes.Add(unionNode);
                                        }
                                    }
                                    else
                                    {
                                        road.lanes[0].nodes.Add(unionNode);
                                    }
                                    unionNodes.Add(unionNode);
                                    nodes.Add(unionNode);
                                }

                                // Conectar giros 
                                // Con el angle, sabemos si el nodo al que queremos ir está a la izquierda o a la derecha, con esto podremos crear un
                                // offset para un nodo intermedio, de forma que no pisen fuera de la carretera y sea más creible
                                minAngle = 20f;
                                float maxAngle = 60f;

                                if (absoluteAngle > minAngle && absoluteAngle < maxAngle)
                                {
                                    Vector2 dirToMove = new Vector2(dirToMovePosition.x, dirToMovePosition.z);
                                    float offsetInfluence = distance < 3f ? .3f : .7f;
                                    Vector3 unionNodePos = (exit.worldPosition + entry.worldPosition) * 0.5f;
                                    Vector2 perpendicularDir = Vector2.Perpendicular(dirToMove);
                                    Vector3 perpendicularDirection = new Vector3(perpendicularDir.x, 0, perpendicularDir.y);

                                    // TODO: IMPROVE READABILITY
                                    Vector3 firstUnionNodePos = Vector3.zero;
                                    Vector3 secondUnionNodePos = Vector3.zero;
                                    Vector3 middleUnionNodePos = Vector3.zero;
                                    if (signedAngle > 0)
                                    {
                                        unionNodePos = unionNodePos + perpendicularDirection * offsetInfluence;
                                        firstUnionNodePos = ((exit.worldPosition + unionNodePos) * .5f) + perpendicularDirection * offsetInfluence;
                                        secondUnionNodePos = (unionNodePos + entry.worldPosition) * .5f + perpendicularDirection * offsetInfluence;
                                        middleUnionNodePos = (firstUnionNodePos + secondUnionNodePos) * .5f + perpendicularDirection * offsetInfluence;
                                    }
                                    else
                                    {
                                        unionNodePos = unionNodePos - perpendicularDirection * offsetInfluence;
                                        firstUnionNodePos = ((exit.worldPosition + unionNodePos) * .5f) - perpendicularDirection * offsetInfluence;
                                        secondUnionNodePos = ((unionNodePos + entry.worldPosition) * .5f) - perpendicularDirection * offsetInfluence;
                                        middleUnionNodePos = (firstUnionNodePos + secondUnionNodePos) * .5f - perpendicularDirection * offsetInfluence;
                                    }

                                    Node firstUnionNode = new Node(firstUnionNodePos, road);
                                    Node secondUnionNode = new Node(secondUnionNodePos, road);
                                    Node middleUnionNode = new Node(middleUnionNodePos, road);
                                    exit.AddNeighbour(firstUnionNode);
                                    firstUnionNode.AddNeighbour(middleUnionNode);
                                    middleUnionNode.AddNeighbour(secondUnionNode);
                                    secondUnionNode.AddNeighbour(entry);
                                    unionNodes.Add(firstUnionNode);
                                    unionNodes.Add(middleUnionNode);
                                    unionNodes.Add(secondUnionNode);
                                    nodes.Add(firstUnionNode);
                                    nodes.Add(middleUnionNode);
                                    nodes.Add(secondUnionNode);
                                    if (road.numberOfLanes > 1)
                                    {
                                        road.lanes[1].nodes.Add(firstUnionNode);
                                        road.lanes[1].nodes.Add(middleUnionNode);
                                        road.lanes[1].nodes.Add(secondUnionNode);
                                    }
                                    else
                                    {
                                        road.lanes[0].nodes.Add(firstUnionNode);
                                        road.lanes[0].nodes.Add(middleUnionNode);
                                        road.lanes[0].nodes.Add(secondUnionNode);
                                    }

                                    //Node unionNode = new Node(unionNodePos, road);
                                    //exit.AddNeighbour(unionNode);
                                    //unionNode.AddNeighbour(entry);
                                    //if (road.numberOfLanes > 1)
                                    //{
                                    //    road.lanes[1].nodes.Add(unionNode);
                                    //}
                                    //else
                                    //{
                                    //    road.lanes[0].nodes.Add(unionNode);

                                    //}
                                    //unionNodes.Add(unionNode);
                                    //grid.Add(unionNode);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (road.numberOfLanes < 2)
            return;

        foreach (Lane lane in road.lanes)
        {
            foreach (Node unionNode in lane.nodes)
            {
                Node exit = unionNode.previousNode;
                Node entry = unionNode.neighbours[0];

                Vector3 exitNodeForward = (exit.worldPosition - exit.previousNode.worldPosition).normalized;
                Vector3 dirToMovePosition = (entry.worldPosition - exit.worldPosition).normalized;
                float signedAngle = Vector3.SignedAngle(exitNodeForward, dirToMovePosition, Vector3.up);
                float absoluteAngle = Mathf.Abs(signedAngle);
                float maxAngle = 4f;
                if (absoluteAngle <= maxAngle)
                {
                    Road connecter = exit.road;
                    Road connected = entry.road;
                    if (connecter.numberOfLanes >= 2 && connecter.numDirection == NumDirection.OneDirectional && connected.numberOfLanes >= 2 && connected.numDirection == NumDirection.OneDirectional)
                    {
                        if (road.lanes[0].nodes.Contains(unionNode))
                        {
                            unionNode.AddNeighbour(connected.entryNodes[1]);
                            connecter.exitNodes[1].AddNeighbour(unionNode);
                        }
                        else
                        {
                            unionNode.AddNeighbour(connected.entryNodes[0]);
                            connecter.exitNodes[0].AddNeighbour(unionNode);
                        }
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (nodes != null && displayGridGizmos)
        {
            foreach (Node n in nodes)
            {
                Gizmos.color = Color.white;

                //if (n.laneSide == LaneSide.Left)
                //{
                //    Gizmos.color = Color.magenta;
                //    Gizmos.DrawCube(n.worldPosition, Vector3.one * .2f);
                //}
                //else if (n.laneSide == LaneSide.Right)
                //{
                //    Gizmos.color = Color.black;
                //    Gizmos.DrawCube(n.worldPosition, Vector3.one * .2f);
                //}

                Gizmos.DrawCube(n.worldPosition, Vector3.one * .1f);

                foreach (Node neighbour in n.neighbours)
                {
                    Gizmos.color = Color.blue;
                    Vector3 yOffset = new Vector3(0, .1f, 0);
                    Gizmos.DrawLine(n.worldPosition + yOffset, neighbour.worldPosition + yOffset);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(n.worldPosition * .15f + neighbour.worldPosition * .85f + yOffset, .025f);
                }
            }

            foreach (Node union in unionNodes)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(union.worldPosition, Vector3.one * (.1f));
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
                foreach (Node entry in road.entryNodes)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(entry.worldPosition, Vector3.one * (0.15f));
                }
                foreach (Node entry in road.exitNodes)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(entry.worldPosition, Vector3.one * (0.15f));
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
            for (int i = 0; i < numNodes; i++)
            {
                newReferencePoints.Add(referencePoints[numNodes - i - 1]);
            }
            referencePoints = newReferencePoints;
        }

        Node entryNode = new Node(startRefPoint1, road);
        Node exitNode1 = new Node(endPoint1, road);
        Node exitNode2 = new Node(endPoint2, road);
        Node previousNode1 = entryNode;
        Node previousNode2 = entryNode;

        road.entryNodes.Add(entryNode);
        road.exitNodes.Add(exitNode1);
        road.exitNodes.Add(exitNode2);
        road.lanes[0].nodes.Add(entryNode);
        road.lanes[1].nodes.Add(entryNode);
        Vector3 newPoint2pos;
        for (int j = 1; j < numNodes - 1; j++)
        {
            Node newNode1 = new Node(referencePoints[j], road);
            if (road.numDirection == NumDirection.TwoDirectional)
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
            nodes.Add(newNode1);
            nodes.Add(newNode2);
            road.lanes[0].nodes.Add(newNode1);
            road.lanes[1].nodes.Add(newNode2);
        }
        road.lanes[0].nodes.Add(exitNode1);
        road.lanes[1].nodes.Add(exitNode2);
        previousNode1.AddNeighbour(exitNode1);
        previousNode2.AddNeighbour(exitNode2);
        nodes.Add(exitNode1);
        nodes.Add(exitNode2);

    }
    private void CreateNodesForDeviation(Deviation road)
    {
        int numLanes = road.numberOfLanes;
        Vector3 straightStartPos = road.startPos;
        Vector3 straightEndPos = road.endPos;
        Vector3[] startPositions = new Vector3[numLanes];
        Vector3[] endPositions = new Vector3[numLanes];
        List<Vector3> linePoints;
        bool invertPath = road.invertPath;
        if (numLanes == 2)
        {
            if (invertPath)
            {
                startPositions[0] = straightEndPos;
                endPositions[0] = straightStartPos;
                linePoints = road.laneReferencePoints;
                linePoints.Reverse();
            }
            else
            {
                startPositions[0] = straightStartPos;
                endPositions[0] = straightEndPos;
                linePoints = road.laneReferencePoints;
            }

        }
        else
        {
            startPositions[0] = (straightStartPos + road.laneWidthObjectPos) * 0.5f;
            startPositions[1] = GetOppositeVector(startPositions[0], straightStartPos);

            endPositions[0] = GetVectorWithSameDistanceAsSources(startPositions[0], straightStartPos, straightEndPos);
            endPositions[1] = GetVectorWithSameDistanceAsSources(startPositions[1], straightStartPos, straightEndPos);
            linePoints = road.laneTwoDirReferencePoints;
            if (invertPath)
            {
                Vector3 end0 = new Vector3(endPositions[0].x, endPositions[0].y, endPositions[0].z);
                Vector3 end1 = new Vector3(endPositions[1].x, endPositions[1].y, endPositions[1].z);
                endPositions[0] = startPositions[0];
                startPositions[0] = end0;
                if (road.numDirection == NumDirection.OneDirectional)
                {
                    endPositions[1] = startPositions[1];
                    startPositions[1] = end1;
                    linePoints.Reverse();
                }
            }
        }

        // Straight line creation
        Node entryNode;
        Node exitNode;
        Node previousNode;
        for (int i = 0; i < numLanes - 1; i++)
        {
            entryNode = new Node(startPositions[i], road);
            exitNode = new Node(endPositions[i], road);
            road.entryNodes.Add(entryNode);
            road.exitNodes.Add(exitNode);

            // Calculate all the possible nodes that could fit in a reasonable distance
            float distance = Vector3.Distance(entryNode.worldPosition, exitNode.worldPosition);
            float xDistance = GetDistanceToReach(entryNode.worldPosition.x, exitNode.worldPosition.x);
            float zDistance = GetDistanceToReach(entryNode.worldPosition.z, exitNode.worldPosition.z);
            int totalNodesInRoad = Mathf.FloorToInt(distance / distancePerNode);
            int nodesToAdd = totalNodesInRoad - 2;
            nodes.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            if (nodesToAdd > 0)
            {
                previousNode = entryNode;
                for (int j = 1; j <= nodesToAdd; j++)
                {
                    float multiplier = j / ((float)nodesToAdd + 1f);
                    Vector3 newNodePos;
                    newNodePos = new Vector3(entryNode.worldPosition.x + xDistance * multiplier, entryNode.worldPosition.y, entryNode.worldPosition.z + zDistance * multiplier);
                    Node newNode = new Node(newNodePos, road);
                    previousNode.AddNeighbour(newNode);
                    nodes.Add(newNode);
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
            nodes.Add(exitNode);
        }

        // Curve creation
        int numLinePoints = linePoints.Count;
        if (numLanes == 2)
        {
            entryNode = road.lanes[0].nodes[0];
        }
        else
        {
            entryNode = road.lanes[1].nodes[0];
        }

        exitNode = new Node(linePoints[numLinePoints - 1], road);
        road.exitNodes.Add(exitNode);
        previousNode = entryNode;
        for (int i = 1; i < numLinePoints - 1; i++)
        {
            Node newNode = new Node(linePoints[i], road);
            previousNode.AddNeighbour(newNode);
            previousNode = newNode;
            nodes.Add(newNode);
            road.lanes[numLanes - 1].nodes.Add(newNode);
        }
        previousNode.AddNeighbour(exitNode);
        road.lanes[numLanes - 1].nodes.Add(exitNode);
        nodes.Add(exitNode);

        // Straight line connections
        int numNodes = road.lanes[0].nodes.Count;
        if (numLanes == 3 && NumDirection.OneDirectional == road.numDirection)
        {
            List<Node> lane1Nodes = road.lanes[0].nodes;
            List<Node> lane2Nodes = road.lanes[1].nodes;
            for (int i = 0; i < numNodes - 1; i++)
            {
                lane1Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                lane2Nodes[i].AddNeighbour(lane1Nodes[i + 1]);
            }
        }
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
                float distance = Mathf.Abs(laneWidth * .5f);
                switch (i)
                {
                    case 0: // La interior
                        lanePositions[i][j] = lineCentre + lineDir * distance;
                        break;
                    case 1: // La exterior
                        lanePositions[i][j] = lineCentre - lineDir * distance;
                        break;
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
            nodes.Add(entryNode);
            roundabout.lanes[i].nodes.Add(entryNode);
            Node previousNode = entryNode;

            for (int j = 1; j < numNodes - 1; j++)
            {
                Node newNode = new Node(lanePositions[i][j], roundabout);
                previousNode.AddNeighbour(newNode);
                previousNode = newNode;
                nodes.Add(newNode);
                roundabout.lanes[i].nodes.Add(newNode);
            }
            previousNode.AddNeighbour(exitNode);
            roundabout.lanes[i].nodes.Add(exitNode);
            nodes.Add(exitNode);
        }

        // Connect nodes between lanes
        ConnectNodesInRoad(roundabout);
    }
    private List<Line> CreateLinesForRoundabout(Roundabout roundabout)
    {
        List<Vector3> referencePoints = roundabout.laneReferencePoints;
        int numNodes = referencePoints.Count;
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
                            if (road.invertPath)
                            {
                                lanePositions[i][j] = lineCentre - lineDir * distance;

                            }
                            else
                            {
                                if (road.numDirection == NumDirection.OneDirectional)
                                {
                                    lanePositions[i][j] = lineCentre + lineDir * distance;

                                }
                                else
                                {
                                    lanePositions[i][j] = lineCentre - lineDir * distance;
                                }

                            }
                            break;
                        case 1:
                            if (road.invertPath)
                            {
                                if (road.numDirection == NumDirection.OneDirectional)
                                {
                                    lanePositions[i][j] = lineCentre + lineDir * distance;
                                }
                                else
                                {
                                    int inverseJ = numNodes - j - 1;
                                    lanePositions[i][inverseJ] = lineCentre + lineDir * distance;
                                }
                            }
                            else
                            {
                                if (road.numDirection == NumDirection.OneDirectional)
                                {
                                    lanePositions[i][j] = lineCentre - lineDir * distance;
                                }
                                else
                                {
                                    int inverseJ = numNodes - j - 1;
                                    lanePositions[i][inverseJ] = lineCentre + lineDir * distance;
                                }
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

        if (road.invertPath)
        {
            Vector3[][] invertedLanePositions = new Vector3[numberOfLanes][];
            for (int i = 0; i < numberOfLanes; i++)
            {
                invertedLanePositions[i] = new Vector3[numNodes];
                for (int j = 0; j < numNodes; j++)
                {
                    invertedLanePositions[i][numNodes - j - 1] = lanePositions[i][j];
                }
            }

            lanePositions = invertedLanePositions;
        }

        // With the lines and the offsets created, find points in the line matching the lane offset calculated before for each lane.
        for (int i = 0; i < numberOfLanes; i++)
        {
            Vector3 laneStartPoint = lanePositions[i][0];
            Vector3 laneEndPoint = lanePositions[i][numNodes - 1];
            Node entryNode = new Node(laneStartPoint, road);
            Node exitNode = new Node(laneEndPoint, road);
            nodes.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            road.entryNodes.Add(entryNode);
            road.exitNodes.Add(exitNode);
            Node previousNode = entryNode;

            for (int j = 1; j < numNodes - 1; j++)
            {
                Node newNode = new Node(lanePositions[i][j], road);
                previousNode.AddNeighbour(newNode);
                previousNode = newNode;
                nodes.Add(newNode);
                road.lanes[i].nodes.Add(newNode);
            }
            previousNode.AddNeighbour(exitNode);
            road.lanes[i].nodes.Add(exitNode);
            nodes.Add(exitNode);
        }
        ConnectNodesInRoad(road);
    }
    private void CreateNodesForSlant(Road road)
    {
        int numberOfLanes = road.numberOfLanes;
        List<Vector3> referencePoints = road.laneReferencePoints;
        int numNodes = referencePoints.Count;
        float zWidth = road.bounds.extents.z * road.transform.localScale.z * .6F;

        // Calculate the offset points
        Vector3[][] lanePositions = new Vector3[numberOfLanes][];
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanePositions[i] = new Vector3[numNodes];
            for (int j = 0; j < numNodes; j++)
            {
                if (numberOfLanes == 1)
                {
                    lanePositions[i][j] = referencePoints[j];
                }
                else if (numberOfLanes == 2)
                {
                    float distance = zWidth * .5f;
                    switch (i)
                    {
                        case 0:
                            if (road.invertPath)
                            {
                                lanePositions[i][j] = referencePoints[j] + road.transform.forward * distance;
                            }
                            else
                            {
                                lanePositions[i][j] = referencePoints[j] - road.transform.forward * distance;

                            }
                            break;
                        case 1:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                if (road.invertPath)
                                {
                                    lanePositions[i][j] = referencePoints[j] - road.transform.forward * distance;
                                }
                                else
                                {
                                    lanePositions[i][j] = referencePoints[j] + road.transform.forward * distance;

                                }
                            }
                            else
                            {
                                int inverseJ = numNodes - j - 1;
                                lanePositions[i][inverseJ] = referencePoints[j] + road.transform.forward * distance;
                            }
                            break;
                    }

                }
                else if (numberOfLanes == 4)
                {
                    float distance = zWidth;
                    int inverseJ = numNodes - j - 1;
                    switch (i)
                    {
                        case 0:
                            lanePositions[i][j] = referencePoints[j] - road.transform.forward * distance * .8f;
                            break;
                        case 1:
                            lanePositions[i][j] = referencePoints[j] - road.transform.forward * distance * .3f;
                            break;
                        case 2:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                lanePositions[i][j] = referencePoints[j] + road.transform.forward * distance * .3f;
                            }
                            else
                            {
                                lanePositions[i][inverseJ] = referencePoints[j] + road.transform.forward * distance * .3f;
                            }
                            break;
                        case 3:
                            if (road.numDirection == NumDirection.OneDirectional)
                            {
                                lanePositions[i][j] = referencePoints[j] + road.transform.forward * distance * .8f;
                            }
                            else
                            {
                                lanePositions[i][inverseJ] = referencePoints[j] + road.transform.forward * distance * .8f;
                            }
                            break;
                    }
                }
            }
        }

        if (road.invertPath)
        {
            Vector3[][] invertedLanePositions = new Vector3[numberOfLanes][];
            for (int i = 0; i < numberOfLanes; i++)
            {
                invertedLanePositions[i] = new Vector3[numNodes];
                for (int j = 0; j < numNodes; j++)
                {
                    invertedLanePositions[i][numNodes - j - 1] = lanePositions[i][j];
                }
            }

            lanePositions = invertedLanePositions;
        }

        // With the lines and the offsets created, find points in the line matching the lane offset calculated before for each lane.
        for (int i = 0; i < numberOfLanes; i++)
        {
            Vector3 laneStartPoint = lanePositions[i][0];
            Vector3 laneEndPoint = lanePositions[i][numNodes - 1];
            Node entryNode = new Node(laneStartPoint, road);
            Node exitNode = new Node(laneEndPoint, road);
            nodes.Add(entryNode);
            road.lanes[i].nodes.Add(entryNode);
            road.entryNodes.Add(entryNode);
            road.exitNodes.Add(exitNode);
            Node previousNode = entryNode;

            for (int j = 1; j < numNodes - 1; j++)
            {
                Node newNode = new Node(lanePositions[i][j], road);
                previousNode.AddNeighbour(newNode);
                previousNode = newNode;
                nodes.Add(newNode);
                road.lanes[i].nodes.Add(newNode);
            }
            previousNode.AddNeighbour(exitNode);
            road.lanes[i].nodes.Add(exitNode);
            nodes.Add(exitNode);
        }
        // Loop and swap the lanes? xd
        ConnectNodesInRoad(road);
    }
    private void CreateNodesForBridge(Bridge road)
    {
        Vector3 startPoint = road.laneReferencePoints[0];
        Vector3 endPoint = road.laneReferencePoints[1];
        Vector3 middlePoint = (startPoint + endPoint) * .5f;

        float upperBridgeHeight = road.transform.TransformPoint(road.bounds.max).y;
        Vector3 angles = new Vector3(0, -90, 0);
        Vector3 upperStartPoint = RotatePointAroundPivot(startPoint, middlePoint, angles);
        upperStartPoint.y = upperBridgeHeight;
        Vector3 upperEndPoint = RotatePointAroundPivot(endPoint, middlePoint, angles);
        upperEndPoint.y = upperBridgeHeight;

        CreateNodesFromStartAndEnd(road, startPoint, endPoint, road.lowerNumDirection, road.invertLowerRoad, road.lowerRoadNumLanes, false, road.lowerLanes);
        CreateNodesFromStartAndEnd(road, upperStartPoint, upperEndPoint, road.upperNumDirection, road.invertUpperRoad, road.upperRoadNumLanes, true, road.upperLanes);
    }
    private void CreateNodesFromStartAndEnd(Road road, Vector3 start, Vector3 end, NumDirection numDirection, bool invert, int numberOfLanes, bool upperBridge, List<Lane> lanes)
    {
        Vector3[] startPoints = new Vector3[numberOfLanes];
        Vector3[] endPoints = new Vector3[numberOfLanes];
        float zWidth = road.bounds.extents.z * road.transform.localScale.z * 0.6f;
        if (numberOfLanes == 1)
        {
            startPoints[0] = start;
            endPoints[0] = end;
        }
        else if (numberOfLanes == 2)
        {
            float distance = zWidth * .5f;
            Vector3 direction = upperBridge ? -road.transform.right : road.transform.forward;

            if (road.typeOfRoad == TypeOfRoad.StraightSlant)
            {
                if (invert)
                {
                    startPoints[0] = end - direction * distance;
                    endPoints[0] = start - direction * distance;

                    startPoints[1] = end + direction * distance;
                    endPoints[1] = start + direction * distance;
                }
                else
                {
                    startPoints[0] = start + direction * distance;
                    endPoints[0] = end + direction * distance;

                    startPoints[1] = start - direction * distance;
                    endPoints[1] = end - direction * distance;
                }

            }
            else if (road.typeOfRoad == TypeOfRoad.Bridge)
            {
                if (invert)
                {
                    startPoints[0] = end - direction * distance;
                    endPoints[0] = start - direction * distance;

                    startPoints[1] = end + direction * distance;
                    endPoints[1] = start + direction * distance;
                }
                else
                {
                    startPoints[0] = start + direction * distance;
                    endPoints[0] = end + direction * distance;

                    startPoints[1] = start - direction * distance;
                    endPoints[1] = end - direction * distance;
                }
            }
            else
            {
                startPoints[0] = start - direction * distance;
                endPoints[0] = end - direction * distance;
                //SpawnSpheres(startPoints[0], endPoints[0]);
                startPoints[1] = start + direction * distance;
                endPoints[1] = end + direction * distance;
                //SpawnSpheres(startPoints[1], endPoints[1]);
                //SpawnSphere(start, Color.red, 3f, 3f);
                //SpawnSphere(end, Color.blue, 3f, 3f);
            }

        }
        else // caso 4 carriles
        {
            startPoints[0] = start - road.transform.forward * zWidth * .8f;
            endPoints[0] = end - road.transform.forward * zWidth * .8f;

            startPoints[1] = start - road.transform.forward * zWidth * .3f;
            endPoints[1] = end - road.transform.forward * zWidth * .3f;

            startPoints[2] = start + road.transform.forward * zWidth * .3f;
            endPoints[2] = end + road.transform.forward * zWidth * .3f;

            startPoints[3] = start + road.transform.forward * zWidth * .8f;
            endPoints[3] = end + road.transform.forward * zWidth * .8f;
        }

        if (invert && TypeOfRoad.StraightSlant != road.typeOfRoad && TypeOfRoad.Bridge != road.typeOfRoad)
        {
            Vector3[] invertedStartPoints = new Vector3[numberOfLanes];
            Vector3[] invertedEndPoints = new Vector3[numberOfLanes];

            for (int i = 0; i < numberOfLanes; i++)
            {
                invertedStartPoints[i] = endPoints[i];
                invertedEndPoints[i] = startPoints[i];
            }
            startPoints = invertedStartPoints;
            endPoints = invertedEndPoints;
        }

        for (int i = 0; i < numberOfLanes; i++)
        {
            Node entryNode;
            Node exitNode;
            if (numDirection == NumDirection.TwoDirectional && i < numberOfLanes / 2 && numberOfLanes > 1)
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

            // Calculate all the possible nodes that could fit in a reasonable distance
            float distance = Vector3.Distance(entryNode.worldPosition, exitNode.worldPosition);
            float xDistance = GetDistanceToReach(entryNode.worldPosition.x, exitNode.worldPosition.x);
            float yDistance = GetDistanceToReach(entryNode.worldPosition.y, exitNode.worldPosition.y);
            float zDistance = GetDistanceToReach(entryNode.worldPosition.z, exitNode.worldPosition.z);
            int totalNodesInRoad = Mathf.FloorToInt(distance / distancePerNode);
            int nodesToAdd = totalNodesInRoad - 2;
            nodes.Add(entryNode);
            lanes[i].nodes.Add(entryNode);
            if (nodesToAdd > 0)
            {
                Node previousNode = entryNode;
                for (int j = 1; j <= nodesToAdd; j++)
                {
                    float multiplier = j / ((float)nodesToAdd + 1f);
                    Vector3 newNodePos;
                    newNodePos = new Vector3(entryNode.worldPosition.x + xDistance * multiplier, entryNode.worldPosition.y + yDistance * multiplier, entryNode.worldPosition.z + zDistance * multiplier);
                    Node newNode = new Node(newNodePos, road);
                    previousNode.AddNeighbour(newNode);
                    nodes.Add(newNode);
                    lanes[i].nodes.Add(newNode);
                    previousNode = newNode;
                }
                previousNode.AddNeighbour(exitNode);
            }
            else
            {
                entryNode.AddNeighbour(exitNode);
            }
            lanes[i].nodes.Add(exitNode);
            nodes.Add(exitNode);
        }
    }
    private void CreateNodesForStraightRoad(Road road)
    {
        int numberOfLanes = road.numberOfLanes;
        Vector3 startRefPoint = road.laneReferencePoints[0];
        Vector3 endRefPoint = road.laneReferencePoints[1];
        road.RecalculateReferencePoints();
        CreateNodesFromStartAndEnd(road, startRefPoint, endRefPoint, road.numDirection, road.invertPath, numberOfLanes, false, road.lanes);
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
            if (road.typeOfRoad == TypeOfRoad.Roundabout)
            {
                lane1Nodes[numNodes - 1].AddNeighbour(lane2Nodes[0]);
                lane2Nodes[numNodes - 1].AddNeighbour(lane1Nodes[0]);
            }

            if (road.typeOfRoad != TypeOfRoad.Curve)
            {
                for (int i = 0; i < numNodes - 1; i++)
                {
                    lane1Nodes[i].AddNeighbour(lane2Nodes[i + 1]);
                    lane2Nodes[i].AddNeighbour(lane1Nodes[i + 1]);
                }
            }
            else
            {
                for (int i = 0; i < numNodes - 2; i++)
                {
                    lane1Nodes[i].AddNeighbour(lane2Nodes[i + 2]);
                    lane2Nodes[i].AddNeighbour(lane1Nodes[i + 2]);
                }
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

    #endregion

    #region Auxiliar methods
    private void SpawnSphere(Vector3 pos, Color color, float offset, float size)
    {
        GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        startSphere.transform.parent = transform;
        startSphere.transform.localScale = Vector3.one * size;
        startSphere.transform.position = pos + Vector3.up * 3f * offset;
        startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
    }
    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - new Vector3(pivot.x, pivot.y, pivot.z);
        dir = Quaternion.Euler(angles) * dir;
        point = dir + pivot;
        return point;
    }
    // Method that finds a random node in roads, used to spawn a car in it and to acquire a new target node
    public Node GetRandomNodeInRoads()
    {
        Node randomNode = null;
        int numRoads = roads.Count;
        int roadIndex = Random.Range(0, numRoads);
        Road selectedRoad = roads[roadIndex];
        // Do not select an intersection
        while (selectedRoad.typeOfRoad == TypeOfRoad.Intersection || selectedRoad.typeOfRoad == TypeOfRoad.Roundabout)
        {
            roadIndex = Random.Range(0, numRoads);
            selectedRoad = roads[roadIndex];
        }
        int numLanes = selectedRoad.numberOfLanes;

        if (numLanes > selectedRoad.numberOfLanes)
        {
            RandomNodeInRoadsDebug(selectedRoad);
        }

        int selectedLane = Random.Range(0, numLanes);
        int numNodes = selectedRoad.lanes[selectedLane].nodes.Count;
        if (numNodes > 0)
        {
            int selectedNode = Random.Range(1, numNodes - 1);
            randomNode = selectedRoad.lanes[selectedLane].nodes[selectedNode];
        }

        if (randomNode == null)
        {
            Debug.LogError("SE ROMPIO EL GetRandomNodeInRoads PUTA MADRE");
        }
        return randomNode;
    }
    private void RandomNodeInRoadsDebug(Road road)
    {
        Debug.LogWarning("ROAD WITH MORE NUMBER OF LANES THAN OBJECT LANES");
        Debug.LogWarning("Road: " + road.gameObject.name + ". TypeOfRoad: " + road.typeOfRoad.ToString() + ". Number of lanes: " + road.numberOfLanes + ". Lanes Count: " + road.lanes.Count);
    }

    private Vector2 V3ToV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
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
        Node closestNode = null;
        float bestDistance = float.PositiveInfinity;
        Vector3[] rayPositions = new Vector3[3];
        rayPositions[0] = worldPoint;
        rayPositions[1] = worldPoint + carForward * 1.5f;
        rayPositions[2] = worldPoint + carForward * 2.5f;
        int i = 0;
        while (closestNode == null && i < rayPositions.Length)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayPositions[i] + Vector3.up * 50, Vector3.down);
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
            i++;
        }

        if (closestNode == null)
        {
            i = 0;
            while (closestNode == null && i < rayPositions.Length)
            {
                RaycastHit hit;
                Ray ray = new Ray(rayPositions[i] + Vector3.up * 50, Vector3.down);
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
                i++;
            }
            CreateDebugRays(rayPositions);

            Debug.LogError("SE ROMPIO EL START NODE LA CONCHA DE LA LORA");
        }
        return closestNode;
    }

    private void CreateDebugRays(Vector3[] rayPositions)
    {
        foreach (Vector3 ray in rayPositions)
            Debug.DrawLine(ray + Vector3.down * 5f, ray + Vector3.up * 5f, Color.red, 50f);
    }

    public Node FindEndNode(Vector3 worldPoint)
    {
        Node closestNode = null;
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
        if (closestNode == null)
        {

            Debug.LogError("SE ROMPIO EL END NODE LA CONCHA DE LA LORA");

        }
        return closestNode;
    }

    #endregion
    private void SpawnSpheres(Vector3 _startNode, Vector3 _endNode)
    {
        GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        startSphere.name = "NewStartNode";
        startSphere.transform.parent = transform.parent;
        startSphere.transform.position = _startNode + Vector3.up * 2.5f;
        startSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.magenta);

        GameObject endSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        endSphere.name = "NewEndNode";
        endSphere.transform.parent = transform.parent;
        endSphere.transform.position = _endNode + Vector3.up * 2.5f;
        endSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
    }
}
