using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;

    [Header("Specs")]
    [SerializeField] float speed = 4;
    [SerializeField] float turnSpeed = 4;
    [SerializeField] float turnDst = 3;
    [HideInInspector] public int pathIndex = 0;
    Path path = null;
    [SerializeField] public float speedPercent = 0f;

    // Stop at traffic light variables
    [Header("TrafficLight")]
    public bool shouldStopAtTrafficLight = false;
    private float trafficLightStopDist = 5;
    [HideInInspector] TrafficLightCarController trafficLightCarController;

    // Car collision avoidance variables
    [Header("CarAvoidance")]
    public bool reactingToCarInFront = false;
    public bool reactionDelay = false;
    public bool shouldBrakeBeforeCar = false;
    public bool brakingDelay = false;
    public bool accelerationDelay = false;
    [SerializeField] float carStartBreakingDistance = 2f;
    [SerializeField] float carStopDistance = 1f;
    [HideInInspector] public Vector3 frontCarPos;
    public Transform carTarget;
    [HideInInspector] public AvoidanceBehavior avoidanceBehavior;
    int recentAddedAvoidancePosIndex = -50;
    Node endNode;

    // Priority variables
    [Header("Priority")]
    public bool shouldStopPriority = false;
    public PriorityLevel priorityLevel = PriorityLevel.Max;
    [HideInInspector] public bool pathRequested = false;
    [HideInInspector] public Vector3 stopPosition = Vector3.zero;
    [HideInInspector] public PriorityBehavior targetPriorityBehavior;
    [HideInInspector] public PriorityBehavior priorityBehavior;

    // Pedestrian variables
    [Header("Pedestrian")]
    public bool shouldStopPedestrian = false;
    public Vector3 pedestrianStopPos;

    [Header("Others")]
    [SerializeField] bool pathDebug;
    public bool isFullyStopped = false;
    [HideInInspector] List<Vector3> waypointsList = new List<Vector3>();
    [HideInInspector] List<Node> nodeList = new List<Node>();

    private IEnumerator followPathCoroutine;
    private IEnumerator reactionTimeCoroutine;
    private static float minDistanceToSpawnNewTarget = 18f;



    // Falla si el objetivo se consigue dentro de una interseccion ya que al mandar una peticion de adquirir un nodo en la interseccion , se es incapaz.
    void Start()
    {
        carStartBreakingDistance = Random.Range(1.5f, 4f);
        //carStartBreakingDistance = 2.5f;
        trafficLightStopDist = Random.Range(4f, 6f);
        carStopDistance = Random.Range(1.3f, 1.7f);
        //carStopDistance = 1.5f;
        float speedMultiplier = Random.Range(0.9f, 1.6f);
        speed *= speedMultiplier;
        turnSpeed *= speedMultiplier;
        trafficLightCarController = GetComponent<TrafficLightCarController>();
        priorityLevel = PriorityLevel.Max;
        StartCoroutine(StartPathfindingOnWorldCreation());
    }

    IEnumerator StartPathfindingOnWorldCreation()
    {
        yield return new WaitForSeconds(1f);
        if (path == null)
        {
            Node targetNode = WorldGrid.Instance.GetRandomNodeInRoads();
            float newDistance = Vector3.Distance(targetNode.worldPosition, transform.position);
            while (newDistance < minDistanceToSpawnNewTarget)
            {
                targetNode = WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
                newDistance = Vector3.Distance(transform.position, targetNode.worldPosition);
            }

            PathfinderRequestManager.RequestPath(transform.position, targetNode, transform.forward, OnPathFound);
            pathRequested = true;
            StartCoroutine(UpdatePath());
        }
    }

    public void StartPathfindingOnSpawn(Node _startNode)
    {
        float newDistance = 0f;
        Vector3 newTargetPos = Vector3.zero;
        Node newNode = null;
        while (newDistance < minDistanceToSpawnNewTarget)
        {
            newNode = WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
            newTargetPos = newNode.worldPosition;
            newDistance = Vector3.Distance(_startNode.worldPosition, newTargetPos);
        }

        PathfinderRequestManager.RequestPath(_startNode, newNode, transform.forward, OnPathFound);
        pathRequested = true;
        StartCoroutine(UpdatePath());
    }

    public void OnPathFound(List<Node> waypointNodes, bool pathSuccessful, Node _startNode, Node _endNode)
    {
        if (pathSuccessful)
        {
            pathRequested = false;
            endNode = _endNode;
            nodeList = waypointNodes;
            waypointsList.Clear();
            foreach (Node node in waypointNodes)
            {
                waypointsList.Add(node.worldPosition);
            }
            if (followPathCoroutine != null)
                StopCoroutine(followPathCoroutine);
            path = new Path(waypointsList, transform.position, turnDst);
            followPathCoroutine = FollowPath();
            StartCoroutine(followPathCoroutine);
        }
        else
        {
            path = null;
            SpawnSpheres(_startNode.worldPosition, _endNode.worldPosition);
            Debug.Log("Path not found for car: " + gameObject.name);
        }
    }

    private void SpawnSpheres(Vector3 _startNode, Vector3 _endNode)
    {
        GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        startSphere.transform.parent = transform.parent;
        startSphere.transform.position = _startNode + Vector3.up;
        startSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.magenta);

        GameObject endSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        endSphere.transform.parent = transform.parent;
        endSphere.transform.position = _endNode + Vector3.up;
        endSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
    }

    IEnumerator UpdatePath()
    {
        //if (Time.timeSinceLevelLoad < .3f)
        //{
        //    yield return new WaitForSeconds(.3f);
        //}
        //if (target == Vector3.zero)
        //{
        //    target = WorldGrid.Instance.GetRandomNodeInRoads().worldPosition;
        //}
        //PathfinderRequestManager.RequestPath(transform.position, target, transform.forward, OnPathFound);

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);
            if (path != null)
            {
                float distance = Vector3.Distance(transform.position, endNode.worldPosition);
                if (distance < 3f)
                {
                    RequestNewPath();
                }
            }
        }
    }

    // This method returns the position in the path in pathIndex+numNodes
    public Vector3 GetPosInPathInNumNodes(int numNodes)
    {
        if (PathEnds(numNodes))
            return Vector3.zero;

        return waypointsList[pathIndex + numNodes];
    }

    public Node GetStoppingNodeFromCurrentNode()
    {
        Node currentNode = nodeList[pathIndex];
        Road currentRoad = currentNode.road;
        Node stoppingNode = null;
        int i = 0;
        bool roadChange = false;
        while (stoppingNode == null && !roadChange)
        {

            // TO FIX - Path ends
            // Hay que controlar que el indice no se pase de la capacidad maxima de la lista
            if (currentRoad.exitNodes.Contains(nodeList[pathIndex + i]))
            {
                stoppingNode = nodeList[pathIndex + i];
            }
            i++;

            if (PathEnds(i))
            {
                roadChange = true;
            }
            else
            {
                if (currentRoad != nodeList[pathIndex + i].road)
                    roadChange = true;
            }
        }
        // Hay que averiguar el stopping node de la carretera, tiene que ser del carril que esté en nuestra trayectoria. Bucle for de
        if (stoppingNode == null)
        {
            // If it has not been found it is because we have gone over the road

            Debug.LogWarning("NO STOPPING NODE HAS BEEN FOUND");
            if (roadChange)
            {
                Debug.LogWarning("THE PATH WAS ABOUT TO END");
            }
        }
        return stoppingNode;
    }

    public float GetAngleBetweenCurrentNodeAndNumNodes(int numNodes)
    {
        if (PathEnds(numNodes))
            return Mathf.Infinity;

        Vector3 dirFromCurrentNodeToTarget = (waypointsList[pathIndex + numNodes] - waypointsList[pathIndex]).normalized;
        Vector3 currentNodeForward = (waypointsList[pathIndex + 1] - waypointsList[pathIndex]).normalized;
        float angle = Vector3.Angle(currentNodeForward, dirFromCurrentNodeToTarget);
        return angle;
    }

    private bool PathEndsNoRequest(int numNodes)
    {
        int numNodesInPath = waypointsList.Count;
        if (pathIndex + numNodes >= numNodesInPath)
        {
            return true;
        }
        return false;
    }
    private bool PathEnds(int numNodes)
    {
        int numNodesInPath = waypointsList.Count;
        if (pathIndex + numNodes >= numNodesInPath)
        {
            RequestNewPath();
            return true;
        }
        return false;
    }
    public void RequestNewPath()
    {
        if (endNode == null)
            return;

        float newDistance = 0f;
        Vector3 newTargetPos = Vector3.zero;
        Node newNode = null;
        while (newDistance < minDistanceToSpawnNewTarget)
        {
            newNode = WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
            newTargetPos = newNode.worldPosition;
            newDistance = Vector3.Distance(endNode.worldPosition, newTargetPos);
        }

        PathfinderRequestManager.RequestPath(endNode, newNode, transform.forward, OnPathFound);
        pathRequested = true;
    }

    IEnumerator FollowPath()
    {
        pathIndex = 0;
        transform.LookAt(path.lookPoints[0]);

        // Check all the time if the unity has passed the boundari
        while (true)
        {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);

            while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
            {
                // Go to next point
                // Reset the obstacle avoider
                if (pathIndex == recentAddedAvoidancePosIndex)
                {
                    avoidanceBehavior.objectHit = false;
                    recentAddedAvoidancePosIndex = -50;
                }
                pathIndex++;
            }

            if (shouldStopPedestrian)
            {
                speedPercent = SlowSpeedPedestrian();
            }
            else if (shouldStopPriority)
            {
                speedPercent = SlowSpeedPriority();
            }
            else if (shouldBrakeBeforeCar)
            {
                speedPercent = SlowSpeedBeforeCar();
            }
            else if (shouldStopAtTrafficLight)
            {
                speedPercent = SlowSpeedAtTrafficLight();
            }
            else
            {
                speedPercent += 0.002f;
                speedPercent = Mathf.Clamp(speedPercent, 0f, 1f);
            }
            if (speedPercent > 0.001f) isFullyStopped = false;

            Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
            if (speedPercent > 0.1f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            transform.Translate(Vector3.forward * speed * Time.deltaTime * speedPercent, Space.Self);

            yield return null;

        }
    }
    public void StopAtTrafficLight(bool subscription)
    {
        if (shouldStopAtTrafficLight == false)
        {
            if (trafficLightCarController.currentRoad == null)
                return;

            if (reactionTimeCoroutine != null)
            {
                StopCoroutine(reactionTimeCoroutine);
            }
            reactionTimeCoroutine = TrafficLightReactionTime(subscription);
            StartCoroutine(reactionTimeCoroutine);
        }
    }

    public void ContinueAtTrafficLight(bool subscription)
    {
        // Start Coroutine to delay the variable change
        if (shouldStopAtTrafficLight == true)
        {
            if (reactionTimeCoroutine != null)
            {
                StopCoroutine(reactionTimeCoroutine);
            }
            reactionTimeCoroutine = TrafficLightReactionTime(subscription);
            StartCoroutine(reactionTimeCoroutine);
        }
    }

    IEnumerator TrafficLightReactionTime(bool subscription)
    {
        float reactionTime;
        if (subscription)
            reactionTime = 0f;
        else
            reactionTime = Random.Range(0.3f, 0.7f);
        yield return new WaitForSeconds(reactionTime);
        shouldStopAtTrafficLight = !shouldStopAtTrafficLight;
    }

    public void SetNewPathByAvoidance(Vector3 newPos)
    {
        //Create a new Path and add this position in the correct index.
        waypointsList.Insert(pathIndex, newPos);
        recentAddedAvoidancePosIndex = pathIndex;
        path = new Path(waypointsList, transform.position, turnDst);
    }

    float SlowSpeedPedestrian()
    {
        float _speedPercent;
        // Maybe its better to calculate a stop position before the crossing and not the targetPos
        float distance = Vector3.Distance(transform.position, pedestrianStopPos);
        _speedPercent = Mathf.Clamp01((distance - 1.5f) / carStartBreakingDistance);
        if (_speedPercent - speedPercent > 0.1f && _speedPercent > 0.5f)
            _speedPercent = speedPercent += 0.005f;

        if (_speedPercent < 0.03f)
        {
            _speedPercent = 0f;
        }
        return _speedPercent;
    }

    float SlowSpeedPriority()
    {
        float _speedPercent;
        float distance = Vector3.Distance(transform.position, stopPosition);
        _speedPercent = Mathf.Clamp01((distance - 1f) / carStartBreakingDistance);
        if (_speedPercent - speedPercent > 0.1f && _speedPercent > 0.5f) // The car was fully stopped
            _speedPercent = speedPercent += 0.005f;

        if (_speedPercent < 0.03f)
        {
            _speedPercent = 0f;
        }
        return _speedPercent;
    }

    float SlowSpeedBeforeCar()
    {
        if (reactionDelay)
            return speedPercent;

        float _speedPercent;
        //float distance = Vector3.Distance(transform.position, frontCarPos);
        float distance = Vector3.Distance(transform.position, carTarget.position);
        _speedPercent = Mathf.Clamp01((distance - carStopDistance) / carStartBreakingDistance);


        if (isFullyStopped && _speedPercent > 0f) // The car is fully stopped and the car in front is resuming the car
        {
            if (!reactingToCarInFront)
            {
                StartCoroutine(ResumeTheCar());
            }
            else
            {
                return 0f;
            }
        }

        if (speedPercent - _speedPercent > 0f && !brakingDelay) // Braking
        {
            StartCoroutine(BrakingDelay());
            return _speedPercent;
        }

        if (_speedPercent - speedPercent > 0f && !accelerationDelay) // Accelerating
        {
            StartCoroutine(AccelerationDelay());
            return _speedPercent;
        }

        // Accelerating - This is how I prevent the unreal acceleration when the car is fully stopped and grabs a fast target
        if (_speedPercent - speedPercent > 0.1f && _speedPercent > 0.5f)
            _speedPercent = speedPercent += 0.002f;


        if (_speedPercent < 0.05f) // Set the car to fully stopped
        {
            _speedPercent = 0f;
            isFullyStopped = true;
        }

        return _speedPercent;
    }

    IEnumerator BrakingDelay()
    {
        brakingDelay = true;
        reactionDelay = true;
        float reactionTime = Random.Range(0.3f, 0.7f);
        yield return new WaitForSeconds(reactionTime);
        accelerationDelay = false;
        reactionDelay = false;
    }

    IEnumerator AccelerationDelay()
    {
        accelerationDelay = true;
        reactionDelay = true;
        float reactionTime = Random.Range(0.3f, 0.7f);
        yield return new WaitForSeconds(reactionTime);
        brakingDelay = false;
        reactionDelay = false;
    }

    IEnumerator ResumeTheCar()
    {
        reactingToCarInFront = true;
        float reactionTime = Random.Range(0.3f, 0.9f);
        yield return new WaitForSeconds(reactionTime);
        isFullyStopped = false;
        reactingToCarInFront = false;
    }

    float SlowSpeedAtTrafficLight()
    {
        float distance = trafficLightCarController.GiveDistanceToPathFollower();
        speedPercent = Mathf.Clamp01((distance - 1.5f) / trafficLightStopDist);
        if (speedPercent < 0.04f)
        {
            speedPercent = 0f;
            isFullyStopped = true;
        }
        return speedPercent;
    }

    public void OnDrawGizmos()
    {
        if (pathDebug && path != null)
        {
            path.DrawWithGizmos();
        }
    }

}


public enum PriorityLevel
{
    Stop,
    Yield,
    Roundabout,
    Max
}
