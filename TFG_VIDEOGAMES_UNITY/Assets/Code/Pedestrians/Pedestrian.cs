using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{
    public NavMeshAgent agent;
	private Animator animator;

    private LineRenderer line;
    private Vector3 destination = Vector3.zero;
    public Transform target;
    float checkUpdateTime = 1f;

    [Header("Crossing")]
    public bool isCrossing = false;
    public Vector3 crossingPos;
  
	void Start()
    {
        line = GetComponent<LineRenderer>();
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        if (destination != Vector3.zero)
            agent.SetDestination(destination);

        if (target != null)
        {
            agent.SetDestination(target.transform.position);
        }
        DrawPath(agent.path);
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

    public void SetTarget(Transform _target)
    {
        destination = _target.position;
        target = _target;

    }

    private void DrawPath(NavMeshPath path)
    {
        line.positionCount = path.corners.Length;
        line.SetPositions(path.corners);
        line.enabled = true;
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
