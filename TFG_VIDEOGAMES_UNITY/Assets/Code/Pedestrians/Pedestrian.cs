using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{
    public NavMeshAgent agent;
	private Animator animator;

    private Vector3 destination = Vector3.zero;
    public Transform target;
    float checkUpdateTime = 1.5f;

    [Header("Crossing")]
    public bool isCrossing = false;
    public Vector3 crossingPos;
    private InvisiblePedestrian invisiblePedestrian;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;

    private List<TrafficLightScheduler> schedulers = new List<TrafficLightScheduler>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        if (destination != Vector3.zero)
        {
            agent.SetDestination(destination);
            StartCoroutine(CheckArrivalToDestination());
        }

        if (target != null)
        {
            invisiblePedestrian = Instantiate(invisiblePedestrianPrefab, transform.position, transform.rotation);
            invisiblePedestrian.SetDestination(target.transform.position);
            invisiblePedestrian.SetPedestrian(this);
            agent.SetDestination(target.transform.position);
            StartCoroutine(CheckArrivalToDestination());
        }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("IntersectionPedestrianTrigger"))
        {
            var trigger = other.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
            if (trigger != null)
            {
                var scheduler = trigger.GetScheduler();
                if (schedulers.Contains(scheduler))
                {
                    // Mirar si hay que parar o no
                    if (scheduler.GetState() == TrafficLightState.Pedestrian)
                    {
                        // Cruzar
                    }
                    else
                    {
                        // Detenerse
                    }
                }
            }
            Debug.Log("trigger entry");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("IntersectionPedestrianTrigger"))
        {
        }

    }

    public void SetCrossings(List<TrafficLightScheduler> _schedulers)
    {
        schedulers = _schedulers;
    }
}
