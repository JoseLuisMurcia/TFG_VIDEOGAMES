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

    public List<PathFollower> carsInSight = new List<PathFollower>();
    public List<PathFollower> relevantCarsInSight = new List<PathFollower>();
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
            if (distance > 3.5f && dot < 0)
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

        float angleBetweenForwards = Vector3.SignedAngle(transform.forward, carInSight.transform.forward, Vector3.up);

        if (pathFollower.priorityLevel == PriorityLevel.Roundabout)
        {
            if (angleToCarInSight > 0) // The car is to our right
                return true;

            if (angleToCarInSight < 0 && angleToCarInSight > -10f) // Just in front or slightly to the left
                return true;

            if (angleToCarInSight < -10f)
            {
                if (angleBetweenForwards > 80f && angleBetweenForwards < 160f)
                    return false;

                if (angleBetweenForwards > -80f)
                    return true;

                if (angleBetweenForwards < -80f)
                    return false;
            }
        }
        string name = transform.gameObject.name;
        if (angleBetweenForwards < 7.5f && angleBetweenForwards > -7.5f) // The car is going in the same direction as us
            return true;

        if (angleToCarInSight > 0f) // The car is on your right
        {
            if (angleBetweenForwards > 50f && angleBetweenForwards < 160f) // It is looking right
                return true;

            return false;
        }
        else // The car is on your left
        {
            if (angleBetweenForwards < 0f) // The car is facing left
            {
                if (angleBetweenForwards > -130f) // This thresholds are important to take into account cars coming in front
                    return true;

                return false;
            }
            else // The car is facing right
            {
                if (angleBetweenForwards < 130f)
                    return true;

                return false;
            }
        }
    }
    private void ProcessRelevantPriorityCarsInSight()
    {
        float maxDistance = 25f;
        List<PathFollower> carsToBeRemoved = new List<PathFollower>();
        Vector3 futureCarPosition = pathFollower.GetPosInPathInNumNodes(6);
        Vector3 dirToFuturePos = (futureCarPosition - transform.position).normalized;
        Vector3 carForward = transform.forward.normalized;
        float angleToFuturePos = Vector3.SignedAngle(carForward, dirToFuturePos, Vector3.up);

        foreach (PathFollower car in carsInSight)
        {
            Vector3 dirToCarInSight = (car.transform.position - transform.position).normalized;
            float angleToCarInSight = Vector3.SignedAngle(carForward, dirToCarInSight, Vector3.up);
            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            float dot = Vector3.Dot(carForward, dirToCarInSight);

            if (distanceToCar > maxDistance || AngleNotRelevant(angleToCarInSight, car, dot) || pathFollower.priorityLevel > car.priorityLevel)
            {
                carsToBeRemoved.Add(car);
            }

            if (distanceToCar <= maxDistance && !pathFollower.pathRequested && !carsToBeRemoved.Contains(car))
            {
                // HERE CHECK THE PATH TRAJECTORY
                if (futureCarPosition != Vector3.zero && !relevantCarsInSight.Contains(car))
                {
                    // ROUNDABOUT CASE
                    if (!isInRoundabout && pathFollower.priorityLevel == PriorityLevel.Roundabout && distanceToCar <= 8f && angleToCarInSight < 0 && !car.priorityBehavior.isInRoundabout) // Hay que meter una comprobacion extra
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
            // Si  estas en una rotonda y él no, eliminar
            if (isInRoundabout && !car.priorityBehavior.isInRoundabout)
                carsToBeRemoved.Add(car);

            if (!carsToBeRemoved.Contains(car))
            {
                if (pathFollower.stopPosition == Vector3.zero)
                {
                    if (pathFollower.priorityLevel == PriorityLevel.Roundabout) // AQUI ES LO QUE ESTÁ PASANDO CUANDO SE PARAN ANTES DE TIEMPO O SE PARAN TARDE
                    {
                        pathFollower.stopPosition = GetClosestStoppingPosInRoundabout();
                    }
                    else // Cualquier caso que no sea una rotonda
                    {
                        Node nodeStop = GetStoppingNodeFromCurrentRoad();
                        if (nodeStop == null)
                        {
                            pathFollower.stopPosition = transform.position + transform.forward * .1f;
                        }
                        else
                        {
                            pathFollower.stopPosition = nodeStop.worldPosition;

                        }
                    }
                }
                else
                {
                    float distanceToStoppingNode = Vector3.Distance(pathFollower.stopPosition, transform.position);
                    float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
                    float division = distanceToStoppingNode / distanceToCar;

                    if (pathFollower.priorityLevel != PriorityLevel.Roundabout)
                    {
                        if (isInRoundabout)
                        {
                            carsToBeRemoved.Add(car);
                        }
                        else
                        {
                            if (division < 0.1f && distanceToCar > 6.5f && distanceToCar < 18f && pathFollower.speedPercent > 0.2f)
                            {
                                carsToBeRemoved.Add(car);
                            }
                        }

                    }
                    else // You are waiting before a roundabout or about to enter it
                    {

                        if (distanceToCar > 12f)
                            carsToBeRemoved.Add(car);

                        if (car.priorityLevel == PriorityLevel.Roundabout && car.speedPercent < 0.15f)
                            carsToBeRemoved.Add(car);

                        if (car.priorityLevel != PriorityLevel.Roundabout && !car.priorityBehavior.isInRoundabout)
                            carsToBeRemoved.Add(car);

                        if (distanceToStoppingNode < 0.3f && distanceToCar > 3f && car.speedPercent < 0.15f)
                        {
                            carsToBeRemoved.Add(car);
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
                    // LOS TIRONES QUE PEGAN LOS COCHES ESPERANDO POR PRIORITY ANTES DE ARRANCAR SE DEBEN A QUE UTILIZAN EL TARGET
                    // QUE TIENEN ASIGNADO Y SE VAN AL STOPPING POINT CUANDO SU TARGET YA NO TIENE RELEVANT CARS, PASA SIEMPRE EN ESE MOMENTO.
                    // PERO POR QUÉ SI EL NO TIENE, YO SI?
                    if (pathFollower.targetPathFollower.speedPercent > 0.25f)
                        SetShouldStopPriority();
                }
            }
            else
            {
                SetShouldStopPriority();
            }

        }
        else
        {
            pathFollower.shouldStopPriority = false;
            pathFollower.stopPosition = Vector3.zero;
        }


    }
    private void SetShouldStopPriority()
    {
        if (pathFollower.stopPosition == Vector3.zero)
            return;
        pathFollower.shouldStopPriority = true;
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
        PathFollower hitCarPathFollower = hit.transform.gameObject.GetComponent<PathFollower>();
        if (carsInSight.Contains(hitCarPathFollower))
            return;

        PriorityLevel carPriority = pathFollower.priorityLevel;
        PriorityLevel hitCarPriority = hitCarPathFollower.priorityLevel;

        if (carPriority <= hitCarPriority)
        {
            Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
            Vector3 dirToHitCar = (hit.transform.position - transform.position).normalized;
            Vector3 carForward = transform.forward;
            float angleTolerance = 45f;
            // El plan es, detectar los coches que tengamos delante y con la misma prioridad o mayor y guardarlos en una lista que procesamos a parte
            // Descartar aquellos que tengamos delante con las mismas intenciones que nosotros O, guardar las distancias con aquellos tambien?¿
            if (Vector3.Angle(carForward, hitCarForward) > angleTolerance && Vector3.Dot(transform.forward, dirToHitCar) > 0f)
            {
                //Debug.DrawLine(rayOrigin, hit.point, Color.green);
                carsInSight.Add(hitCarPathFollower);
            }
        }
        else
        {
            //Debug.DrawLine(rayOrigin, hit.point, Color.red);
        }
    }
    private bool TargetHasRelevantCars()
    {
        if (pathFollower.targetPriorityBehavior == null)
        {
            Debug.Log("pathFollowerTarget: " + pathFollower.carTarget);
            Debug.Log("pathFollowerTarget: " + pathFollower.targetPathFollower);
            Debug.LogError("TARGET PRIORITY BEHAVIOR ES NULL DIOOOOOOO");

        }
        return pathFollower.targetPriorityBehavior.relevantCarsInSight.Count > 0;
    }
    private Vector3 GetClosestStoppingPosInRoundabout()
    {
        Roundabout road = FindRoundaboutInPath(pathFollower.nodeList[pathFollower.pathIndex]);
        if (road == null)
            return Vector3.zero;

        float bestDistance = Mathf.Infinity;
        float distance;
        Vector3 bestPos = Vector3.zero;
        Node bestNode = null;
        foreach (Node entry in road.entryNodes)
        {
            distance = Vector3.Distance(entry.worldPosition, transform.position);
            if (distance < bestDistance)
            {
                bestPos = entry.worldPosition;
                bestNode = entry;
                bestDistance = distance;
            }
        }
        // Now multiply it a bit so that it is proportional to the distance to the center and it nails on the perfect spot
        Vector3 nodeForward = (bestNode.worldPosition - bestNode.previousNode.worldPosition).normalized;
        float distanceToTheCenter = Vector3.Distance(bestNode.worldPosition, road.transform.position);
        bestPos = bestNode.worldPosition + nodeForward * distanceToTheCenter * .05f;
        // bestPos = bestNode.worldPosition ;
        //SpawnSpheres(transform.position, bestNode.worldPosition, Color.white, Color.black);
        return bestPos;
    }
    private Roundabout FindRoundaboutInPath(Node currentNode)
    {
        bool roundaboutFound = false;
        Roundabout roundabout = null;
        Node startingNode = currentNode;
        int i = 0;
        while (!roundaboutFound && i < 10)
        {
            if (currentNode.neighbours[0].road.typeOfRoad == TypeOfRoad.Roundabout)
            {
                roundabout = (Roundabout)currentNode.neighbours[0].road;
            }
            else
            {
                currentNode = currentNode.neighbours[0];
            }
            i++;
        }
        if (roundabout == null)
        {
            SpawnSpheres(startingNode.worldPosition, currentNode.worldPosition, Color.white, Color.red);
            SpawnSphere(transform.position, Color.cyan);
            hasSignalInSight = true;
            pathFollower.priorityLevel = PriorityLevel.Max;
            Debug.LogWarning("HEMOS HECHO LA 13 14 HAHAHA");
        }
        return roundabout;
    }
    private Node GetStoppingNodeFromCurrentRoad()
    {
        Node startingNode = pathFollower.nodeList[pathFollower.pathIndex];
        Node currentNode = startingNode;
        Road currentRoad = currentNode.road;
        Node stoppingNode = null;
        int i = 0;
        while (stoppingNode == null && i < 10)
        {
            if (currentRoad.exitNodes.Contains(currentNode))
            {
                stoppingNode = currentNode;
            }
            else
            {
                currentNode = currentNode.neighbours[0];
            }
            i++;
        }
        if (stoppingNode == null)
        {
            //Debug.LogError("NO STOPPING NODE FOUND");
            //SpawnSpheres(startingNode.worldPosition, currentNode.worldPosition, Color.red);
        }
        else
        {
            //if (visualDebug) SpawnSpheres(startingNode.worldPosition, stoppingNode.worldPosition, Color.white, Color.black);
        }

        //SpawnSphere(stoppingNode.worldPosition, Color.magenta);
        return stoppingNode;
    }
    private void SpawnSphere(Vector3 pos, Color color)
    {
        GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //startSphere.transform.parent = transform;
        startSphere.transform.position = pos + Vector3.up;
        startSphere.GetComponent<Renderer>().material.SetColor("_Color", color);
    }
    private void SpawnSpheres(Vector3 pos1, Vector3 pos2, Color color1, Color color2)
    {
        GameObject startSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //startSphere.transform.parent = transform;
        startSphere.transform.position = pos1 + Vector3.up;
        startSphere.GetComponent<Renderer>().material.SetColor("_Color", color1);

        GameObject endSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //endSphere.transform.parent = transform;
        endSphere.transform.position = pos2 + Vector3.up;
        endSphere.GetComponent<Renderer>().material.SetColor("_Color", color2);
    }

}

