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
    private float signalSensorReach = 2f;
    private float prioritySensorReach = 3f;
    [SerializeField] private bool hasSignalInSight = false;
    LayerMask carLayer, signalLayer;

    List<PathFollower> carsInSight = new List<PathFollower>();

    public PriorityBehavior(LayerMask _carLayer, LayerMask _signalLayer, List<Transform> _whiskers, PathFollower _pathFollower)
    {
        carLayer = _carLayer;
        signalLayer = _signalLayer;
        whiskers = _whiskers;
        pathFollower = _pathFollower;
    }

    public void Update(Transform transform)
    {
        rayOrigin = whiskers[0].position;
        if(!hasSignalInSight) LookForTrafficSignals(transform);
        LookForCarsWithPriority(transform);
    }

    // Check para detectar la señal, solo sirven los rayos a la derecha. (Signed Angle Check)
    // Una vez detectada la señal, se deja de buscar nuevas señales y se pasa a un nuevo modo de comportamiento en el que lo que se mira es por coches que sí tengan prioridad mientras tú no la tienes
    // Cuando se deja de mirar por la señal?
    private void LookForTrafficSignals(Transform transform)
    {
        foreach (Transform sensor in whiskers)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, carRayDistance * signalSensorReach, signalLayer))
            {
                Vector3 carForward = transform.forward.normalized;
                // We know it is on the right side of the car
                float angleBetweenCarAndSignal = Vector3.SignedAngle(carForward, ray.direction.normalized, Vector3.up);
                if(angleBetweenCarAndSignal > 0)
                {
                    // Now we need to know if it is in looking in front of us
                    Vector3 signalForward = hit.transform.forward;
                    float angleBetweenCarAndSignalForward = Vector3.Angle(carForward, signalForward);
                    if(angleBetweenCarAndSignalForward > 145)
                    {
                        Debug.DrawLine(rayOrigin, hit.point, Color.green);
                        hasSignalInSight = true;
                        pathFollower.priorityLevel = GetPriorityOfSignal(hit.transform.gameObject.tag);
                    }
                    
                }
                else // In front but not in the same road
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
                }

            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * signalSensorReach, Color.red);

            }
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

    // Este metodo va a tener que ser sensible a los casos particulares, de forma que si quieres girar a la izquierda o derecha tendrás que hacer unas comprobaciones u otras
    // No tienes que mirar hacia la izquierda si quieres girar a la izquierda, a no ser que sea una carretera de doble sentido a la que te quieres incorporar.
    // Habrá que modificar el pathfollower para que se pueda parar en un sitio concreto si detecta a un coche con prioridad que interrumpe tu trayectoria

    // Distinguir entre stop y ceda?
    private void LookForCarsWithPriority(Transform transform) 
    {
        foreach (Transform sensor in whiskers)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, carRayDistance * prioritySensorReach, carLayer))
            {
                // Hacer cosas xd
                PathFollower hitCarPathFollower = hit.transform.gameObject.GetComponent<PathFollower>();
                PriorityLevel carPriority = pathFollower.priorityLevel;
                PriorityLevel hitCarPriority = pathFollower.priorityLevel;
                if(carPriority <= hitCarPriority)
                {
                    Vector3 hitCarForward = hit.collider.gameObject.transform.forward;
                    Vector3 dirToHitCar = (hit.transform.position - transform.position).normalized;
                    Vector3 carForward = transform.forward;
                    float angleTolerance = 20f;
                    // El plan es, detectar los coches que tengamos delante y con la misma prioridad o mayor y guardarlos en una lista que procesamos a parte
                    // Descartar aquellos que tengamos delante con las mismas intenciones que nosotros O, guardar las distancias con aquellos tambien?¿
                    if (Vector3.Angle(hitCarForward, carForward) > angleTolerance)
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
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * prioritySensorReach, Color.red);

            }
        }
    }

    private void ProcessRelevantPriorityCarsInSight()
    {
        // loop through carsInSight
        // Utilizar el pathfollower para conocer las intenciones del coche
         
    }

}
