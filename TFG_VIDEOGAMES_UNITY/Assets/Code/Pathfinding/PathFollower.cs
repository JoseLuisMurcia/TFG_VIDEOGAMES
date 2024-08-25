using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;

    [Header("Specs")]
    [SerializeField] private TypeOfCar typeOfCar;
    [SerializeField] public float speed;
    [SerializeField] float turnSpeed;
    [SerializeField] float turnDst;
    [HideInInspector] public int pathIndex = 0;
    public Path path = null;
    [SerializeField] public float speedPercent = 0f;
    private float previousSpeedPercent = 0f;
    public float movementSpeed = 0f;

    // Stop at traffic light variables
    [Header("TrafficLight")]
    public bool shouldStopAtTrafficLight = false;
    private float carStartBrakingDistanceTrafficLight = 5f;
    private float carStopDistanceTrafficLight = 1.5f;
    [HideInInspector] TrafficLightCarController trafficLightCarController;

    // Car collision avoidance variables
    [Header("CarAvoidance")]
    public bool reactingToCarInFront = false;
    public bool shouldBrakeBeforeCar = false;
    [SerializeField] float carStartBrakingDistance;
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
    [HideInInspector] private float carStartBrakingDistancePriority;
    [HideInInspector] private float carStopDistancePriority;
    float carStopDistancePriorityRoundabout = 1f;
    float carStartBrakingDistancePriorityRoundabout = 5f;


    // Pedestrian variables
    [Header("Pedestrian")]
    public bool shouldStopPedestrian = false;
    float carStartBrakingDistancePedestrian;
    float carStopDistancePedestrian;
    public Vector3 pedestrianStopPos;

    [Header("Others")]
    [SerializeField] bool pathDebug;
    public bool isFullyStopped = true;
    [HideInInspector] List<Vector3> waypointsList = new List<Vector3>();
    [HideInInspector] public List<Node> nodeList = new List<Node>();

    [Header("Reactions")]
    public bool reactionDelay = false;
    public bool adjustingDistance = false;
    public bool cooldown = false;

    [Header("Overtake")]
    public bool roadValidForOvertaking;
    public LaneSide laneSide = LaneSide.None;
    public bool overtaking = false;
    public OvertakeBehavior overtakeBehavior;

    private IEnumerator followPathCoroutine;
    private IEnumerator reactionTimeCoroutine;
    private static float minDistanceToSpawnNewTarget = 18f;
    float accelerationRate = 0.2f;
    float decelerationRate = 100f;

    private bool isProcedural = false;
    private void SetSpecsForTypeCar()
    {
        float speedMultiplier;
        float turnExtraSpeed = .3f;
        switch (typeOfCar)
        {
            case TypeOfCar.Delivery:
                carStartBrakingDistance = Random.Range(2.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4.5f, 6.5f);
                carStartBrakingDistancePriority = Random.Range(3, 5f);
                carStartBrakingDistancePedestrian = Random.Range(4.2f, 5.5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(0.8f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.Sedan:
                carStartBrakingDistance = Random.Range(1.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(3, 4f);
                carStartBrakingDistancePedestrian = Random.Range(4f, 5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(1f, 1.5f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.SedanSport:
                carStartBrakingDistance = Random.Range(1.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(3f, 5f);
                carStartBrakingDistancePedestrian = Random.Range(4f, 5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(.5f, 1f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(1.3f, 1.7f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.Suv:
                carStartBrakingDistance = Random.Range(1.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(3f, 4f);
                carStartBrakingDistancePedestrian = Random.Range(4.5f, 5.5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(1f, 1.3f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.SuvLuxury:
                carStartBrakingDistance = Random.Range(1.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(3f, 4f);
                carStartBrakingDistancePedestrian = Random.Range(4f, 5f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(1.2f, 1.6f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.Truck:
                carStartBrakingDistance = Random.Range(2.5f, 5f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(4f, 5.5f);
                carStartBrakingDistancePedestrian = Random.Range(5f, 6.5f);
                carStopDistance = Random.Range(1.3f, 1.7f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1.3f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(0.75f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            case TypeOfCar.Van:
                carStartBrakingDistance = Random.Range(1.5f, 4f);
                carStartBrakingDistanceTrafficLight = Random.Range(4f, 6f);
                carStartBrakingDistancePriority = Random.Range(3f, 4.5f);
                carStartBrakingDistancePedestrian = Random.Range(4.2f, 6f);
                carStopDistance = Random.Range(1.35f, 1.65f);
                carStopDistanceTrafficLight = Random.Range(1.35f, 1.65f);
                carStopDistancePriority = Random.Range(1f, 1.5f);
                carStopDistancePedestrian = Random.Range(3f, 4f);
                speedMultiplier = Random.Range(0.8f, 1.1f);
                speed *= speedMultiplier;
                turnSpeed *= speedMultiplier + turnExtraSpeed;
                break;
            default:
                Debug.Log("haha wtf bro");
                speed = 3f;
                turnSpeed = 3.2f;
                turnDst = 0.5f;
                break;
        }

    }
    private void Awake()
    {
        isProcedural = WorldGrid.Instance == null && RoadConnecter.Instance != null;
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
        if (path == null && !pathRequested)
        {
            //Debug.LogWarning("CAR WAS SPAWNED AND IT DID NOT HAVE A PATH WTF");
            Node targetNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads();
            float newDistance = Vector3.Distance(targetNode.worldPosition, transform.position);
            while (newDistance < minDistanceToSpawnNewTarget)
            {
                targetNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
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
            newNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
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

            if(Vector3.Distance(_startNode.worldPosition, transform.position) > 27f)
            {
                Debug.LogWarning("ALGO SE HA ROTO");

                GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                startSphere.name = "NewStartNode";
                startSphere.transform.parent = transform.parent;
                startSphere.transform.position = _startNode.worldPosition + Vector3.up * 2.5f;
                startSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.magenta);

                GameObject endSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                endSphere.name = "NewEndNode";
                endSphere.transform.parent = transform.parent;
                endSphere.transform.position = _endNode.worldPosition + Vector3.up * 2.5f;
                endSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);

                GameObject previousEndSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                previousEndSphere.name = "PreviousEndNode";
                previousEndSphere.transform.parent = transform.parent;
                previousEndSphere.transform.position = endNode.worldPosition + Vector3.up * 2.5f;
                previousEndSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);

                GameObject currentPosSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentPosSphere.name = "CurrentPosSphere";
                currentPosSphere.transform.parent = transform.parent;
                currentPosSphere.transform.position = transform.position + Vector3.up * 2.5f;
                currentPosSphere.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
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
            endNode = _endNode;
            nodeList = pathfindingResult.nodes;
            waypointsList.Clear();
            waypointsList = pathfindingResult.pathPositions;

            if (followPathCoroutine != null)
                StopCoroutine(followPathCoroutine);
            path = new Path(waypointsList, transform.position, turnDst);
            followPathCoroutine = FollowPath();
            pathRequested = false;
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
    IEnumerator UpdatePath()
    {
        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);
            if (path != null)
            {
                int numNodesInPath = path.lookPoints.Count;
                if (pathIndex >= numNodesInPath - 6 && !pathRequested)
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
        if (endNode == null || pathRequested)
            return;

        float newDistance = 0f;
        Vector3 newTargetPos = Vector3.zero;
        Node newNode = null;
        while (newDistance < minDistanceToSpawnNewTarget)
        {
            newNode = isProcedural ? RoadConnecter.Instance.GetRandomNodeInRoads() : WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
            newTargetPos = newNode.worldPosition;
            newDistance = Vector3.Distance(endNode.worldPosition, newTargetPos);
        }

        PathfinderRequestManager.RequestPath(endNode, newNode, transform.forward, OnPathFound);
        pathRequested = true;
    }
    public void RequestLaneSwap()
    {
        if (pathRequested)
            return;

        PathfinderRequestManager.RequestLaneSwap(nodeList[pathIndex], OnLaneSwap);
        Debug.LogWarning("LANE SWAP REQUESTED");
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

            if (shouldStopPedestrian && !TargetIsStoppingBeforePedestrians())
            {
                speedPercent = SlowSpeedPedestrian();
            }
            else if (shouldStopAtTrafficLight && !TargetIsStoppingBeforeTL())
            {
                speedPercent = SlowSpeedAtTrafficLight();
            }
            else if (shouldStopPriority && !TargetIsStoppingBeforePriority())
            {
                speedPercent = SlowSpeedPriority();
            }
            else if (shouldBrakeBeforeCar)
            {
                speedPercent = SlowSpeedBeforeCar();
            } 
            else
            {
                speedPercent = Mathf.Min(1f, speedPercent + accelerationRate * Time.deltaTime);
            }
            if (speedPercent > 0.003f) isFullyStopped = false;

            if (speedPercent - previousSpeedPercent > 0.1f)
            {
                speedPercent = previousSpeedPercent += 0.001f;
                speedPercent = Mathf.Clamp01(speedPercent);
            }
            
            previousSpeedPercent = speedPercent;
            
            Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
            if (speedPercent > 0.03f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            transform.Translate(Vector3.forward * speed * Time.deltaTime * speedPercent, Space.Self);
            movementSpeed = speed * speedPercent;
            yield return null;

        }
    }
    public void StopAtTrafficLight(bool subscription)
    {
        if (shouldStopAtTrafficLight == false)
        {
            if (trafficLightCarController.trafficLight == null)
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
    #region SlowCarMethods
    float SlowSpeedPedestrian()
    {
        float distance = Vector3.Distance(transform.position, pedestrianStopPos);

        float brakingDistance = Mathf.Clamp01((distance - carStopDistancePedestrian) / carStartBrakingDistancePedestrian);
        float _speedPercent = Mathf.Lerp(speedPercent, brakingDistance, decelerationRate * Time.deltaTime);
        if (_speedPercent < 0.03f)
        {
            StopTheCar();
        }
        return _speedPercent;
    }
    float SlowSpeedPriority()
    {
        float distance = Vector3.Distance(transform.position, stopPosition);
        float brakingDistance;
        
        if (priorityLevel == PriorityLevel.Roundabout)
        {
            brakingDistance = Mathf.Clamp01((distance - carStopDistancePriorityRoundabout) / carStartBrakingDistancePriorityRoundabout);
        }
        else
        {
            brakingDistance = Mathf.Clamp01((distance - carStopDistancePriority) / carStartBrakingDistancePriority);
        }
        float _speedPercent = Mathf.Lerp(speedPercent, brakingDistance, decelerationRate * Time.deltaTime);

        if (speedPercent - _speedPercent > 0.5f)
            _speedPercent = speedPercent - 0.01f;

        if (_speedPercent < 0.03f)
        {
            StopTheCar();
        }
        return _speedPercent;
    }
    float SlowSpeedBeforeCar()
    {
        if (reactionDelay)
            return speedPercent;

        float distance = Vector3.Distance(transform.position, carTarget.position);
        float brakingDistance = Mathf.Clamp01((distance - carStopDistance) / carStartBrakingDistance);
        float _speedPercent = Mathf.Lerp(speedPercent, brakingDistance, decelerationRate * Time.deltaTime);

        //if (laneSide == LaneSide.Right && speed - targetPathFollower.speed > 0.3f && overtakeBehavior.canSwapLane && !overtaking)
        //{
        //    RequestLaneSwap();
        //    //SpawnSpheres(transform.position, targetPathFollower.transform.position);
        //    overtaking = true;
        //    avoidanceBehavior.AddCarToBlacklist(targetPathFollower);
        //    overtakeBehavior.overtakenCar = targetPathFollower;
        //    avoidanceBehavior.UnableTarget();
        //    return speedPercent;
        //}

        //if (!adjustingDistance && targetPathFollower.movementSpeed - movementSpeed < 0.05f && !isFullyStopped && !cooldown && !targetPathFollower.isFullyStopped)
        //{
        //    StartCoroutine(AdjustDistance());
        //}

        if (isFullyStopped && _speedPercent > 0.01f) // The car is fully stopped and the car in front is resuming the car
        {
            if (!reactingToCarInFront)
            {
                StartCoroutine(ResumeTheCar());
            }
            return 0f;
        }

        if (_speedPercent < 0.05f && !reactingToCarInFront) // Set the car to fully stopped
        {
            StopTheCar();
        }

        return _speedPercent;
    }
    float SlowSpeedAtTrafficLight()
    {
        float distance = trafficLightCarController.GiveDistanceToPathFollower();
        float _speedPercent = Mathf.Clamp01((distance - carStopDistanceTrafficLight) / carStartBrakingDistanceTrafficLight);
        if (_speedPercent < 0.04f)
        {
            StopTheCar();
        }
        return _speedPercent;
    }
    #endregion

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
            float distanceResult = carStartBrakingDistance - increaseValue * numIterations;
            if (distanceResult < minRange)
                randomInt = 1;
        }
        else
        {
            float distanceResult = carStartBrakingDistance + increaseValue * numIterations;
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
                carStartBrakingDistance -= increaseValue;
            }
            else
            {
                // Get further from the car in front
                carStartBrakingDistance += increaseValue;
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
    private void StopTheCar()
    {
        speedPercent = 0f;
        isFullyStopped = true;
    }
    public bool TargetIsStoppingBeforePedestrians()
    {
        if (carTarget == null || !targetPathFollower.shouldStopPedestrian) return false;

        return true;
    }
    private bool TargetIsStoppingBeforeTL()
    {
        if (carTarget == null || !targetPathFollower.shouldStopAtTrafficLight) return false;

        return true;
    }
    private bool TargetIsStoppingBeforePriority()
    {
        if (carTarget == null || !targetPathFollower.shouldStopPriority) return false;

        return true;
    }
    public void OnDrawGizmos()
    {
        if (isFullyStopped)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 4f, .4f);
        }
        if (pathDebug && path != null)
        {
            path.DrawWithGizmos();
        }
    }
    public Road FindPreviousRoadOnPath()
    {
        Road currentRoad = nodeList[pathIndex].road;
        int i = pathIndex - 1;
        while (i > 0)
        {
            Road previousRoad = nodeList[i].road;
            if (previousRoad != currentRoad) return previousRoad;
            i--;
        }
        return null;
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
    Roundabout,
    Stop,
    Yield,
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
