using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianGroupMovement : MonoBehaviour
{
    [SerializeField] List<Pedestrian> pedestrianPrefabs;
    [SerializeField] GameObject target;
    [SerializeField] public int groupSize;
    float horizontalSpacing = .50f;
    float closeHorizontalSpacing = .35f;
    float verticalSpacing = .4f;
    float closeVerticalSpacing = .25f;

    private List<Pedestrian> pedestrians = new List<Pedestrian>();
    private List<NavMeshAgent> pedestriansAgents = new List<NavMeshAgent>();
    [SerializeField] private InvisibleLeader leaderPrefab;
    private InvisibleLeader leader = null;
    [SerializeField] private Formation formation = Formation.Lane;
    private DistanceHandler distanceHandler;
    // Crossing
    private bool reachingSlots = false;
    private bool isWaitingInSlot = false;
    private List<Vector3> waitingPositions;
    Quaternion crossingRotation = Quaternion.identity;

    void Start()
    {
        distanceHandler = new DistanceHandler();
        // Create the invisible leader
        leader = Instantiate(leaderPrefab, transform.position, Quaternion.identity, transform);
        leader.SetDestination(target.transform.position);
        leader.SetGroupMovement(this);
        leader.transform.LookAt(target.transform.position);

        // Spawn pedestrians according to the leader
        var positions = GetPositions();
        for (int i = 0; i < groupSize; i++)
        {
            int pIndex = Random.Range(0, pedestrianPrefabs.Count);
            Pedestrian pedestrian = Instantiate(pedestrianPrefabs[pIndex], positions[i], Quaternion.identity, transform);
            pedestrian.transform.LookAt(target.transform.position);
            pedestrians.Add(pedestrian);
            pedestrianPrefabs.RemoveAt(pIndex);
            pedestrian.isIndependent = false;
            pedestriansAgents.Add(pedestrian.GetComponent<NavMeshAgent>());
        }
    }
    public void SetFormation(Formation _formation)
    {
        formation = _formation;
    }

    List<Vector3> GetPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        switch (formation)
        {
            case Formation.Abreast: return GetAbreastPositions();
            case Formation.CloseAbreast: return GetAbreastPositions();
            case Formation.Lane: return GetLanePositions();
            case Formation.CloseLane: return GetLanePositions();
            case Formation.Wedge: return GetAbreastPositions();
            case Formation.InverseWedge: return GetAbreastPositions();
        }

        return positions;
    }
    private List<Vector3> GetAbreastPositions()
    {
        float offset = formation == Formation.Abreast ? horizontalSpacing : closeHorizontalSpacing;
        return CalculatePositions(offset, leader.transform.right, leader.transform.position);
    }
    private List<Vector3> GetLanePositions()
    {
        float offset = formation == Formation.Lane ? verticalSpacing : closeVerticalSpacing;
        return CalculatePositions(offset, leader.transform.forward, leader.transform.position);
    }
    private List<Vector3> CalculatePositions(float offset, Vector3 direction, Vector3 referencePos)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 startPos = referencePos;

        // Calculate half group size and an offset for centering
        int halfGroupSize = groupSize / 2;

        for (int i = -halfGroupSize; i <= halfGroupSize; i++)
        {
            Vector3 position;
            // For even group sizes, shift positions to the right to avoid gaps
            if (groupSize % 2 == 0)
            {
                position = startPos + (direction * ((i + 0.5f) * offset));
            }
            else
            {
                position = startPos + (direction * (i * offset));
            }
            positions.Add(position);
        }

        // If the group size is even, we need to remove the additional position at the end
        if (groupSize % 2 == 0)
        {
            positions.RemoveAt(positions.Count - 1);
        }

        return positions;
    }
    public Vector3 GetAveragePositionFromSlots(List<Slot> slots)
    {
        if (formation != Formation.Lane && formation != Formation.CloseLane)
        {
            Vector3 sum = slots.Aggregate(Vector3.zero, (acc, slot) => acc + slot.position);
            return (sum / slots.Count);
        }
        return Vector3.zero;
    }
    void RemoveRandomPedestrianFromFormation()
    {


    }
    void RemovePedestrianFromFormation(Pedestrian pedestrian)
    {

    }
    void Update()
    {
        if (isWaitingInSlot || reachingSlots)
        {
            // Match rotation
            //leader.MatchTrafficLightStopRotation();
            return;
        }

        // Move followers to maintain formation
        var positions = GetPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 targetPosition = positions[i];
            pedestriansAgents[i].SetDestination(targetPosition);
            distanceHandler.CheckDistance(pedestriansAgents[i], targetPosition);
        }
    }
    public void SetWaitingSlots(List<Slot> slots, Quaternion rotation)
    {
        crossingRotation = rotation;
        reachingSlots = true;
        // Asignar para mantener formacion
        // M�s facil, y si calculo el punto medio de los slots y asigno esa posicion al lider y en funcion del lider calculo la del resto?
        Vector3 leaderWaitingPos = GetAveragePositionFromSlots(slots);
        leader.MatchTrafficLightStopRotation();
        Vector3 leaderRight = new Vector3(leader.transform.right.x, leader.transform.right.y, leader.transform.right.z);
        waitingPositions = CalculatePositions(closeHorizontalSpacing, leaderRight, leaderWaitingPos);
        StartCoroutine(ReachSlots());
    }
    private IEnumerator ReachSlots()
    {
        for (int i = 0; i < waitingPositions.Count; i++)
        {
            pedestriansAgents[i].SetDestination(waitingPositions[i]);
        }
        while (reachingSlots)
        {
            yield return new WaitForSeconds(0.3f);
            if (!reachingSlots) yield break;
            int numSlotsOccupied = 0;
            for (int i = 0; i < waitingPositions.Count; i++)
            {
                float distance = Vector3.Distance(pedestriansAgents[i].transform.position, waitingPositions[i]);
                if (distance < 0.3f && !pedestriansAgents[i].isStopped)
                {
                    pedestriansAgents[i].isStopped = true;
                    numSlotsOccupied++;
                    StartCoroutine(RotatePedestrian(pedestriansAgents[i]));
                }
            }
            if (numSlotsOccupied == waitingPositions.Count)
                reachingSlots = false;
        }
        isWaitingInSlot = true;
    }

    private IEnumerator RotatePedestrian(NavMeshAgent agent)
    {
        yield return new WaitForSeconds(.2f);
        MatchTrafficLightStopRotation(agent);
    }
    public void Cross()
    {
        isWaitingInSlot = false;
        reachingSlots = false;
        pedestriansAgents.ForEach(agent => agent.isStopped = false);
        waitingPositions = null;
    }
    private void OnDrawGizmos()
    {
        if (!leader) return;

        //Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(target.transform.position, .3f);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(leader.transform.position, .2f);
        foreach (var position in GetPositions())
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, .1f);
        }

        if(waitingPositions != null)
        {
            foreach (var position in waitingPositions)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(position, .1f);
            }
        }
    }
    private void MatchTrafficLightStopRotation(NavMeshAgent agent)
    {
        //agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, crossingRotation, 1f);
        agent.transform.rotation = crossingRotation;
        Debug.Log("rotation: " + agent.transform.rotation);
    }

    public enum Formation
    {
        Abreast,
        CloseAbreast,
        Wedge,
        InverseWedge,
        Lane,
        CloseLane
    }
}
