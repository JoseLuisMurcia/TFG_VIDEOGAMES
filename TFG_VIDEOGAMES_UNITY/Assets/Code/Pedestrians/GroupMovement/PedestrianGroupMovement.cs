using System.Collections;
using System.Collections.Generic;
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

    void Start()
    {
        distanceHandler = new DistanceHandler();
        // Create the invisible leader
        leader = Instantiate(leaderPrefab, transform.position, Quaternion.identity, transform);
        leader.SetDestination(target.transform.position);

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
        List<Vector3> positions = new List<Vector3>();

        Vector3 startPos = leader.transform.position;

        float offset = formation == Formation.Abreast ? horizontalSpacing : closeHorizontalSpacing;
        // Determine the start offset to center the group around the leader
        int halfGroupSize = (groupSize - 1) / 2;

        for (int i = -halfGroupSize; i <= halfGroupSize; i++)
        {
            Vector3 position = startPos + leader.transform.right * i * offset;
            positions.Add(position);
        }

        return positions;
    }
    private List<Vector3> GetLanePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        Vector3 startPos = leader.transform.position;

        float offset = formation == Formation.Abreast ? verticalSpacing : closeVerticalSpacing;
        // Determine the start offset to center the group around the leader
        int halfGroupSize = (groupSize - 1) / 2;

        for (int i = -halfGroupSize; i <= halfGroupSize; i++)
        {
            Vector3 position = startPos + leader.transform.forward * i * offset;
            positions.Add(position);
        }

        return positions;
    }
    void RemoveRandomPedestrianFromFormation()
    {


    }
    void RemovePedestrianFromFormation(Pedestrian pedestrian)
    {

    }
    void Update()
    {
        // Move followers to maintain formation
        var positions = GetPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 targetPosition = positions[i];
            pedestriansAgents[i].SetDestination(targetPosition);
            distanceHandler.CheckDistance(pedestriansAgents[i], targetPosition);
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
