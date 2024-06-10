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
    [SerializeField] int groupSize;
    float spacing = .55f;
    float closeSpacing = .3f;

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

        // Spawn pedestrians according to the leader
        var positions = GetPositions();
        for (int i = 0; i < groupSize; i++)
        {
            int pIndex = Random.Range(0, pedestrianPrefabs.Count);
            Pedestrian pedestrian = Instantiate(pedestrianPrefabs[pIndex], positions[i], Quaternion.identity, transform);
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
            case Formation.CloseAbreast: return GetCloseAbreastPositions();
            case Formation.Lane: return GetLanePositions();
            case Formation.Wedge: return GetWedgePositions();
            case Formation.InverseWedge: return GetInverseWedgePositions();
        }
       
        return positions;
    }
    private List<Vector3> GetAbreastPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        float totalWidth = (groupSize - 1) * spacing;
        float startX = leader.transform.position.x - totalWidth / 2;

        for (int i = 0; i < groupSize; i++)
        {
            float xOffset = startX + i * spacing;
            Vector3 position = new Vector3(xOffset, leader.transform.position.y, leader.transform.position.z);
            positions.Add(position);
        }

        return positions;
    }
    private List<Vector3> GetCloseAbreastPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        float totalWidth = (groupSize - 1) * spacing;
        float startX = leader.transform.position.x - totalWidth / 2;

        for (int i = 0; i < groupSize; i++)
        {
            float xOffset = startX + i * closeSpacing;
            Vector3 position = new Vector3(xOffset, leader.transform.position.y, leader.transform.position.z);
            positions.Add(position);
        }

        return positions;
    }
    private List<Vector3> GetLanePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        float totalWidth = (groupSize - 1) * spacing;
        float startZ = leader.transform.position.z - totalWidth / 2;

        for (int i = 0; i < groupSize; i++)
        {
            float zOffset = startZ + i * spacing;
            Vector3 position = new Vector3(leader.transform.position.x, leader.transform.position.y, zOffset);
            positions.Add(position);
        }

        return positions;
    }
    private List<Vector3> GetWedgePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        // Two pedestrians behind and to the sides
        Vector3 leftPosition = new Vector3(leader.transform.position.x - spacing, leader.transform.position.y, leader.transform.position.z - spacing);
        Vector3 rightPosition = new Vector3(leader.transform.position.x + spacing, leader.transform.position.y, leader.transform.position.z - spacing);

        positions.Add(leftPosition);
        positions.Add(rightPosition);

        // Leader at the front
        positions.Add(leader.transform.position);

        return positions;
    }
    private List<Vector3> GetInverseWedgePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        // Two pedestrians behind and to the sides
        Vector3 leftPosition = new Vector3(leader.transform.position.x - spacing, leader.transform.position.y, leader.transform.position.z + spacing);
        Vector3 rightPosition = new Vector3(leader.transform.position.x + spacing, leader.transform.position.y, leader.transform.position.z + spacing);

        positions.Add(leftPosition);
        positions.Add(rightPosition);

        // Leader at the front
        positions.Add(leader.transform.position);

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
            Gizmos.DrawSphere(position, .2f);
        }
    }

    public enum Formation
    {
        Abreast,
        CloseAbreast,
        Wedge,
        InverseWedge,
        Lane
    }
}
