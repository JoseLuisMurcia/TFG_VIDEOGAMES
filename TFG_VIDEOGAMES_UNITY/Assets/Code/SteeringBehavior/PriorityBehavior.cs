using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityBehavior
{
    List<Transform> whiskers = new List<Transform>();

    private PathFollower pathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    private float carRayDistance = 3f;
    private float prioritySensorReach = 3f;
    public bool hasSignalInSight = false;
    LayerMask carLayer, signalLayer;
    private Transform transform;
    private Transform signalInSight;

    List<PathFollower> carsInSight = new List<PathFollower>();
    List<PathFollower> relevantCarsInSight = new List<PathFollower>();

    public PriorityBehavior(LayerMask _carLayer, LayerMask _signalLayer, List<Transform> _whiskers, PathFollower _pathFollower)
    {
        carLayer = _carLayer;
        signalLayer = _signalLayer;
        whiskers = _whiskers;
        pathFollower = _pathFollower;
    }

    public void Update(Transform _transform)
    {
        rayOrigin = whiskers[0].position;
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
        float dot = Vector3.SignedAngle(carForward, dirToSignal, Vector3.up);
        if (distance > 3 && dot < 0)
        {
            signalInSight = null;
            hasSignalInSight = false;
        }
    }

    private PriorityLevel GetPriorityOfSignal(string signalTag)
    {
        switch (signalTag)
        {
            case "Stop":
                return PriorityLevel.Stop;
            case "Yield":
                return PriorityLevel.Yield;
            default:
                return PriorityLevel.Max;
        }
    }

    // Sumar 5 al path que tienen, si se pasa hay que mandar a calcular uno nuevo.
    // La forma en la que va a funcionar es la siguiente:
    // En carsInSight se guardan todos los coches que el coche detecte que vengan en un sentido el cual pueda colisionar, además de que tengan más prioridad que él.
    // En relevantCarsInSight se guardan aquellos coches de carsInSight que están lo suficientemente cerca y tienen una trayectoria que interfiere con la nuestra.
    // Si en relevantCarsInSight hay un coche, nuestro coche debe esperar

    // Como se comprueba la trayectoria?¿
    // Cogemos nuestro pathFollower y le sumamos 4 o 5 puntos para recibir la posicion que tendría nuestro coche en aquel entonces, de esa forma sabemos la intención
    // Podemos coger el angulo desde el punto actual hasta ese nuevo punto y de esa forma, saber si vamos a ir a la izquierda o a la derecha.
    // Con esa informacion, la distancia y la prioridad de dichos coches, sabremos si tenemos que parar o no.
    private void ProcessRelevantPriorityCarsInSight()
    {
        float relevantDistance = 15f;
        float maxDistance = 20f;
        List<PathFollower> carsToBeRemoved = new List<PathFollower>();
        Vector3 futureCarPosition = pathFollower.GetPosInPathInNumNodes(5);
        Vector3 dirToFuturePos = (futureCarPosition - transform.position).normalized;
        Vector3 carForward = transform.forward.normalized;
        float angle = Vector3.SignedAngle(carForward, dirToFuturePos, Vector3.up);

        // Utilizar el pathfollower para conocer las intenciones del coche
        foreach (PathFollower car in carsInSight)
        {
            Vector3 dirToCarInSight = (car.transform.position - transform.position).normalized;

            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            float dot = Vector3.Dot(carForward, dirToCarInSight);
            // If the car is too far or already behind us we can discard it
            if (distanceToCar > maxDistance || dot < 0)
            {
                carsToBeRemoved.Add(car);
            }

            if (distanceToCar <= relevantDistance && dot > 0 && !pathFollower.pathRequested)
            {
                // HERE CHECK THE PATH TRAJECTORY
                if (futureCarPosition != Vector3.zero && !relevantCarsInSight.Contains(car))
                {
                    bool carInSightHasHigherPrio = (pathFollower.priorityLevel > car.priorityLevel) ? true : false;
                    // Nuestro coche va a tirar a la izquierda
                    if (angle < -5)
                    {
                        relevantCarsInSight.Add(car);
                    }
                    else if (angle > 5) // nuestro coche va a tirar a la derecha
                    {
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

        int numRelevantCars = relevantCarsInSight.Count;
        if (numRelevantCars > 0)
        {
            // Tell the pathfollower to stop at a certain point 
            // Si el target, el coche que tienes delante, no tiene a true shouldStopPriority y tú si, entonces deja de mantener
            // la distancia con él porque se va a ir y entonces tú paras por la priority
            pathFollower.shouldStopPriority = true;

        }
        else
        {
            // Tell the pathfollower to resume its previous behavior
            pathFollower.shouldStopPriority = false;
        }

        foreach (PathFollower car in relevantCarsInSight)
        {
            float distanceToCar = Vector3.Distance(car.transform.position, transform.position);
            Vector3 dirToCarInSight = (car.transform.position - transform.position).normalized;

            float dot = Vector3.Dot(carForward, dirToCarInSight);
            if (distanceToCar > relevantDistance || dot < 0)
                carsToBeRemoved.Add(car);
        }

        foreach (PathFollower car in carsToBeRemoved)
        {
            carsInSight.Remove(car);
            //if (relevantCarsInSight.Contains(car))
            relevantCarsInSight.Remove(car);
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
                Debug.DrawLine(rayOrigin, hit.point, Color.green);
                hasSignalInSight = true;
                pathFollower.priorityLevel = GetPriorityOfSignal(hit.transform.gameObject.tag);
                signalInSight = hit.transform;
            }

        }
        else // In front but not in the same road
        {
            Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
        }
    }

    public void ProcessCarHit(Ray ray, RaycastHit hit, Transform sensor)
    {
        // Hacer cosas xd
        PathFollower hitCarPathFollower = hit.transform.gameObject.GetComponent<PathFollower>();
        PriorityLevel carPriority = pathFollower.priorityLevel;
        PriorityLevel hitCarPriority = pathFollower.priorityLevel;
        if (carPriority <= hitCarPriority)
        {
            Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
            Vector3 dirToHitCar = (hit.transform.position - transform.position).normalized;
            Vector3 carForward = transform.forward;
            float angleTolerance = 20f;
            // El plan es, detectar los coches que tengamos delante y con la misma prioridad o mayor y guardarlos en una lista que procesamos a parte
            // Descartar aquellos que tengamos delante con las mismas intenciones que nosotros O, guardar las distancias con aquellos tambien?¿
            if (Vector3.Angle(hitCarForward, carForward) > angleTolerance && Vector3.Dot(transform.forward, dirToHitCar) > 0f)
            {
                if (!carsInSight.Contains(hitCarPathFollower))
                    carsInSight.Add(hitCarPathFollower);
            }
        }
        else
        {
            Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * prioritySensorReach, Color.red);
        }
    }

}
