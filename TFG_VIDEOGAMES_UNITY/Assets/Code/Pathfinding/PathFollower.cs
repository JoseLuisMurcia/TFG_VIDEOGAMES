using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;

    private Vector3 target;
    [SerializeField] float speed = 4;
    [SerializeField] float turnSpeed = 4;
    [SerializeField] float turnDst = 3;
    public int pathIndex = 0;
    Path path;
    [SerializeField] float speedPercent = 0f;

    // Stop at traffic light variables
    public bool shouldStopAtTrafficLight = false;
    private float trafficLightStopDist = 5;

    // Car collision avoidance variables
    public bool shouldBrakeBeforeCar = false;
    [SerializeField] float carStartBreakingDistance = 2f;
    [SerializeField] float carStopDistance = 1f;
    public Vector3 frontCarPos;
    public Transform carTarget;
    [HideInInspector] public AvoidanceBehavior avoidanceBehavior;
    int recentAddedAvoidancePosIndex = -50;
    Node endNode;

    // Priority variables
    public bool pathRequested = false;
    public bool shouldStopPriority = false;
    public Vector3 stopPosition = Vector3.zero;

    [HideInInspector] TrafficLightCarController trafficLightCarController;
    [SerializeField] bool visualDebug;
    [HideInInspector] List<Vector3> waypointsList = new List<Vector3>();

    private IEnumerator followPathCoroutine;
    private IEnumerator reactionTimeCoroutine;
    private static float minDistanceToSpawnNewTarget = 20f;

    public PriorityLevel priorityLevel = PriorityLevel.Max;

    // Falla si el objetivo se consigue dentro de una interseccion ya que al mandar una peticion de adquirir un nodo en la interseccion , se es incapaz.
    void Start()
    {
        carStartBreakingDistance = Random.Range(1f, 4f);
        carStartBreakingDistance = 2.5f;
        trafficLightStopDist = Random.Range(4f, 6f);
        carStopDistance = Random.Range(0.5f, 2f);
        carStopDistance = 1.5f;
        float speedMultiplier = Random.Range(1f, 1.01f);
        speed *= speedMultiplier;
        turnSpeed *= speedMultiplier;
        StartCoroutine(UpdatePath());
        trafficLightCarController = GetComponent<TrafficLightCarController>();
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful, Node _startNode, Node _endNode)
    {
        if (pathSuccessful)
        {
            pathRequested = false;
            endNode = _endNode;
            waypointsList = new List<Vector3>();
            foreach (Vector3 waypoint in waypoints)
            {
                waypointsList.Add(waypoint);
            }
            if (followPathCoroutine != null)
                StopCoroutine(followPathCoroutine);
            path = new Path(waypointsList, transform.position, turnDst);
            followPathCoroutine = FollowPath();
            StartCoroutine(followPathCoroutine);
        }
        else
        {
            Debug.Log("Va a haber problemas");
        }
    }

    IEnumerator UpdatePath()
    {

        if (Time.timeSinceLevelLoad < .3f)
        {
            yield return new WaitForSeconds(.3f);
        }
        if (target == Vector3.zero)
        {
            target = WorldGrid.Instance.GetRandomNodeInRoads().worldPosition;
        }
        PathfinderRequestManager.RequestPath(transform.position, target, transform.forward, OnPathFound);

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);

            float distance = Vector3.Distance(transform.position, target);
            if (distance < 3f)
            {
                RequestNewPath();
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

    public float GetAngleBetweenCurrentNodeAndNumNodes(int numNodes)
    {
        if (PathEnds(numNodes))
            return Mathf.Infinity;

        Vector3 dirFromCurrentNodeToTarget = (waypointsList[pathIndex+numNodes] - waypointsList[pathIndex]).normalized;
        Vector3 currentNodeForward = (waypointsList[pathIndex+1] - waypointsList[pathIndex]).normalized;
        float angle = Vector3.Angle(currentNodeForward, dirFromCurrentNodeToTarget);
        return angle;
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
        float newDistance = 0f;
        Vector3 newTargetPos = Vector3.zero;
        Vector3 oldPos = target;
        Node newNode = null;
        while (newDistance < minDistanceToSpawnNewTarget)
        {
            newNode = WorldGrid.Instance.GetRandomNodeInRoads(); // This will be the endNode
            newTargetPos = newNode.worldPosition;
            newDistance = Vector3.Distance(oldPos, newTargetPos);
        }

        PathfinderRequestManager.RequestPath(endNode, newNode, transform.forward, OnPathFound);
        target = newTargetPos;
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

            if (shouldStopPriority)
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

            if(reactionTimeCoroutine != null)
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

    float SlowSpeedPriority()
    {
        float speedPercent;
        float distance = Vector3.Distance(transform.position, stopPosition);
        speedPercent = Mathf.Clamp01((distance - 1f) / carStartBreakingDistance);
        if (speedPercent < 0.03f)
        {
            speedPercent = 0f;
        }
        return speedPercent;
    }

    float SlowSpeedBeforeCar()
    {
        float speedPercent;

        //float distance = Vector3.Distance(transform.position, frontCarPos);
        float distance = Vector3.Distance(transform.position, carTarget.position);
        speedPercent = Mathf.Clamp01((distance - carStopDistance) / carStartBreakingDistance);
        if (speedPercent < 0.03f)
        {
            speedPercent = 0f;
        }

        return speedPercent;
    }
    float SlowSpeedAtTrafficLight()
    {
        float distance = trafficLightCarController.GiveDistanceToPathFollower();
        speedPercent = Mathf.Clamp01((distance - 1.5f) / trafficLightStopDist);
        if (speedPercent < 0.03f)
        {
            speedPercent = 0f;
        }
        return speedPercent;
    }

    public void OnDrawGizmos()
    {
        if (visualDebug && path != null)
        {
            path.DrawWithGizmos();
        }
    }
}


public enum PriorityLevel
{
    Stop,
    Roundabout,
    Yield,
    Max
}
