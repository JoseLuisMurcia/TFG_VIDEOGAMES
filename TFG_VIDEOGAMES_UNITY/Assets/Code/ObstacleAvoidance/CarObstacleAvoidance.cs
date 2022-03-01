using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarObstacleAvoidance : MonoBehaviour
{
    [SerializeField] Transform centerSensor, leftSensor, rightSensor;
    [SerializeField] LayerMask obstacleLayer;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = centerSensor.position;
        Ray ray = new Ray(position, leftSensor.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3f, obstacleLayer))
        {
            Debug.DrawRay(position, leftSensor.forward, Color.red);
        }
        else
        {
            Debug.DrawRay(position, leftSensor.forward, Color.blue);

        }

        ray = new Ray(position, centerSensor.forward);
        if (Physics.Raycast(ray, out hit, 5f, obstacleLayer))
        {
            Debug.DrawRay(position, centerSensor.forward, Color.red);
        }
        else
        {
            Debug.DrawRay(position, centerSensor.forward, Color.blue);

        }

        ray = new Ray(position, rightSensor.forward);
        if (Physics.Raycast(ray, out hit, 3f, obstacleLayer))
        {
            Debug.DrawRay(position, rightSensor.forward, Color.red);
        }
        else
        {
            Debug.DrawRay(position, rightSensor.forward, Color.blue);

        }

    }

   


    
}
