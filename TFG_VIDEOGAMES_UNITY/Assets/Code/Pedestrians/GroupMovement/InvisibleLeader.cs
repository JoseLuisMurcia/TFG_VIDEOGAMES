using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class InvisibleLeader : MonoBehaviour
{
    private PedestrianGroupMovement groupMovement;
    private NavMeshAgent agent;

    private Vector3 destination = Vector3.zero;
    float checkUpdateTime = 1.5f;

    [Header("Crossing")]
    public bool isCrossing = false;
    public bool isStoppedAtTrafficLight = false;
    private InvisiblePedestrian invisiblePedestrian = null;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;
    private List<PedestrianIntersectionController> intersectionControllers = new List<PedestrianIntersectionController>();
    private List<Slot> assignedSlots = null;
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
            var trigger = other.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
            if (trigger != null)
            {
                var controller = trigger.GetIntersectionController();
                if (intersectionControllers.Contains(controller))
                {
                    // Suscribirse
                    controller.SubscribeToLightChangeEvent(OnTrafficLightChange);
                    // Mirar si hay que parar o no
                    if (controller.GetState() != TrafficLightState.Pedestrian)
                    {
                        // Detenerse
                        assignedSlots = trigger.GetSlotsForGroup(groupMovement.groupSize);
                        OnSlotsAssigned();
                        //StartCoroutine(OnSlotAssigned());
                        //StopMoving();
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
    private void OnSlotsAssigned()
    {
        //pedestrianGroup.SetWaitingSlots(assignedSlots);
        Vector3 slotPosition = groupMovement.GetLeaderPositionFromSlots(assignedSlots);
        agent.SetDestination(slotPosition);
        StartCoroutine(OnSlotAssigned(slotPosition));
    }
    private IEnumerator OnSlotAssigned(Vector3 slotPosition)
    {
        // Probar a hacer el movimiento del grupo desde el lider y no individual...
        // Calcular la posicion del lider en funcion de los slots debería hacer que mantengan la formación y sea natural
        // Ingenieria inversa, sacar la posicion del lider a partir de la posicion de los slots.

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
    public void OnSlotsReached()
    {

    }
    public void SetCrossings(List<PedestrianIntersectionController> _controllers)
    {
        intersectionControllers = _controllers;
    }

    private void StartMoving()
    {
        assignedSlots.ForEach(assignedSlot => assignedSlot.isLocked = false);
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
        assignedSlots = null;
        agent.SetDestination(destination);
    }

    private void StopMoving()
    {
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
    }
}
