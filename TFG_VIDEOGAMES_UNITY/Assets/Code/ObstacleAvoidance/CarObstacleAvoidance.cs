using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarObstacleAvoidance : MonoBehaviour
{
    [SerializeField] Transform centerSensor, leftSensor, rightSensor;
    [SerializeField] LayerMask obstacleLayer;
    public bool objectHit = false;
    private PathFollower pathFollower;
    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    // Update is called once per frame
    void Update()
    {
        if (objectHit) return;
        Vector3 position = centerSensor.position;
        Ray ray = new Ray(position, leftSensor.forward);
        RaycastHit hit;
        float rayDistance = 0.75f;

        // If the object is hit with the middle ray first, go left or right, it does not matter.
        // If the object is hit with the left ray, create a position to go to the right
        // If the object is hit with the right ray, create a position to go to the left
        // Only if the object hit is in the middle of our path should we really try to evade it
        // Also, if we have already calculated a position, we should not keep calculating unless there is an object that is not letting us return.

        // Si se quiere hacer para evitar objetos en movimiento, se debería calcular la trayectoria de ese movimiento para ver si la trayectoria resultaría en una colisión.

        // Necesito un metodo en pathfollower para detectar en que nodo actual estoy, como insertar un nuevo nodo entre el actual y el siguiente que sea la posicion de evasion
        // Crear un nuevo objeto path, manteniendo el pathIndex.
        if (Physics.Raycast(ray, out hit, 3f * rayDistance, obstacleLayer))
        {
            Debug.DrawRay(position, leftSensor.forward, Color.red);
        }
        else
        {
            Debug.DrawRay(position, position + leftSensor.forward * 2.5f, Color.blue);

        }

        ray = new Ray(position, centerSensor.forward);
        if (Physics.Raycast(ray, out hit, 5f * rayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawRay(position, centerSensor.forward, Color.red);
            Debug.DrawLine(position, hit.point, Color.red);
            Vector3 newPoint = hit.point + transform.right;
            pathFollower.SetNewPathByAvoidance(newPoint);
        }
        else
        {
            Debug.DrawLine(position, position + centerSensor.forward * 4f, Color.blue);

        }

        ray = new Ray(position, rightSensor.forward);
        if (Physics.Raycast(ray, out hit, 3f * rayDistance, obstacleLayer))
        {
            Debug.DrawRay(position, rightSensor.forward, Color.red);
        }
        else
        {
            Debug.DrawRay(position, position + rightSensor.forward * 2.5f, Color.blue);

        }

    }

   


    
}
