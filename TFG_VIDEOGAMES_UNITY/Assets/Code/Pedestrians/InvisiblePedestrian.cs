using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class InvisiblePedestrian : MonoBehaviour
{
    public NavMeshAgent agent;

    private Vector3 destination = Vector3.zero;
    float checkUpdateTime = 1f;
    private HashSet<PedestrianIntersectionController> controllers = new HashSet<PedestrianIntersectionController>();
    private Pedestrian pedestrian = null;
    private InvisibleLeader invisibleLeader = null;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (destination != Vector3.zero)
        {
            StartCoroutine(GoToTarget());
        }
    }

    public void SetDestination(Vector3 _destination)
    {
        destination = _destination;
    }
    public void SetPedestrian(Pedestrian _pedestrian)
    {
        pedestrian = _pedestrian;
    }
    public void SetLeader(InvisibleLeader _leader)
    {
        invisibleLeader = _leader;
    }
    IEnumerator GoToTarget()
    {
        yield return new WaitForSeconds(1f);
        agent.SetDestination(destination);
        StartCoroutine(CheckArrivalToDestination());
    }
    IEnumerator CheckArrivalToDestination()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkUpdateTime);
            float distance = Vector3.Distance(transform.position, destination);
            if (distance < 1.5f)
            {
                if (pedestrian)
                {
                    pedestrian.SetCrossings(controllers.ToList());
                }
                else
                {
                    invisibleLeader.SetCrossings(controllers.ToList());
                }
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("InvisiblePedestrianTrigger"))
        {
            var trigger = other.gameObject.GetComponent<PedestrianTrafficLightTrigger>();
            if (trigger != null)
            {
                controllers.Add(trigger.GetIntersectionController());
            }
        }

    }
}
