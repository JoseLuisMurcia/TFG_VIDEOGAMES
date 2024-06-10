using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class InvisibleLeader : MonoBehaviour
{
    private NavMeshAgent agent;

    private Vector3 destination = Vector3.zero;
    float checkUpdateTime = 1.5f;

    private PedestrianGroupMovement pedestrianGroup = null;

    [Header("Crossing")]
    public bool isCrossing = false;
    public bool isStoppedAtTrafficLight = false;
    private InvisiblePedestrian invisiblePedestrian = null;
    [SerializeField] InvisiblePedestrian invisiblePedestrianPrefab;

    private List<PedestrianIntersectionController> intersectionControllers = new List<PedestrianIntersectionController>();
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
        //StartCoroutine(CheckArrivalToDestination());
    }
    IEnumerator CheckArrivalToDestination()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkUpdateTime);
            float distance = Vector3.Distance(transform.position, destination);
            if (distance < 3.5f && agent.velocity.magnitude < .05f)
            {
                Destroy(gameObject);
            }
        }
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
                    if (controller.GetState() == TrafficLightState.Pedestrian)
                    {
                        // Cruzar
                    }
                    else
                    {
                        // Detenerse
                        StopMoving();
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
    public void SetCrossings(List<PedestrianIntersectionController> _controllers)
    {
        intersectionControllers = _controllers;
    }

    private void StartMoving()
    {
        isStoppedAtTrafficLight = false;
        agent.isStopped = false;
    }

    private void StopMoving()
    {
        isStoppedAtTrafficLight = true;
        agent.isStopped = true;
    }
}
