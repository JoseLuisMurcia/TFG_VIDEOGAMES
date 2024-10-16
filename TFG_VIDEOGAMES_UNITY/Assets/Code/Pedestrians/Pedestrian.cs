using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
public class Pedestrian : MonoBehaviour
{
    public NavMeshAgent agent;
	private Animator animator;

    public Transform target;
    private Vector3 destination;
    float checkUpdateTime = 1.5f;
    public bool isIndependent = true;
    private float baseAgentSpeed = 1f;
    private float sprintAgentSpeed = 1.7f;

    [Header("Crossing")]
    public bool isCrossing = false;
    public bool hasCrossed = false;
    private InvisiblePedestrian invisiblePedestrian = null;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;

    [Header("TrafficLightCrossing")]
    public bool isStoppedAtTrafficLight = false;
    private Quaternion crossingRotation = Quaternion.identity;
    private Slot assignedSlot = null;
    private Vector3 mirrorSlot = Vector3.zero;
    private List<PedestrianIntersectionController> intersectionControllers = new List<PedestrianIntersectionController>();
    private HashSet<PedestrianIntersectionController> subscribedControllers = new HashSet<PedestrianIntersectionController>();
    private PedestrianTrafficLightTrigger tlTrigger = null;
    private PedestrianIntersectionController tlController = null;

    [Header("NormalCrossing")]
    private List<Road> crossingRoads = new List<Road>();
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();

