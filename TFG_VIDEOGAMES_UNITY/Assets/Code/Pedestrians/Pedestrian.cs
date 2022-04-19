using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent agent;
	private Animator animator;

    private float baseSpeed;
    private float baseAngularSpeed;
    private float baseAcceleration;

    private bool slowedDown = false;
  
	void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        baseSpeed = agent.speed;
        baseAngularSpeed = agent.angularSpeed;
        baseAcceleration = agent.acceleration;
    }

    // Update is called once per frame
    void Update()
    {
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
				agent.SetDestination(hit.point);
			}
        }

        if (agent.isOnOffMeshLink && !slowedDown)
        {
            ReduceSpeedOnLink();
        }
        else
        {
            if(slowedDown) ResetSpecs();
        }

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

    }

    private void ReduceSpeedOnLink()
    {
        float speedSlash = 0.12f;
        agent.speed = baseSpeed * speedSlash;
        agent.angularSpeed = baseAngularSpeed * speedSlash;
        agent.acceleration = baseAcceleration * speedSlash;
        slowedDown = true;
    }

    private void ResetSpecs()
    {
        agent.speed = baseSpeed;
        agent.angularSpeed = baseAngularSpeed;
        agent.acceleration = baseAcceleration;
        slowedDown = false;
    }
}
