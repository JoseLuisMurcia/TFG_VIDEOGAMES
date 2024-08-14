using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianAvoidanceBehavior
{
    private PathFollower pathFollower;
    private Transform transform;
    private bool visualDebug;
    Vector3 rayOrigin;
    private List<Pedestrian> crossingPedestrians = new List<Pedestrian>();
    private List<Pedestrian> notCrossingPedestrians = new List<Pedestrian>();
    private bool shouldStopCrossingPedestrians = false;
    private bool shouldStopNotCrossingPedestrians = false;
    private Vector3 crossingPos = Vector3.zero;

    public PedestrianAvoidanceBehavior(PathFollower _pathFollower)
    {
        pathFollower = _pathFollower;
    }

    public void ResetStructures()
    {
        crossingPedestrians.Clear();
        notCrossingPedestrians.Clear();
    }
    // Update is called once per frame
    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        // Think about if avoiding an obstacle you come too close with a car.
        visualDebug = _visualDebug;
        transform = _transform;
        rayOrigin = _rayOrigin;

        if(crossingPedestrians.Count > 0) ProcessCrossingPedestrians();
        if(notCrossingPedestrians.Count > 0) ProcessNotCrossingPedestrians();

        Debug.Log("shouldStopCrossingPedestrians: " + shouldStopCrossingPedestrians + ". shouldStopNotCrossingPedestrians: " + shouldStopNotCrossingPedestrians);

        if (shouldStopCrossingPedestrians || shouldStopNotCrossingPedestrians)
        {
            // STOP THE CAR
            pathFollower.shouldStopPedestrian = true;
            pathFollower.pedestrianStopPos = crossingPos;
        }
        else
        {
            pathFollower.shouldStopPedestrian = false;
            pathFollower.pedestrianStopPos = Vector3.zero;
        }
    }
    public void ProcessPedestrianHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        Pedestrian pedestrian = hit.transform.gameObject.GetComponent<Pedestrian>();
        if (pedestrian == null) // || Pedestrian no va a meterse realmente) 
            return;

        if (pedestrian.isCrossing && !crossingPedestrians.Contains(pedestrian))
        {
            crossingPedestrians.Add(pedestrian);
        }
        if (!pedestrian.isCrossing && !notCrossingPedestrians.Contains(pedestrian))
        {
            notCrossingPedestrians.Add(pedestrian);
        }
    }
    void ProcessCrossingPedestrians()
    {
        List<Pedestrian> pedestriansToRemove = new List<Pedestrian>();
        foreach(Pedestrian pedestrian in crossingPedestrians)
        {
            float carDistToPed = Vector3.Distance(transform.position, pedestrian.transform.position);
            float pedDistToCross = Vector3.Distance(pedestrian.transform.position, crossingPos);
            float carDistToCross = Vector3.Distance(transform.position, crossingPos);

            //Debug.Log("pedDistToCross: " + pedDistToCross + ", carDistToCross: " + carDistToCross + ", carDistToPed: " + carDistToPed);
            if (!AngleRelevant(pedestrian, false) || 
                (carDistToCross < 2.5f && carDistToPed > 3f && pathFollower.speedPercent > 0.65f) 
                || !pedestrian.isCrossing)
            {
                pedestriansToRemove.Add(pedestrian);
            }
        }

        foreach (Pedestrian pedestrian in pedestriansToRemove)
            crossingPedestrians.Remove(pedestrian);

        shouldStopCrossingPedestrians = crossingPedestrians.Count > 0 ? true : false;
    }
    void ProcessNotCrossingPedestrians()
    {
        List<Pedestrian> pedestriansToRemove = new List<Pedestrian>();
        foreach (Pedestrian pedestrian in notCrossingPedestrians)
        {
            float carDistToPed = Vector3.Distance(transform.position, pedestrian.transform.position);
            float pedDistToCross = Vector3.Distance(pedestrian.transform.position, crossingPos);
            float carDistToCross = Vector3.Distance(transform.position, crossingPos);

            //Debug.Log("pedDistToCross: " + pedDistToCross + ", carDistToCross: " + carDistToCross + ", carDistToPed: " + carDistToPed);
            if (!(AngleRelevant(pedestrian, true) && pedDistToCross <= 3.5f) ||
                (carDistToCross < 3.5f && carDistToPed > 4f && pathFollower.speedPercent > 0.6f)
                || pedestrian.isCrossing || pedestrian.hasCrossed)
            {
                pedestriansToRemove.Add(pedestrian);
            }
        }

        foreach (Pedestrian pedestrian in pedestriansToRemove)
            notCrossingPedestrians.Remove(pedestrian);

        shouldStopNotCrossingPedestrians = notCrossingPedestrians.Count > 0 ? true : false;
    }
    private bool AngleRelevant(Pedestrian pedestrian, bool softThreshold)
    {
        // If true, the car should stop
        Vector3 dirToPedestrian = (pedestrian.transform.position - transform.position).normalized;
        Vector3 carForward = transform.forward.normalized;

        float angleToPedestrian = Vector3.SignedAngle(carForward, dirToPedestrian, Vector3.up);
        Vector3 pedestrianForward = pedestrian.transform.forward;
        float pedThreshold = softThreshold ? .5f : .65f;
        float carThreshold = .75f;
        float angleThreshold = 20f;

        if (carForward.x > carThreshold) // Car is going right
        {
            if(angleToPedestrian > 0) // Pedestrian to the right
            {
                if(pedestrianForward.z > pedThreshold) // Pedestrian is looking left enough
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.z < -pedThreshold) // Pedestrian is looking right enough
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
        }
        else if(carForward.x < -carThreshold) // Car is going left
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.z < -pedThreshold) // Pedestrian is looking left enough
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.z > pedThreshold) // Pedestrian is looking right enough
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
        }
        else if (carForward.z > carThreshold) // Car is going up
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.x < -pedThreshold)
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.x > pedThreshold)
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
        }
        else if (carForward.z < -carThreshold) // Car is going down
        {
            if (angleToPedestrian > 0) // Pedestrian to the right
            {
                if (pedestrianForward.x > pedThreshold)
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
            else // Pedestrian to the left
            {
                if (pedestrianForward.x < -pedThreshold)
                {
                    return true;
                }
                return Mathf.Abs(angleToPedestrian) < angleThreshold;
            }
        }

        return false;
    }
    public void SetCrossingPos(Vector3 _crossingPos)
    {
        crossingPos = _crossingPos;
    }
}
