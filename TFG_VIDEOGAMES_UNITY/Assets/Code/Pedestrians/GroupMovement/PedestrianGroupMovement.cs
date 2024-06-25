using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class PedestrianGroupMovement : MonoBehaviour
{
    [SerializeField] List<Pedestrian> pedestrianPrefabs;
    [SerializeField] GameObject target;
    [SerializeField] public int groupSize;
    float horizontalSpacing = .50f;
    float closeHorizontalSpacing = .3f;
    float verticalSpacing = .4f;
    float closeVerticalSpacing = .25f;

    private List<Pedestrian> pedestrians = new List<Pedestrian>();
    private List<NavMeshAgent> pedestriansAgents = new List<NavMeshAgent>();
    [SerializeField] private InvisibleLeader leaderPrefab;
    private InvisibleLeader leader = null;
    [SerializeField] private Formation formation = Formation.Lane;
    private DistanceHandler distanceHandler;

    // Crossing
    private bool isWaitingInSlot = false;
    private List<SlotAsignation> slotAsignations;

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
        return CalculatePositions(offset, leader.transform.right);
    }
    private List<Vector3> GetLanePositions()
    {
        float offset = formation == Formation.Lane ? verticalSpacing : closeVerticalSpacing;
        return CalculatePositions(offset, leader.transform.forward);
    }
    private List<Vector3> CalculatePositions(float offset, Vector3 direction)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 startPos = leader.transform.position;

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
    public Vector3 GetLeaderPositionFromSlots(List<Slot> slots)
    {
        if(formation != Formation.Lane && formation != Formation.CloseLane)
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
        if (isWaitingInSlot) return;

        // Move followers to maintain formation
        var positions = GetPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 targetPosition = positions[i];
            pedestriansAgents[i].SetDestination(targetPosition);
            distanceHandler.CheckDistance(pedestriansAgents[i], targetPosition);
        }
    }
    public void SetWaitingSlots(List<Slot> slots)
    {
        // Crear asignaciones por posiciones
        slotAsignations = new List<SlotAsignation>();
        // Hay que tener en cuenta la formación, si están abroad (que es lo normal)
        // hay que asignar las posiciones de forma que sigan al lado...
        if(formation == Formation.Lane || formation == Formation.CloseLane)
        {
            // Asignar según distancia, al que está más cerca se le manda más lejos
        }
        else
        {
            // Asignar para mantener formacion
            for (int i = 0; i < groupSize; i++)
            {
                Slot slotWithLowestId = slots.OrderBy(slot => slot.id).FirstOrDefault();
                slotAsignations.Add(new SlotAsignation(pedestriansAgents[i], slotWithLowestId));
                slots.Remove(slotWithLowestId);
            }
        }
        
        StartCoroutine(ReachSlots());
    }
    private IEnumerator ReachSlots()
    {
        List<NavMeshAgent> pedestriansNotPositioned = new List<NavMeshAgent>(pedestriansAgents);
        List<SlotAsignation> assignations = new List<SlotAsignation>(slotAsignations);
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            int numSlotsOccupied = 0;
            for (int i = 0; i < slotAsignations.Count; i++)
            {
                float distance = Vector3.Distance(pedestriansAgents[i].transform.position, slotAsignations[i].slot.position);
                if (distance < 0.3f)
                {
                    numSlotsOccupied++;
                }
            }
            if (numSlotsOccupied == assignations.Count)
                break;
        }

    }
    private void OnDrawGizmos()
    {
        if (!leader) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(target.transform.position, .35f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(leader.transform.position, .25f);
        foreach (var position in GetPositions())
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, .1f);
        }
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
