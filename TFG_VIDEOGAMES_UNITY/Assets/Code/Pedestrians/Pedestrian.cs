using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent agent;
	private Animator animator;

    private Vector3 destination = Vector3.zero;
    float checkUpdateTime = 1f;

    [Header("Crossing")]
    public bool isCrossing = false;
    public Vector3 crossingPos;
  
	void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        if (destination != Vector3.zero)
            agent.SetDestination(destination);

        StartCoroutine(CheckArrivalToDestination());
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.remainingDistance > agent.stoppingDistance)
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

    }

    public void SetTarget(Transform target)
    {
        destination = target.position;
    }

    IEnumerator CheckArrivalToDestination()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkUpdateTime);
            float distance = Vector3.Distance(transform.position, destination);
            if(distance < 5f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f) 
            {
                Destroy(gameObject);
            }
        }
    }
}
