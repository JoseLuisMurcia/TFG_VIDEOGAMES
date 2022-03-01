using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;
    const float pathUpdateMoveThreshold = .5f;

    public Transform target;
    public float speed = 4;
    public float turnSpeed = 4;
    public float turnDst = 5;
    public float stoppingDst = 10;
    public int pathIndex = 0;
    Path path;
    bool followingPath;

    public bool shouldStopAtTrafficLight = false;
    private Vector3 trafficLightPos;
    private float trafficLightStopDist = 3;
    private bool vehicleWasStopped = false;

    TrafficLightCarController trafficLightCarController;
    [SerializeField] bool visualDebug;
    List<Vector3> waypointsList = new List<Vector3>();

    void Start()
    {
        StartCoroutine(UpdatePath());
        trafficLightCarController = GetComponent<TrafficLightCarController>();
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            foreach(Vector3 waypoint in waypoints)
            {
                waypointsList.Add(waypoint);
            }
            path = new Path(waypointsList, transform.position, turnDst, stoppingDst);

            //trafficLightCarController.path = path;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath()
    {

        if (Time.timeSinceLevelLoad < .3f)
        {
            yield return new WaitForSeconds(.3f);
        }
        PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);

        float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
        Vector3 targetPosOld = target.position;

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);
            if ((target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold)
            {
                PathfinderRequestManager.RequestPath(transform.position, target.position, OnPathFound);
                targetPosOld = target.position;
            }
        }
    }

    IEnumerator FollowPath()
    {

        followingPath = true;
        pathIndex = 0;
        // Rotate to look at the first point
        transform.LookAt(path.lookPoints[0]);

        float speedPercent = 0;

        // Check all the time if the unity has passed the boundari
        while (followingPath)
        {
            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
            while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
            {
                if (pathIndex == path.finishLineIndex)
                {
                    Debug.Log("END ARRIVED");
                    followingPath = false;
                    break;
                }
                else
                {
                    // Go to next point
                    pathIndex++;
                }
            }

            if (followingPath)
            {

                if (shouldStopAtTrafficLight)
                {
                    speedPercent = StopAtTrafficLight();
                }
                else
                {
                    // When the car is stopped, set the speedPercent to 0 so that it accelerates from 0 and not instantly.
                    if (vehicleWasStopped)
                    {
                        speedPercent = 0f;
                        vehicleWasStopped = false;
                    }

                    // When the car is close enough to the path objective.
                    if (pathIndex > path.slowDownIndex && stoppingDst > 0)
                    {
                        speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
                        if (speedPercent < 0.01f)
                        {
                            Debug.Log("END ARRIVED V2");
                            followingPath = false;
                        }
                    }
                    else
                    {
                        speedPercent += 0.002f;
                        speedPercent = Mathf.Clamp(speedPercent, 0f, 1f);
                    }
                }
                Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                if (speedPercent > 0.1f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * speed * Time.deltaTime * speedPercent, Space.Self);


            }

            yield return null;

        }
    }
    public void SetTrafficLightPos(Vector3 _trafficLightPos)
    {
        trafficLightPos = _trafficLightPos;
        shouldStopAtTrafficLight = true;
    }

    public void SetNewPathByAvoidance(Vector3 newPos)
    {
        //Create a new Path and add this position in the correct index.
        waypointsList.Insert(pathIndex, newPos);
        path = new Path(waypointsList, transform.position, turnDst, stoppingDst);
    }


    float StopAtTrafficLight()
    {
        float speedPercent = 1;
        // Hay que cambiar la comprobación
        if (trafficLightStopDist > 0)
        {
            float distance = trafficLightCarController.GiveDistanceToPathFollower();
            speedPercent = Mathf.Clamp01((distance-1.5f) / trafficLightStopDist);
            if (speedPercent < 0.03f)
            {
                speedPercent = 0f;
                vehicleWasStopped = true;
            }
        }
        return speedPercent;
    }

    float CheckToStop(Vector2 pos2D)
    {
        float speedPercent = 1;
        if (pathIndex > path.slowDownIndex && stoppingDst > 0)
        {
            speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
            if (speedPercent < 0.01f)
            {
                Debug.Log("END ARRIVED V2");
                followingPath = false;
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
