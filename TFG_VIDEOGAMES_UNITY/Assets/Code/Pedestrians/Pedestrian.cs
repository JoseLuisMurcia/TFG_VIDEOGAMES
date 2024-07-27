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
    private Vector3 mirrorSlot = Vector3.zero;

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
            if(distance < 3f && !animator.GetBool("IsMoving") && agent.velocity.magnitude < .05f) 
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
                        assignedSlot = trigger.GetSlotForPedestrian(transform.position);
                        mirrorSlot = assignedSlot.position + trigger.transform.forward * Vector3.Distance(trigger.transform.position, controller.transform.position) * 1.5f;
                        StartCoroutine(OnSlotAssigned());
                    }

                    // TODO
                    /* Aquí habria que mirar cuanto tiempo queda para que se ponga en rojo
                       Al suscribirte, quieres comprobar si == Pedestrian y cuanto tiempo le queda, porque en el caso de estar ya parpadeando
                       sería inteligente no cruzar. 
                       En TrafficLightScheduler o bien creo un nuevo estado para el parpadeo o hago publico el tiempo que queda hasta que se ponga rojo (lo mismo pero con numeros)
                        
                    */
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
                StartMoving();
                break;

            // TODO
            /* Casuística: Pedestrian se suscribe cuando estado == Pedestrian, pero justo se pone rojo, no recibe notificación
             * Debe saber pararse, tambien debo poder controlar si está cruzando ya o no. Si está cruzando mientras se pone rojo, 100% debe ir más rapido
             * El pedestrian tiene que saber que está corriendo o andando, esto en el GTA V lo hacían (aunque mal).
             */
            default:

                break;

        }
    }

    private IEnumerator OnSlotAssigned()
    {
        assignedSlot.isLocked = true;
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

    //private void OnDrawGizmos()
    //{
    //    if(mirrorSlot != Vector3.zero)
    //    {
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawCube(mirrorSlot, new Vector3(.1f, .1f, .1f));
    //    }
    //}
}
