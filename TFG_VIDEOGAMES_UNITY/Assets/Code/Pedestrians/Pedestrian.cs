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
    private float angularSpeed;
    private float acceleration;
  
	void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
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

    }
}
