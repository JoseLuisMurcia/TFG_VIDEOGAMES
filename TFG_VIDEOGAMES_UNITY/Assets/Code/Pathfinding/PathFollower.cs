using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;

    [Header("Specs")]
    private TypeOfCar typeOfCar;
    [SerializeField] public float speed;
    [SerializeField] float turnSpeed;
    [SerializeField] float turnDst;
    [HideInInspector] public int pathIndex = 0;
    Path path = null;
    [SerializeField] public float speedPercent = 0f;
    public float movementSpeed = 0f;

    // Stop at traffic light variables
    [Header("TrafficLight")]
    public bool shouldStopAtTrafficLight = false;
    private float trafficLightStopDist = 5;
    [HideInInspector] TrafficLightCarController trafficLightCarController;

    // Car collision avoidance variables
    [Header("CarAvoidance")]
    public bool reactingToCarInFront = false;
    public bool shouldBrakeBeforeCar = false;
    [SerializeField] float carStartBreakingDistance;
    [SerializeField] float carStopDistance;
    [HideInInspector] public Transform carTarget;
    [HideInInspector] public PathFollower targetPathFollower;
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
    [HideInInspector] private float carStartBreakingDistancePriority;


    // Pedestrian variables
    [Header("Pedestrian")]
    public bool shouldStopPedestrian = false;
    public Vector3 pedestrianStopPos;

    [Header("Others")]
    [SerializeField] bool pathDebug;
    public bool isFullyStopped = false;
    [HideInInspector] List<Vector3> waypointsList = new List<Vector3>();
    [HideInInspector] public List<Node> nodeList = new List<Node>();
    [SerializeField] bool codeDebug;

    [Header("Reactions")]
    public bool reactionDelay = false;
    public bool adjustingDistance = false;
    public bool cooldown = false;
    public float distanceToTarget = -1f;
    float originalBreakingDistance;

    [Header("Overtake")]
    public bool roadValidForOvertaking;
    public LaneSide laneSide = LaneSide.None;
    public bool overtaking = false;
    public OvertakeBehavior overtakeBehavior;

    private IEnumerator followPathCoroutine;
    private IEnumerator reactionTimeCoroutine;
    private static float minDistanceToSpawnNewTarget = 18f;

    private void Awake()
    {

        switch (gameObject.name)
        {
            case "delivery" + "(Clone)":
                typeOfCar = TypeOfCar.Delivery;
                break;
            case "sedan" + "(Clone)":
                typeOfCar = TypeOfCar.Sedan;
                break;
            case "sedanSport" + "(Clone)":
                typeOfCar = TypeOfCar.SedanSport;
                break;
            case "suv" + "(Clone)":
                typeOfCar = TypeOfCar.Suv;
                break;
            case "suvLuxury" + "(Clone)":
                typeOfCar = TypeOfCar.SuvLuxury;
                break;
            case "truck" + "(Clone)":
                typeOfCar = TypeOfCar.Truck;
                break;
            case "van" + "(Clone)":
                typeOfCar = TypeOfCar.Van;
                break;
            default:
                Debug.LogWarning("haha wtf bro: " + gameObject.name);
                break;
        }
    }
    private void SetSpecsForTypeCar()
    {
        float speedMultiplier;
        switch (typeOfCar)
        {
            case TypeOfCar.Delivery:
                carStartBreakingDistance = Random.Range(2.5f, 4f);
                //carStartBreakingDistance = 2.5f;
                trafficLightStopDist = Random.Range(4.5f, 6.5f);
                carStartBreakingDistancePriority = Random.Range(3, 5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                //carStopDistance = 1.5f;
                speedMultiplier = Random.Range(0.7f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.Sedan:
                carStartBreakingDistance = Random.Range(1.5f, 4f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(3, 4f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                speedMultiplier = Random.Range(0.9f, 1.6f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.SedanSport:
                carStartBreakingDistance = Random.Range(1.5f, 4f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(3.5f, 5f);

                carStopDistance = Random.Range(1.35f, 1.65f);
                speedMultiplier = Random.Range(1.3f, 1.9f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.Suv:
                carStartBreakingDistance = Random.Range(1.5f, 4f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(3, 4f);

                carStopDistance = Random.Range(1.35f, 1.65f);
                speedMultiplier = Random.Range(1f, 1.3f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.SuvLuxury:
                carStartBreakingDistance = Random.Range(1.5f, 4f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(2.7f, 4f);

                carStopDistance = Random.Range(1.35f, 1.65f);
                speedMultiplier = Random.Range(1.2f, 1.6f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.Truck:
                carStartBreakingDistance = Random.Range(2.5f, 5f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(3, 5.5f);

                carStopDistance = Random.Range(1.3f, 1.7f);
                speedMultiplier = Random.Range(0.8f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            case TypeOfCar.Van:
                carStartBreakingDistance = Random.Range(1.5f, 4f);
                trafficLightStopDist = Random.Range(4f, 6f);
                carStartBreakingDistancePriority = Random.Range(3, 4.5f);

                carStopDistance = Random.Range(1.35f, 1.65f);
                speedMultiplier = Random.Range(0.7f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier;
                break;
            default:
                Debug.Log("haha wtf bro");
                speed = 3f;
                turnSpeed = 3.2f;
                turnDst = 0.5f;
                break;
        }

        originalBreakingDistance = carStartBreakingDistance;
    }
    void Start()
    {
        SetSpecsForTypeCar();
        trafficLightCarController = GetComponent<TrafficLightCarController>();
        priorityLevel = PriorityLevel.Max;
        StartCoroutine(StartPathfindingOnWorldCreation());
    }
    private void Update()
    {
        if (path != null)
        {
            Road currentRoad = nodeList[pathIndex].road;
            LaneSide currentLane = nodeList[pathIndex].laneSide;
            if (currentRoad.numberOfLanes >= 2 && currentLane != LaneSide.None
                && currentRoad.typeOfRoad != TypeOfRoad.Roundabout && currentRoad.typeOfRoad != TypeOfRoad.Intersection && currentRoad.typeOfRoad != TypeOfRoad.Deviation)
            {
                Debug.Log("OVERTAKIN");
                roadValidForOvertaking = true;
                if (nodeList[pathIndex].laneSide == LaneSide.Left)
                {
                    laneSide = LaneSide.Left;
                }
                else
                {
                    laneSide = LaneSide.Right;
                }
            }
            else
            {
                roadValidForOvertaking = false;
                laneSide = LaneSide.None;
            }
        }
        else
        {
            laneSide = LaneSide.None;
        }
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
    public void OnPathFound(PathfindingResult pathfindingResult, bool pathSuccessful, Node _startNode, Node _endNode)
    {
        if (pathSuccessful)
        {
            if (path == null)
            {
                nodeList = pathfindingResult.nodes;
                waypointsList = pathfindingResult.pathPositions;
            }
            else
            {
                AdjustActualPathToReceivedPath(pathfindingResult);
            }

            pathRequested = false;
            endNode = _endNode;
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
    private void AdjustActualPathToReceivedPath(PathfindingResult pathfindingResult)
    {
        // En lugar de hacer esto, hay que ajustarlo
        // Si quedan 3 nodos del camino actual, no vamos a ir directamente al nodo final y empezar el nuevo path, añadimos al path recibido los nodos que quedan.
        int maxNodes = nodeList.Count;
        List<Node> lastNodesInCurrPath = new List<Node>();
        List<Vector3> lastPosInCurrPath = new List<Vector3>();
        for (int i = pathIndex; i < maxNodes - 1; i++)
        {
            lastNodesInCurrPath.Add(nodeList[i]);
            lastPosInCurrPath.Add(waypointsList[i]);
        }
        nodeList = lastNodesInCurrPath;
        waypointsList = lastPosInCurrPath;
        nodeList.AddRange(pathfindingResult.nodes);
        waypointsList.AddRange(pathfindingResult.pathPositions);
    }
    public void OnLaneSwap(PathfindingResult pathfindingResult, bool pathSuccessful, Node _startNode, Node _endNode)
    {
        if (pathSuccessful)
        {
            pathRequested = false;
            endNode = _endNode;
            nodeList = pathfindingResult.nodes;
            waypointsList.Clear();
            waypointsList = pathfindingResult.pathPositions;

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
            Debug.LogError("Lane swap not found for car: " + gameObject.name);
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
        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);
            if (path != null)
            {
                int numNodesInPath = path.lookPoints.Count;
                if (pathIndex >= numNodesInPath - 6)
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
    public void RequestLaneSwap()
    {
        PathfinderRequestManager.RequestLaneSwap(nodeList[pathIndex], OnLaneSwap);
        pathRequested = true;
    }
    IEnumerator FollowPath()
    {
        pathIndex = 0;
        transform.LookAt(path.lookPoints[0]);

        // Check all the time if the unity has passed the boundaries
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
            movementSpeed = speed * speedPercent;
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
        if (priorityLevel == PriorityLevel.Roundabout)
        {
            _speedPercent = Mathf.Clamp01(distance / 5f);
        }
        else
        {
            _speedPercent = Mathf.Clamp01(distance / 8f);
        }

        //if (_speedPercent - speedPercent > 0.1f && _speedPercent > 0.5f) // The car was fully stopped
        //    _speedPercent = speedPercent + 0.005f;

        if (speedPercent - _speedPercent > 0.5f)
            _speedPercent = speedPercent - 0.005f;

        if (_speedPercent < 0.03f)
        {
            isFullyStopped = true;
            _speedPercent = 0f;
        }
        return _speedPercent;
    }
    float SlowSpeedBeforeCar()
    {
        if (reactionDelay)
            return speedPercent;

        float _speedPercent;
        float distance = Vector3.Distance(transform.position, carTarget.position);
        distanceToTarget = distance;
        _speedPercent = Mathf.Clamp01((distance - carStopDistance) / carStartBreakingDistance);

        if (laneSide == LaneSide.Right && speed - targetPathFollower.speed > 0.3f && overtakeBehavior.canSwapLane && !overtaking)
        {
            RequestLaneSwap();
            SpawnSpheres(transform.position, targetPathFollower.transform.position);
            overtaking = true;
            avoidanceBehavior.AddCarToBlacklist(targetPathFollower);
            overtakeBehavior.overtakenCar = targetPathFollower;
            avoidanceBehavior.UnableTarget();
            return speedPercent;
        }

        if (!adjustingDistance && targetPathFollower.movementSpeed - movementSpeed < 0.05f && !isFullyStopped && !cooldown && !targetPathFollower.isFullyStopped)
        {
            StartCoroutine(AdjustDistance());
        }


        if (isFullyStopped && _speedPercent > 0.01f) // The car is fully stopped and the car in front is resuming the car
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


        if (_speedPercent - speedPercent > 0.1f)
        {
            _speedPercent = speedPercent + 0.005f;
        }
        else if (speedPercent - _speedPercent >= 0.2f)
        {
            _speedPercent = speedPercent - 0.01f;
        }


        if (_speedPercent < 0.05f && !reactingToCarInFront) // Set the car to fully stopped
        {
            _speedPercent = 0f;
            isFullyStopped = true;
        }

        return _speedPercent;
    }
    IEnumerator AdjustDistance()
    {
        adjustingDistance = true;

        float minRange = 1.5f;
        float maxRange = 10f;
        float increaseValue = 0.04f;
        int numIterations = 60;
        int randomInt = Random.Range(0, 3);
        // It updates every 10th of a second, so 40 iterations means 4 seconds of adjusting distance
        float updateFreq = .1f;

        // Security check so that it does not get stupid close
        if (randomInt == 0)
        {
            float distanceResult = carStartBreakingDistance - increaseValue * numIterations;
            if (distanceResult < minRange)
                randomInt = 1;
        }
        else
        {
            float distanceResult = carStartBreakingDistance + increaseValue * numIterations;
            if (distanceResult > maxRange)
                randomInt = 0;
        }

        int i = 0;
        while (i < numIterations)
        {
            yield return new WaitForSeconds(updateFreq);
            if (randomInt == 0)
            {
                // Get closer to the car in front
                carStartBreakingDistance -= increaseValue;
            }
            else
            {
                // Get further from the car in front
                carStartBreakingDistance += increaseValue;
            }
            i++;
        }

        adjustingDistance = false;
        cooldown = true;
        StartCoroutine(AdjustDistanceCooldown());
    }
    IEnumerator AdjustDistanceCooldown()
    {
        yield return new WaitForSeconds(2f);
        cooldown = false;
    }
    public void UnableTarget() // Habría que investigar por qué a veces se llama a unable target cuando no se debería
    {
        //carStartBreakingDistance = originalBreakingDistance;
        carTarget = null;
        shouldBrakeBeforeCar = false;
        targetPriorityBehavior = null;
        targetPathFollower = null;
    }
    public void EnableTarget(Transform _target, PathFollower _targetPathFollower)
    {
        carTarget = _target;
        shouldBrakeBeforeCar = true;
        targetPriorityBehavior = _targetPathFollower.priorityBehavior;
        if (_targetPathFollower == null || targetPriorityBehavior == null)
        {
            Debug.LogError("ERROR EN ENABLETARGET");
        }
        targetPathFollower = _targetPathFollower;
    }
    IEnumerator ResumeTheCar()
    {
        reactingToCarInFront = true;
        float reactionTime = Random.Range(0.3f, 0.8f);
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

public enum LaneSide
{
    None = -1,
    Right,
    Left
}
public enum PriorityLevel
{
    Stop,
    Yield,
    Roundabout,
    Max
}

public enum TypeOfCar
{
    Delivery,
    Sedan,
    SedanSport,
    Suv,
    SuvLuxury,
    Truck,
    Van
}