        if (target != null && isIndependent)
        {
            destination = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);
            invisiblePedestrian = Instantiate(invisiblePedestrianPrefab, transform.position, transform.rotation);
            invisiblePedestrian.SetDestination(destination);
            invisiblePedestrian.SetPedestrian(this);
            StartCoroutine(CheckArrivalToDestination());
        }
        else if (!isIndependent)
        {

        }
        baseAgentSpeed = agent.speed + Random.Range(-.2f, .2f);
        sprintAgentSpeed += Random.Range(-.2f, .2f);
    }
    void Update()
    {
        if (!isStoppedAtTrafficLight)
        {
            // todo
            //float distance = Vector3.Distance(target.position, transform.position);
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
        agent.SetDestination(destination);

        while (true)
        {
            yield return new WaitForSeconds(checkUpdateTime);
            float distance = Vector3.Distance(transform.position, destination);
            if(distance < 3f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f) 
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isIndependent) return;

        if (other.gameObject.CompareTag("TrafficLightPedestrianTrigger"))
        {
            HandleTrafficLightTrigger(other);
        }
    }
    private void HandleTrafficLightTrigger(Collider other)
    {
        tlTrigger = other.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
        if (tlTrigger != null)
        {
            tlController = tlTrigger.GetIntersectionController();
            if (intersectionControllers.Contains(tlController))
            {
                if (!subscribedControllers.Contains(tlController))
                {
                    crossingRotation = tlTrigger.transform.rotation;
                    // Suscribirse
                    tlController.SubscribeToLightChangeEvent(OnTrafficLightChange);
                    subscribedControllers.Add(tlController);
                    // Mirar si hay que parar o no
                    if (!tlController.IsPedestrianState())
                    {
                        // Detenerse
                        AssignSlot();
                    }
                    else
                    {
                        TrafficLightState state = tlController.GetState();
                        if (state == TrafficLightState.PedestrianRush)
                        {
                            // Randomize the probability of rushing
                            if (Random.value > 0.6f)
                            {
                                // Rush
                                float timeLeft = tlController.GetPedestrianTurnTimeLeft();
                                if (timeLeft < 5f && timeLeft > 3f)
                                {
                                    agent.speed = sprintAgentSpeed;
                                    mirrorSlot = CalculateOffsetFromBestSlot();
                                    StartCoroutine(OnCrossingEnabled());
                                }
                                else if (timeLeft <= 3f)
                                {
                                    AssignSlot();
                                }
                            }
                            else
                            {
                                AssignSlot();
                            }
                        }
                        else
                        {
                            mirrorSlot = CalculateOffsetFromBestSlot();
                            StartCoroutine(OnCrossingEnabled());
                        }
                    }
                }
                else
                {
                    tlController.UnsubscribeToLightChangeEvent(OnTrafficLightChange);
                    subscribedControllers.Remove(tlController);
                    agent.speed = baseAgentSpeed;
                }
            }
        }
    }
    private Vector3 CalculateOffsetFromBestSlot()
    {
        var centeredPos = tlTrigger.GetBestSlot().position + tlTrigger.transform.forward * Vector3.Distance(tlTrigger.transform.position, tlController.transform.position) * 1.5f;

        Quaternion previousRotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        transform.rotation = crossingRotation;

        Vector3 dir = (centeredPos - transform.position).normalized;
        Vector3 forward = transform.forward.normalized;

        float angle = Vector3.SignedAngle(forward, dir, Vector3.up);
        Vector3 offsetPos = new Vector3(centeredPos.x, centeredPos.y, centeredPos.z) - transform.right * angle * .05f;
        transform.rotation = previousRotation;
        return offsetPos;
    }
    private void AssignSlot()
    {
        assignedSlot = tlTrigger.GetSlotForPedestrian(this);
        mirrorSlot = assignedSlot.position + tlTrigger.transform.forward * Vector3.Distance(tlTrigger.transform.position, tlController.transform.position) * 1.5f;
        StartCoroutine(OnSlotAssigned());
    }
    public void ReassignSlot(Slot slot)
    {
        StopCoroutine(OnSlotAssigned());
        assignedSlot = slot;
        mirrorSlot = assignedSlot.position + tlTrigger.transform.forward * Vector3.Distance(tlTrigger.transform.position, tlController.transform.position) * 1.5f;
        StartCoroutine(OnSlotAssigned());
    }
    public void SetTLCrossings(List<PedestrianIntersectionController> _controllers)
    {
        intersectionControllers = _controllers;
    }
    public void SetCrossings(List<Road> _crossingRoads)
    {
        crossingRoads = _crossingRoads;
    }
    private void OnTrafficLightChange(TrafficLightState newColor, bool subscription)
    {
        switch (newColor)
        {
            case TrafficLightState.Pedestrian:
                StartMoving();
                break;

            case TrafficLightState.PedestrianRush:
                if(Random.value > 0.6f && GetDistanceToMirrorSlot() > 5f)
                {
                    agent.speed = sprintAgentSpeed;
                }
                break;

            case TrafficLightState.Red:
                if (isCrossing)
                {
                    agent.speed = sprintAgentSpeed;
                }
                else
                {
                    // Hay que ver si ya ten�a slot asignadooo
                    if (assignedSlot == null)
                    {
                        AssignSlot();
                    }
                }
                break;
            default:

                break;

        }
    }
    private float GetDistanceToMirrorSlot()
    {
        if(mirrorSlot == Vector3.zero)
        {
            var bestSlot = tlTrigger.GetBestSlot().position;
            mirrorSlot = bestSlot + tlTrigger.transform.forward * Vector3.Distance(tlTrigger.transform.position, tlController.transform.position) * 1.5f;
            return Vector3.Distance(transform.position, mirrorSlot);
        }
        else
        {
            return Vector3.Distance(transform.position, mirrorSlot);
        }
    }
    private IEnumerator OnSlotAssigned()
    {
        assignedSlot.isReserved = true;
        assignedSlot.isLocked = false;
        Vector3 alteredPos = new Vector3(
            assignedSlot.position.x + Random.Range(-0.15f, 0.15f),
            assignedSlot.position.y,
            assignedSlot.position.z + Random.Range(-0.15f, 0.15f)
        );
        agent.SetDestination(alteredPos);
        bool slotReached = false;
        while (!slotReached)
        {
            yield return new WaitForSeconds(0.1f);
            float distance = Vector3.Distance(transform.position, alteredPos);
            if (distance < 1f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f)
            {
                assignedSlot.isLocked = true;
                slotReached = true;
            }
        }
        StopMoving();
    }
    private void StartMoving()
    {
        assignedSlot.isReserved = false;
        assignedSlot.isLocked = false;
        tlTrigger.RemoveAssignation(this);
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
        animator.SetBool("IsMoving", true);
        assignedSlot = null;
        StartCoroutine(OnCrossingEnabled());
    }
    private IEnumerator OnCrossingEnabled()
    {
        agent.SetDestination(mirrorSlot);
        bool slotReached = false;
        while (!slotReached)
        {
            yield return new WaitForSeconds(0.2f);
            float distance = Vector3.Distance(transform.position, mirrorSlot);
            if (distance < 1.5f)
            {
                slotReached = true;
            }
        }
        mirrorSlot = Vector3.zero;
        agent.SetDestination(target.position);
    }
    public void StartMovingDependent()
    {
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
    }
    private void StopMoving()
    {
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
        animator.SetBool("IsMoving", false);
    }
    public void StopMovingDependent(Quaternion _crossingRotation)
    {
        crossingRotation = _crossingRotation;
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
        animator.SetBool("IsMoving", false);
    }
    private void MatchTrafficLightStopRotation()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, crossingRotation, 5f * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (mirrorSlot != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(mirrorSlot, new Vector3(.5f, .5f, .5f));
        }
    }

    public void OnEnterPedestrianCrossing(Road crossingRoad)
    {
        isCrossing = true;
        hasCrossed = false;
        if (crossingRoads.Contains(crossingRoad))
        {
            // Cruzar recto
            StartCoroutine(GoToMirrorCrossingPos());
        }
    }

    private IEnumerator GoToMirrorCrossingPos()
    {
        Vector3 newForward = Mathf.Abs(transform.forward.x) > 0.65f 
            ? new Vector3(Mathf.Sign(transform.forward.x), 0, 0)
            : new Vector3(0, 0, Mathf.Sign(transform.forward.z));
        Vector3 mirrorCrossingPos = transform.position + newForward * 5.5f;
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(mirrorCrossingPos, path))
        {
            if (agent.SetPath(path))
            {
                bool targetReached = false;
                while (!targetReached)
                {
                    yield return new WaitForSeconds(0.2f);
                    float distance = Vector3.Distance(transform.position, mirrorCrossingPos);
                    if (distance < .8f)
                    {
                        targetReached = true;
                    }
                }
                mirrorCrossingPos = Vector3.zero;
                agent.SetDestination(target.position);
            }
        }        
    }

    public void OnExitPedestrianCrossing()
    {
        if(isCrossing) { hasCrossed = true; }
        isCrossing = false;
    }
}
