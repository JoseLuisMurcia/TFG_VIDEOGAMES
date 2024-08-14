using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

public class InvisibleLeader : MonoBehaviour
{
    private PedestrianGroupMovement groupMovement;
    private NavMeshAgent agent;

    private Vector3 destination = Vector3.zero;

    [Header("Crossing")]
    public bool isCrossing = false;
    public bool isStoppedAtTrafficLight = false;
    private InvisiblePedestrian invisiblePedestrian = null;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;
    private Quaternion crossingRotation = Quaternion.identity;
    private List<PedestrianIntersectionController> intersectionControllers = new List<PedestrianIntersectionController>();
    private HashSet<PedestrianIntersectionController> subscribedControllers = new HashSet<PedestrianIntersectionController>();
    private PedestrianTrafficLightTrigger tlTrigger = null;
    private PedestrianIntersectionController tlController = null;

    private float baseAgentSpeed = 1f;
    private float sprintAgentSpeed = 1.7f;

    private List<Slot> assignedSlots = null;
    private Vector3 mirrorSlot = Vector3.zero;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (destination != Vector3.zero)
        {
            invisiblePedestrian = Instantiate(invisiblePedestrianPrefab, transform.position, transform.rotation);
            invisiblePedestrian.SetDestination(destination);
            invisiblePedestrian.SetLeader(this);
            StartCoroutine(GoToTarget());
        }
        baseAgentSpeed = agent.speed + Random.Range(-.2f, .2f);
        sprintAgentSpeed += Random.Range(-.2f, .2f);
    }

    IEnumerator GoToTarget()
    {
        yield return new WaitForSeconds(1f);
        agent.SetDestination(destination);
    }

    public void SetGroupMovement(PedestrianGroupMovement _groupMovement)
    {
        groupMovement = _groupMovement;
    }
    public void SetDestination(Vector3 _destination)
    {
        destination = _destination;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("IntersectionPedestrianTrigger"))
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
                            AssignSlots();
                        }
                        else
                        {
                            TrafficLightState state = tlController.GetState();
                            if (state == TrafficLightState.PedestrianRush)
                            {
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
                                        AssignSlots();
                                    }
                                }
                                else
                                {
                                    AssignSlots();
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
            Debug.Log("trigger entry");
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
    private void AssignSlots()
    {
        // Detenerse
        assignedSlots = tlTrigger.GetSlotsForGroup(this, groupMovement.groupSize - 1);
        mirrorSlot = groupMovement.GetAveragePositionFromSlots(assignedSlots) + tlTrigger.transform.forward * Vector3.Distance(tlTrigger.transform.position, tlController.transform.position) * 1.5f;
        assignedSlots.ForEach(slot => slot.isLocked = true);
        OnSlotsAssigned();
    }
    private void OnTrafficLightChange(TrafficLightState newColor, bool subscription)
    {
        switch (newColor)
        {
            case TrafficLightState.Pedestrian:
                StartMoving();
                break;

            case TrafficLightState.PedestrianRush:
                Debug.Log(GetDistanceToMirrorSlot());
                if (Random.value > 0.6f && GetDistanceToMirrorSlot() > 5f)
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
                    // Hay que ver si ya tenía slot asignadooo
                    if (assignedSlots == null)
                    {
                        AssignSlots();
                    }
                }
                break;
            default:

                break;

        }
    }
    private float GetDistanceToMirrorSlot()
    {
        if (mirrorSlot == Vector3.zero)
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
    private void OnSlotsAssigned()
    {
        Vector3 slotPosition = groupMovement.GetAveragePositionFromSlots(assignedSlots);
        agent.SetDestination(slotPosition);
        StartCoroutine(OnSlotAssigned(slotPosition));
        groupMovement.SetWaitingSlots(assignedSlots, crossingRotation);
    }
    private IEnumerator OnSlotAssigned(Vector3 slotPosition)
    {
        Vector3 alteredPos = new Vector3(
            slotPosition.x + Random.Range(-0.15f, 0.15f),
            slotPosition.y,
            slotPosition.z + Random.Range(-0.15f, 0.15f)
        );
        bool slotReached = false;
        while (!slotReached)
        {
            yield return new WaitForSeconds(0.1f);
            float distance = Vector3.Distance(transform.position, slotPosition);
            if (distance < 1f && agent.velocity.magnitude < .05f)
            {
                slotReached = true;
            }
        }
        StopMoving();
    }
    public void SetCrossings(List<PedestrianIntersectionController> _controllers)
    {
        intersectionControllers = _controllers;
    }

    private void StartMoving()
    {
        assignedSlots.ForEach(slot => { slot.isLocked = false; slot.isReserved = false; });
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
        assignedSlots = null;
        //agent.SetDestination(destination);
        groupMovement.Cross();
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
        agent.SetDestination(destination);
    }
    private void StopMoving()
    {
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
        MatchTrafficLightStopRotation();
    }

    public void MatchTrafficLightStopRotation()
    {
        transform.rotation = crossingRotation;
    }

    private void OnDrawGizmos()
    {
        if (mirrorSlot != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(mirrorSlot, new Vector3(.1f, .1f, .1f));
        }
    }
    public void OnEnterPedestrianCrossing()
    {
        isCrossing = true;
    }

    public void OnExitPedestrianCrossing()
    {
        isCrossing = false;
    }
}
