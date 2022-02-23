using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    const float minPathUpdateTime = .2f;
    const float pathUpdateMoveThreshold = .5f;

    public Transform target;
    public float speed = 20;
    public float turnSpeed = 3;
    public float turnDst = 5;
    public float stoppingDst = 10;
    public int pathIndex = 0;
    Path path;
    bool followingPath;
    bool shouldStopAtTrafficLight = false;

    TrafficLightCarController trafficLightCarController;

    void Start()
    {
        StartCoroutine(UpdatePath());
        trafficLightCarController = GetComponent<TrafficLightCarController>();
    }

    public void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
    {
        if (pathSuccessful)
        {
            path = new Path(waypoints, transform.position, turnDst, stoppingDst);

            trafficLightCarController.path = path;
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

        float speedPercent = 1;

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

                // Aquí hay que implementar una lógica alternativa, debo tener un control que me permita ordenar al coche a pararse
                if (shouldStopAtTrafficLight)
                {
                    speedPercent = StopAtPoint(pos2D);
                    Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                    if (speedPercent > 0.1f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                    transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
                }
                else
                {
                    speedPercent = CheckToStop(pos2D);
                    Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                    if (speedPercent > 0.1f) transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                    transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
                }

                
            }

            yield return null;

        }
    }

    float CheckToStop(Vector2 pos2D)
    {
        float speedPercent = 1;
        // Aquí hay que implementar una lógica alternativa, debo tener un control que me permita ordenar al coche a pararse
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

    float StopAtPoint(Vector2 pos2D)
    {
        float speedPercent = 1;
        // Hay que cambiar la comprobación
        if (pathIndex > path.slowDownIndex && stoppingDst > 0)
        {
            speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);
            if (speedPercent < 0.01f)
            {
                Debug.Log("STOPPED AT TRAFFIC LIGHT");
            }
        }
        return speedPercent;
    }

    public void OnDrawGizmos()
    {
        if (path != null)
        {
            path.DrawWithGizmos();
        }
    }
}
