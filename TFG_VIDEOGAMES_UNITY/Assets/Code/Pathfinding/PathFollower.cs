using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;

    public Transform target;
    public float speed = 4;
    public float turnSpeed = 4;
    public float turnDst = 5;
    public float stoppingDst = 10;
    public int pathIndex = 0;
    Path path;
    [SerializeField] float speedPercent = 0f;

    // Stop at traffic light variables
    public bool shouldStopAtTrafficLight = false;
    private float trafficLightStopDist = 3;
    private bool vehicleWasStopped = false;
    private bool vehicleWasStoppedByTraffic = false;
    private bool driverHasReacted = false;

    // Car collision avoidance variables
    public bool shouldBrakeBeforeCar = false;
    private float carStopDistance = 2;
    public Vector3 frontCarPos;
    CarObstacleAvoidance carObstacleAvoidance;
    int recentAddedAvoidancePosIndex = -1;

    TrafficLightCarController trafficLightCarController;
    [SerializeField] bool visualDebug;
    List<Vector3> waypointsList = new List<Vector3>();
    [SerializeField] WorldGrid grid;

    private IEnumerator followPathCoroutine;

    void Start()
    {
        StartCoroutine(UpdatePath());
        trafficLightCarController = GetComponent<TrafficLightCarController>();
        carObstacleAvoidance = GetComponent<CarObstacleAvoidance>();
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            waypointsList = new List<Vector3>();
            foreach (Vector3 waypoint in waypoints)
            {
                waypointsList.Add(waypoint);
            }
            if(followPathCoroutine != null)
                StopCoroutine(followPathCoroutine);
            path = new Path(waypointsList, transform.position, turnDst, stoppingDst);
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
        PathfinderRequestManager.RequestPath(transform.position, target.position, transform.forward, OnPathFound);

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < 8f)
            {
                float newDistance = 0f;
                Vector3 newTargetPos = Vector3.zero;
                Vector3 oldPos = target.position;
                while (newDistance < 20f)
                {
                    newTargetPos = grid.GetRandomPosInRoads();
                    newDistance = Vector3.Distance(oldPos, newTargetPos);
                }
                PathfinderRequestManager.RequestPath(transform.position, newTargetPos, transform.forward, OnPathFound);
                target.position = newTargetPos;
            }
        }
    }

    // EL problema es que no está encontrando un camino valido a veces, probablemente porque no se encuentre un nodo valido? Pintar nodo start y nodo end en el mundo.
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
                    carObstacleAvoidance.objectHit = false;
                }
                pathIndex++;
                if(pathIndex >= path.turnBoundaries.Count)
                {
                    Debug.Log("QUE SE ROMPE EL CODIGO JAJA");
                }
            }

            if (shouldBrakeBeforeCar && !vehicleWasStoppedByTraffic)
            {
                speedPercent = StopBeforeCar();
                driverHasReacted = true;
            }
            else if (shouldStopAtTrafficLight)
            {
                speedPercent = StopAtTrafficLight();
                driverHasReacted = true;
            }
            else
            {
                if (driverHasReacted)
                {
                    float reactionTime = Random.Range(0.2f, 0.7f);
                    yield return new WaitForSeconds(reactionTime);
                    driverHasReacted = false;
                }

                // When the car is stopped, set the speedPercent to 0 so that it accelerates from 0 and not instantly.
                if (vehicleWasStopped)
                {
                    speedPercent = 0f;
                    vehicleWasStopped = false;
                    if (vehicleWasStoppedByTraffic)
                        vehicleWasStoppedByTraffic = true;
                }

                speedPercent += 0.002f;
                speedPercent = Mathf.Clamp(speedPercent, 0f, 1f);

            }
            Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
            if (speedPercent > 0.1f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            transform.Translate(Vector3.forward * speed * Time.deltaTime * speedPercent, Space.Self);

            yield return null;

        }
    }
    public void SetTrafficLightPos(Vector3 _trafficLightPos)
    {
        shouldStopAtTrafficLight = true;
    }

    public void SetNewPathByAvoidance(Vector3 newPos)
    {
        //Create a new Path and add this position in the correct index.
        waypointsList.Insert(pathIndex, newPos);
        recentAddedAvoidancePosIndex = pathIndex;
        path = new Path(waypointsList, transform.position, turnDst, stoppingDst);
    }

    float StopBeforeCar()
    {
        float speedPercent;

        float distance = Vector3.Distance(transform.position, frontCarPos);
        speedPercent = Mathf.Clamp01((distance - 1f) / carStopDistance);
        if (speedPercent < 0.03f)
        {
            speedPercent = 0f;
            vehicleWasStopped = true;
        }

        return speedPercent;
    }
    float StopAtTrafficLight()
    {
        float speedPercent = 1;
        // Hay que cambiar la comprobación
        if (trafficLightStopDist > 0)
        {
            float distance = trafficLightCarController.GiveDistanceToPathFollower();
            speedPercent = Mathf.Clamp01((distance - 1.5f) / trafficLightStopDist);
            if (speedPercent < 0.03f)
            {
                speedPercent = 0f;
                vehicleWasStoppedByTraffic = true;
                vehicleWasStopped = true;
            }
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
