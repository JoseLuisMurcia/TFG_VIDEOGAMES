using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityBehavior
{
    List<Transform> whiskers = new List<Transform>();

    private PathFollower pathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    public bool hasSignalInSight = false;
    LayerMask carLayer, signalLayer;
    private Transform transform;
    private Transform signalInSight;

    List<PathFollower> carsInSight = new List<PathFollower>();
    List<PathFollower> relevantCarsInSight = new List<PathFollower>();
    private bool visualDebug = false;
    public bool isInRoundabout = false;
    private AvoidanceBehavior avoidanceBehavior;

    public PriorityBehavior(LayerMask _carLayer, LayerMask _signalLayer, List<Transform> _whiskers, PathFollower _pathFollower, AvoidanceBehavior _avoidanceBehavior)
    {
        carLayer = _carLayer;
        signalLayer = _signalLayer;
        whiskers = _whiskers;
        pathFollower = _pathFollower;
        avoidanceBehavior = _avoidanceBehavior;
        pathFollower.priorityBehavior = this; 
    }

    public void Update(Transform _transform, bool _visualDebug)
    {
        rayOrigin = whiskers[0].position;
        visualDebug = _visualDebug;
        transform = _transform;
        
        if (hasSignalInSight)
            CheckIfSignalOutOfRange();

        if (carsInSight.Count > 0) ProcessRelevantPriorityCarsInSight();
    }

    void CheckIfSignalOutOfRange()
    {
        float distance = Vector3.Distance(transform.position, signalInSight.position);
        Vector3 carForward = transform.forward.normalized;
        Vector3 dirToSignal = (signalInSight.position - transform.position).normalized;
        float dot = Vector3.Dot(carForward, dirToSignal);
        if (isInRoundabout)
        {
            if (distance > 4.5f && dot < 0 && NextNodesAreStraight())
                RemoveSignalFromSight();
        }
        else
        {
            if (distance > 2f && dot < 0)
            {
                RemoveSignalFromSight();
            }
        }

    }

    private bool NextNodesAreStraight()
    {
        float angle = pathFollower.GetAngleBetweenCurrentNodeAndNumNodes(4);
        if (angle != Mathf.Infinity)
        {
            float threshold = 6f;
            if (angle < threshold)
                return true;
        }
        return false;
    }

    private void RemoveSignalFromSight()
    {
        signalInSight = null;
        hasSignalInSight = false;
        pathFollower.priorityLevel = PriorityLevel.Max;
    }
    private bool HasHitRoundaboutSignal(PathFollower _pathFollower)
    {
        return _pathFollower.priorityLevel == PriorityLevel.Roundabout;
    }

    private PriorityLevel GetPriorityOfSignal(string signalTag)
    {
        switch (signalTag)
        {
            case "Stop":
                return PriorityLevel.Stop;
            case "YieldRoundabout":
                return PriorityLevel.Roundabout;
            case "Yield":
                return PriorityLevel.Yield;
            default:
                return PriorityLevel.Max;
        }
    }

    private bool AngleNotRelevant(float angleToCarInSight, PathFollower carInSight)
    {
        if (carInSight.priorityLevel == PriorityLevel.Roundabout)
            return false;

        bool relevant = false;
        Vector3 carInSightForward = carInSight.transform.forward.normalized;
        float threshHold = 0.4f;
        if (angleToCarInSight < 0 && (carInSightForward.x < -threshHold || carInSightForward.z < -threshHold))
        {
            relevant = true;
        }
        else if (angleToCarInSight > 0 && (carInSightForward.x > threshHold || carInSightForward.z > threshHold))
        {
            relevant = true;
        }
        return relevant;
    }
    private void ProcessRelevantPriorityCarsInSight()
    {
        float relevantDistance = 19f;
        float maxDistance = 20f;
        List<PathFollower> carsToBeRemoved = new List<PathFollower>();
        Vector3 futureCarPosition = pathFollower.GetPosInPathInNumNodes(5);
        Vector3 dirToFuturePos = (futureCarPosition - transform.position).normalized;
        Vector3 carForward = transform.forward.normalized;
        float angleToFuturePos = Vector3.SignedAngle(carForward, dirToFuturePos, Vector3.up);

        foreach (PathFollower car in carsInSight)
        {
            Vector3 dirToCarInSight = (car.transform.position - transform.position).normalized;
            float angleToCarInSight = Vector3.SignedAngle(carForward, dirToCarInSight, Vector3.up);
            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            float dot = Vector3.Dot(carForward, dirToCarInSight);
            if (distanceToCar > maxDistance || dot < 0 || AngleNotRelevant(angleToCarInSight, car))
            {
                carsToBeRemoved.Add(car);
            }

            if (distanceToCar <= relevantDistance && dot > 0 && !pathFollower.pathRequested)
            {
                // HERE CHECK THE PATH TRAJECTORY
                if (futureCarPosition != Vector3.zero && !relevantCarsInSight.Contains(car))
                {
                    if (pathFollower.priorityLevel == PriorityLevel.Roundabout) // Hay que meter una comprobacion extra
                    {
                        if (angleToCarInSight < 0)
                            relevantCarsInSight.Add(car);
                    }
                    else
                    {
                        bool carInSightHasHigherPrio = (car.priorityLevel > pathFollower.priorityLevel) ? true : false;
                        // Nuestro coche va a tirar a la izquierda
                        if (angleToFuturePos < -5)
                        {
                            // Siempre vas a tener que a�adir el coche porque tiene la misma o m�s prioridad que tu y ante un giro a la izquierda hay que frenar
                            relevantCarsInSight.Add(car);
                        }
                        else if (angleToFuturePos > 5) // nuestro coche va a tirar a la derecha
                        {
                            // Solo si el coche que tienes delante tiene m�s preferencia deber�s parar
                            if (carInSightHasHigherPrio)
                                relevantCarsInSight.Add(car);
                        }
                        else // nuestro coche va a tirar recto
                        {
                            if (carInSightHasHigherPrio)
                                relevantCarsInSight.Add(car);
                        }
                    }


                }

            }
        }
        // Improve this by speedPercent calculations and distance?�
        foreach (PathFollower car in relevantCarsInSight)
        {
            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            float carSpeed = pathFollower.speedPercent;
            float carInSightSpeed = car.speedPercent;
            // Este valor indica el resultado umbral a partir del cual el coche deber� frenar o no
            float minValue = 0.1f;
            float divisionVal = carSpeed / carInSightSpeed;
            float distanceWithSpeed = divisionVal * distanceToCar;

            if (distanceToCar > relevantDistance || (isInRoundabout && !car.priorityBehavior.isInRoundabout))
                carsToBeRemoved.Add(car);

            if (!carsToBeRemoved.Contains(car))
            {
                if (pathFollower.stopPosition == Vector3.zero)
                {
                    if (distanceWithSpeed < 1f)
                        distanceWithSpeed = 0f;

                    pathFollower.stopPosition = transform.position + transform.forward * distanceWithSpeed * .25f;
                }
            }
        }

        foreach (PathFollower car in carsToBeRemoved)
        {
            carsInSight.Remove(car);
            relevantCarsInSight.Remove(car);
        }

        int numRelevantCars = relevantCarsInSight.Count;
        if (numRelevantCars > 0)
        {
            // TO FIX
            // Si el target, el coche que tienes delante, no tiene a true shouldStopPriority y t� si, entonces deja de mantener
            // la distancia con �l porque se va a ir y entonces t� paras por la priority�
            //if(pathFollower.carTarget != null && pathFollower.carTarget.GetComponent<PathFollower>().shouldStopPriority)
            //{

            //}
            if(pathFollower.gameObject.name == "Coche Stop (2)")
            {
                int haha = 5;
                haha += 3;
            }
            if (avoidanceBehavior.hasTarget)
            {
                if (pathFollower.targetPriorityBehavior.relevantCarsInSight.Count > 0)
                {
                    pathFollower.shouldStopPriority = false;
                    pathFollower.stopPosition = Vector3.zero;
                }
                else
                {
                    pathFollower.shouldStopPriority = true;
                }
            }
            else
            {
                pathFollower.shouldStopPriority = true;
            }

        }
        else
        {
            pathFollower.shouldStopPriority = false;
            pathFollower.stopPosition = Vector3.zero;
        }
    }


    public void ProcessSignalHit(Ray ray, RaycastHit hit)
    {
        Vector3 carForward = transform.forward.normalized;
        // We know it is on the right side of the car
        float angleBetweenCarAndSignal = Vector3.SignedAngle(carForward, ray.direction.normalized, Vector3.up);
        if (angleBetweenCarAndSignal > 0)
        {
            // Now we need to know if it is in looking in front of us
            Vector3 signalForward = hit.transform.forward;
            float angleBetweenCarAndSignalForward = Vector3.Angle(carForward, signalForward);
            if (angleBetweenCarAndSignalForward > 145)
            {
                //if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.green);
                hasSignalInSight = true;
                pathFollower.priorityLevel = GetPriorityOfSignal(hit.transform.gameObject.tag);
                signalInSight = hit.transform;
            }

        }
        else // In front but not in the same road
        {
            //if (visualDebug) Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
        }
    }

    public void ProcessCarHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        // Hacer cosas xd
        PathFollower hitCarPathFollower = hit.transform.gameObject.GetComponent<PathFollower>();
        PriorityLevel carPriority = pathFollower.priorityLevel;
        PriorityLevel hitCarPriority = hitCarPathFollower.priorityLevel;
        if (carPriority <= hitCarPriority)
        {
            Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
            Vector3 dirToHitCar = (hit.transform.position - transform.position).normalized;
            Vector3 carForward = transform.forward;
            float angleTolerance = 25f;
            // El plan es, detectar los coches que tengamos delante y con la misma prioridad o mayor y guardarlos en una lista que procesamos a parte
            // Descartar aquellos que tengamos delante con las mismas intenciones que nosotros O, guardar las distancias con aquellos tambien?�
            if (Vector3.Angle(carForward, hitCarForward) > angleTolerance && Vector3.Dot(transform.forward, dirToHitCar) > 0f)
            {
                if (!carsInSight.Contains(hitCarPathFollower))
                {
                    carsInSight.Add(hitCarPathFollower);
                    //Debug.DrawLine(rayOrigin, hit.point, Color.green);
                }

            }
        }
        else
        {
            //Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * 14f, Color.red);
        }
    }

}
