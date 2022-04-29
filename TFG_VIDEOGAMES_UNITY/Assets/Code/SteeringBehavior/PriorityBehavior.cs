using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityBehavior
{
    private PathFollower pathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    public bool hasSignalInSight = false;
    private Transform transform;
    private Transform signalInSight;

    List<PathFollower> carsInSight = new List<PathFollower>();
    List<PathFollower> relevantCarsInSight = new List<PathFollower>();
    private bool visualDebug = false;
    public bool isInRoundabout = false;
    private AvoidanceBehavior avoidanceBehavior;


    public PriorityBehavior(PathFollower _pathFollower, AvoidanceBehavior _avoidanceBehavior)
    {
        pathFollower = _pathFollower;
        avoidanceBehavior = _avoidanceBehavior;
        pathFollower.priorityBehavior = this;
    }

    public void Update(Transform _transform, bool _visualDebug, Vector3 _rayOrigin)
    {
        rayOrigin = _rayOrigin;
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
        PriorityLevel priority = pathFollower.priorityLevel;
        float dot = Vector3.Dot(carForward, dirToSignal);
        if (priority != PriorityLevel.Roundabout)
        {
            if (distance > 1.5f && dot < 0)
            {
                RemoveSignalFromSight();
            }
        }
    }

    public void RemoveSignalFromSight()
    {
        signalInSight = null;
        hasSignalInSight = false;
        pathFollower.priorityLevel = PriorityLevel.Max;
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
    private bool AngleNotRelevant(float angleToCarInSight, PathFollower carInSight, float dot)
    {
        if (dot < 0)
            return true;

        if (carInSight.priorityLevel == PriorityLevel.Roundabout)
            return false;

        Vector3 carInSightForward = carInSight.transform.forward;
        Vector3 myForward = transform.forward;
        float threshHold = 0.4f;
        if (angleToCarInSight < 0 && (carInSightForward.x < -threshHold || carInSightForward.z < -threshHold))
        {
            return true;
        }
        else if (angleToCarInSight > 0 && (carInSightForward.x > threshHold || carInSightForward.z > threshHold))
        {
            return true;
        }
        return false;
    }
    private void ProcessRelevantPriorityCarsInSight()
    {
        float maxDistance = 15f;
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

            if (distanceToCar > maxDistance || AngleNotRelevant(angleToCarInSight, car, dot))
            {
                carsToBeRemoved.Add(car);
            }

            if (distanceToCar <= maxDistance && !pathFollower.pathRequested)
            {
                // HERE CHECK THE PATH TRAJECTORY
                if (futureCarPosition != Vector3.zero && !relevantCarsInSight.Contains(car))
                {
                    // ROUNDABOUT CASE
                    if (!isInRoundabout && pathFollower.priorityLevel == PriorityLevel.Roundabout && distanceToCar <= 4f && angleToCarInSight < 0) // Hay que meter una comprobacion extra
                    {
                        relevantCarsInSight.Add(car);
                    }
                    else // EVERY OTHER CASE
                    {
                        if (isInRoundabout) // IF YOU ARE INSIDE A ROUNDABOUT, JUST GO, NO STOPPING POINTS
                        {
                            carsToBeRemoved.Add(car);
                        }
                        else // CLASSIC INTERSECTION SCENARIO
                        {
                            bool carInSightHasHigherPrio = (car.priorityLevel > pathFollower.priorityLevel) ? true : false;
                            // Nuestro coche va a tirar a la izquierda
                            if (angleToFuturePos < -5)
                            {
                                // Siempre vas a tener que añadir el coche porque tiene la misma o más prioridad que tu y ante un giro a la izquierda hay que frenar
                                relevantCarsInSight.Add(car);
                            }
                            else if (angleToFuturePos > 5) // nuestro coche va a tirar a la derecha
                            {
                                // Solo si el coche que tienes delante tiene más preferencia deberás parar
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
        }
        // TO FIX - If the car coming is too slow for our speed, dont event concern about it, ignore it
        // Improve this by speedPercent calculations and distance?¿
        foreach (PathFollower car in relevantCarsInSight)
        {
            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            float carSpeed = pathFollower.speedPercent;
            float carInSightSpeed = car.speedPercent;

            float divisionVal = carSpeed / carInSightSpeed;
            float distanceWithSpeed = divisionVal * distanceToCar;

            // Si está lejos o tu estas en una rotonda y él no, eliminar
            if (isInRoundabout && !car.priorityBehavior.isInRoundabout)
                carsToBeRemoved.Add(car);

            if (!carsToBeRemoved.Contains(car))
            {
                if (pathFollower.stopPosition == Vector3.zero)
                {
                    if (pathFollower.priorityLevel == PriorityLevel.Roundabout)
                    {
                        pathFollower.stopPosition = transform.position + transform.forward.normalized * 3f;
                    }
                    else
                    {
                        // TO FIX - ESTO CRASHEA
                        // Se está llamando cuando no se debe llamar, repasar la logica del metodo
                        Node nodeStop = pathFollower.GetStoppingNodeFromCurrentNode();
                        if (nodeStop == null)
                        {
                            //Debug.LogError("HOSTIA PROBLEMAS");
                            carsToBeRemoved.Add(car);
                        }
                        else
                        {
                            pathFollower.stopPosition = nodeStop.worldPosition;

                        }
                    }
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
            if (avoidanceBehavior.hasTarget)
            {
                if (TargetHasRelevantCars())
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
            float angleTolerance = 60f;
            // El plan es, detectar los coches que tengamos delante y con la misma prioridad o mayor y guardarlos en una lista que procesamos a parte
            // Descartar aquellos que tengamos delante con las mismas intenciones que nosotros O, guardar las distancias con aquellos tambien?¿
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

    private bool TargetHasRelevantCars()
    {
        return pathFollower.targetPriorityBehavior.relevantCarsInSight.Count > 0;
    }

}

