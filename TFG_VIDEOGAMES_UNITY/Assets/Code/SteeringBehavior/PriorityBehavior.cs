using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityBehavior : MonoBehaviour
{
    List<Transform> prioritySensors = new List<Transform>();
    List<Transform> signalSensors = new List<Transform>();

    [SerializeField] LayerMask carLayer, trafficSignLayer;
    private PathFollower pathFollower;
    private PathFollower hitCarPathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    private float carRayDistance = 3f;
    private float signalSensorReach = 2f;
    private float prioritySensorReach = 3f;
    [SerializeField] private bool hasSignalInSight = false;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        Transform signalSensorsParent = transform.Find("SignalSensors");
        Transform prioritySensorsParent = transform.Find("PrioritySensors");

        foreach (Transform sensor in signalSensorsParent.transform)
        {
            signalSensors.Add(sensor);
        }
        foreach (Transform sensor in prioritySensorsParent.transform)
        {
            prioritySensors.Add(sensor);
        }
    }

    void Update()
    {
        rayOrigin = signalSensors[0].position;
        if(!hasSignalInSight) LookForTrafficSignals();
        LookForCarsWithPriority();
    }

    // Check para detectar la señal, solo sirven los rayos a la derecha. (Signed Angle Check)
    // Una vez detectada la señal, se deja de buscar nuevas señales y se pasa a un nuevo modo de comportamiento en el que lo que se mira es por coches que sí tengan prioridad mientras tú no la tienes
    // Cuando se deja de mirar por la señal?
    private void LookForTrafficSignals()
    {
        foreach (Transform sensor in signalSensors)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, carRayDistance * signalSensorReach, trafficSignLayer))
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
    private void LookForCarsWithPriority() 
    {
        foreach (Transform sensor in prioritySensors)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, carRayDistance * prioritySensorReach, carLayer))
            {
                // Hacer cosas xd
                PriorityLevel priority = pathFollower.priorityLevel;
                // Utilizar el pathfollower para conocer las intenciones del coche
            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance * prioritySensorReach, Color.red);

            }
        }
    }

}
