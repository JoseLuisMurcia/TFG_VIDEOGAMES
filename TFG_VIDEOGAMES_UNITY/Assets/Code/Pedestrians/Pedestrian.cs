using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pedestrian : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent agent;
    float turnSpeed = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        //PedestrianSpawner.instance.spa
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {

                StartCoroutine(LookAtTarget(hit.point));
            }
        }
    }

    IEnumerator LookAtTarget(Vector3 target)
    {
        float seconds = 0.5f;
        float t = 0;
        Quaternion initialRotation = transform.rotation;
        Vector3 direction = target - transform.position;
        while (t < 1)
        {
            transform.rotation = Quaternion.Slerp(initialRotation, Quaternion.LookRotation(direction), t);
            t += 0.0001f;
        }
        agent.SetDestination(target);
        yield return null;
    }
}
