using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{
    public NavMeshAgent agent;
	private Animator animator;

    public Transform target;
    float checkUpdateTime = 1.5f;
    public bool isIndependent = true;

    [Header("Crossing")]
    public bool isCrossing = false;
    public bool isStoppedAtTrafficLight = false;
    public Vector3 crossingPos = Vector3.zero;
    private InvisiblePedestrian invisiblePedestrian = null;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;
    private Quaternion crossingRotation = Quaternion.identity;
    private Vector3 stoppingCrossPos = Vector3.zero;
    private Slot assignedSlot = null;

    private List<PedestrianIntersectionController> intersectionControllers = new List<PedestrianIntersectionController>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        if (target != null && isIndependent)
        {
            invisiblePedestrian = Instantiate(invisiblePedestrianPrefab, transform.position, transform.rotation);
            invisiblePedestrian.SetDestination(target.transform.position);
            invisiblePedestrian.SetPedestrian(this);
            //agent.SetDestination(target.transform.position);
            StartCoroutine(CheckArrivalToDestination());
        }
        else if (!isIndependent)
        {

        }
    }
    void Update()
    {
        if (!isStoppedAtTrafficLight)
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
        else
        {
            MatchTrafficLightStopRotation();
        }
    }

    public void SetTarget(Transform _target)
    {
        target = _target;
    }
    IEnumerator CheckArrivalToDestination()
    {
        yield return new WaitForSeconds(1);
        agent.SetDestination(target.transform.position);

        while (true)
        {
            yield return new WaitForSeconds(checkUpdateTime);
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if(distance < 5f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f) 
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isIndependent) return;

        if (other.gameObject.CompareTag("IntersectionPedestrianTrigger"))
        {
            var trigger = other.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
            if (trigger != null)
            {
                var controller = trigger.GetIntersectionController();
                if (intersectionControllers.Contains(controller))
                {
                    crossingRotation = trigger.transform.rotation;
                    // Suscribirse
                    controller.SubscribeToLightChangeEvent(OnTrafficLightChange);
                    // Mirar si hay que parar o no
                    if (controller.GetState() != TrafficLightState.Pedestrian)
                    {
                        // Detenerse
                        assignedSlot = trigger.GetSlotForPedestrian();
                        StartCoroutine(OnSlotAssigned());
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isIndependent) return;

        if (other.gameObject.CompareTag("IntersectionPedestrianTrigger"))
        {
        }
    }

    public void SetCrossings(List<PedestrianIntersectionController> _controllers)
    {
        intersectionControllers = _controllers;
    }

    private void OnTrafficLightChange(TrafficLightState newColor, bool subscription)
    {
        switch (newColor)
        {
            case TrafficLightState.Pedestrian:
                if (agent.isStopped)
                {
                    StartMoving();
                }
                break;

            default:

                break;

        }
    }

    private IEnumerator OnSlotAssigned()
    {
        assignedSlot.isLocked = true;
        agent.SetDestination(assignedSlot.position);
        bool slotReached = false;
        while (!slotReached)
        {
            yield return new WaitForSeconds(0.1f);
            float distance = Vector3.Distance(transform.position, assignedSlot.position);
            if (distance < 1f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f)
            {
                slotReached = true;
            }
        }
        StopMoving();
    }
    private void StartMoving()
    {
        assignedSlot.isLocked = false;
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
        animator.SetBool("IsMoving", true);
        agent.SetDestination(target.position);
        assignedSlot = null;
    }

    private void StopMoving()
    {
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
        animator.SetBool("IsMoving", false);
    }

    private void MatchTrafficLightStopRotation()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, crossingRotation, Time.deltaTime * agent.angularSpeed * 0.02f);
    }

    public void MatchRotation(Quaternion rotation)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * agent.angularSpeed * 0.02f);
    }
}
