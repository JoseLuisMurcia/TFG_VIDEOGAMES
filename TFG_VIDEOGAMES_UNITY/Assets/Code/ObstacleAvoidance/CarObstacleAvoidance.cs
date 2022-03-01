using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarObstacleAvoidance : MonoBehaviour
{
    [SerializeField] float rayRange = 2f;
    [SerializeField] int numberOfRays = 3;
    [SerializeField] float angle = 90;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 deltaPos = Vector3.zero;
        for(int i=0; i < numberOfRays; i++)
        {
            Quaternion rot = transform.rotation;
            Quaternion rotMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, transform.position);
            Vector3 direction = rot * rotMod * Vector3.forward;

            Ray ray = new Ray(transform.position, direction);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, rayRange))
            {
                Debug.DrawRay(transform.position, direction, Color.red);
            }
        }
    }
}
