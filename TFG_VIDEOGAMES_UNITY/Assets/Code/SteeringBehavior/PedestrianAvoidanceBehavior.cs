using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianAvoidanceBehavior
{
    private PathFollower pathFollower;
    private Transform transform;
    private bool visualDebug;
    Vector3 rayOrigin;
    private List<Pedestrian> relevantPedestrians = new List<Pedestrian>();
    public PedestrianAvoidanceBehavior(PathFollower _pathFollower)
    {
        pathFollower = _pathFollower;
    }


    // Update is called once per frame
    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        // Think about if avoiding an obstacle you come too close with a car.
        visualDebug = _visualDebug;
        transform = _transform;
        rayOrigin = _rayOrigin;

        if(relevantPedestrians.Count > 0) ProcessRelevantPedestrians();
    }

    public void ProcessPedestrianHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        Pedestrian pedestrian = hit.transform.gameObject.GetComponent<Pedestrian>();
        if (pedestrian == null)
            return;

        if (pedestrian.isCrossing && !relevantPedestrians.Contains(pedestrian))
            relevantPedestrians.Add(pedestrian);
    }

    void ProcessRelevantPedestrians()
    {
        List<Pedestrian> pedestriansToRemove = new List<Pedestrian>();
        foreach(Pedestrian pedestrian in relevantPedestrians)
        {
            if (!pedestrian.isCrossing || AngleNotRelevant(pedestrian))
                pedestriansToRemove.Add(pedestrian);
        }

        foreach (Pedestrian pedestrian in pedestriansToRemove)
            relevantPedestrians.Remove(pedestrian);

        if(relevantPedestrians.Count > 0)
        {
            // STOP THE CAR
            pathFollower.shouldStopPedestrian = true;
            pathFollower.pedestrianStopPos = relevantPedestrians[0].crossingPos;
        }
        else
        {
            // RESUME THE CAR
            pathFollower.shouldStopPedestrian = false;
            pathFollower.pedestrianStopPos = Vector3.zero;

        }
    }
    private bool AngleNotRelevant(Pedestrian pedestrian)
    {
        Vector3 dirToPedestrian = (pedestrian.transform.position - transform.position).normalized;
        Vector3 carForward = transform.forward.normalized;

        float angleToPedestrian = Vector3.SignedAngle(carForward, dirToPedestrian, Vector3.up);
        Vector3 pedestrianForward = pedestrian.transform.forward;
        float threshold = .7f;
        // TO FIX -- REVISARLO

        // ANGULOS ANGULOS FORWARD, LOS ABSOLUTOS NO FUNCIONAN

        if (carForward.x > threshold) // Car is going right
        {
            if(angleToPedestrian > 0) // Pedestrian to the right
            {
                if(pedestrianForward.z < threshold)
                {
                    return true;
                }
                return false;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.z > threshold)
                {
                    return true;
                }
                return false;
            }
        }
        else if(carForward.x < -threshold) // Car is going left
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.z < threshold)
                {
                    return false;
                }
                return true;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.z > threshold)
                {
                    return false;
                }
                return true;
            }
        }
        else if (carForward.z > threshold) // Car is going up
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.x > threshold)
                {
                    return true;
                }
                return false;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.x < threshold)
                {
                    return true;
                }
                return false;
            }
        }
        else if (carForward.z < -threshold)
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.x < threshold)
                {
                    return true;
                }
                return false;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.x > threshold)
                {
                    return true;
                }
                return false;
            }
        } // Car is going down

        return false;
    }
}
