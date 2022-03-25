using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityBehavior : MonoBehaviour
{
    List<Transform> prioritySensors = new List<Transform>();

    [SerializeField] LayerMask carLayer, trafficSignLayer;
    float carRayDistance = 4f;
    private PathFollower pathFollower;
    private PathFollower hitCarPathFollower;
    private Vector3 rayOrigin;
    private Transform carTarget;
    [SerializeField] private bool hasSignalInSight = false;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
        Transform prioritySensorsParent = transform.Find("PrioritySensors");

        foreach (Transform sensor in prioritySensorsParent.transform)
        {
            prioritySensors.Add(sensor);
        }
    }

    void Update()
    {
        rayOrigin = prioritySensors[0].position;
        if (!hasSignalInSight)
            LookForTrafficSignals();
        else
            LookForCarsWithPriority();
    }

    // Check para detectar la señal, solo sirven los rayos a la derecha. (Signed Angle Check)
    // Una vez detectada la señal, se deja de buscar nuevas señales y se pasa a un nuevo modo de comportamiento en el que lo que se mira es por coches que sí tengan prioridad mientras tú no la tienes
    // Encontrar la señal es activar un modo de búsqueda de coches/comportamiento alternativo.
    // Hay que mirar tambien el producto escalar, no vale con que esté a la derecha solo, puede que el angulo entre la señal y tu forward
    private void LookForTrafficSignals()
    {
        foreach (Transform sensor in prioritySensors)
        {
            RaycastHit hit;
            Ray ray = new Ray(rayOrigin, sensor.forward);
            if (Physics.Raycast(ray, out hit, carRayDistance, trafficSignLayer))
            {

                Vector3 carForward = transform.forward.normalized;
                float signedAngle = Vector3.SignedAngle(carForward, ray.direction.normalized, Vector3.up);
                if(signedAngle > 0)
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.green);
                    hasSignalInSight = true;
                }
                else
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
                }

            }
            else
            {
                Debug.DrawLine(rayOrigin, rayOrigin + sensor.forward * carRayDistance, Color.red);

            }
        }
    }

    // Este metodo va a tener que ser sensible a los casos particulares, de forma que si quieres girar a la izquierda o derecha tendrás que hacer unas comprobaciones u otras
    // No tienes que mirar hacia la izquierda si quieres girar a la izquierda, a no ser que sea una carretera de doble sentido a la que te quieres incorporar.
    // Habrá que modificar el pathfollower para que se pueda parar en un sitio concreto si detecta a un coche con prioridad que interrumpe tu trayectoria

    // Distinguir entre stop y ceda?
    private void LookForCarsWithPriority() 
    {
        
    }

}
