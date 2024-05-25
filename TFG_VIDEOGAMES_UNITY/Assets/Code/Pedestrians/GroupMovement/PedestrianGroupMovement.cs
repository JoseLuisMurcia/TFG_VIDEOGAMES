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
    int groupSize = 4;
    float spacing = .4f;
    float spawnSpacing = 1f;

    private List<Pedestrian> pedestrians = new List<Pedestrian>();
    private List<NavMeshAgent> pedestriansAgents = new List<NavMeshAgent>();
    [SerializeField] private InvisibleLeader leaderPrefab;
    private InvisibleLeader leader = null;
    private Formation formation = Formation.Abreast;

    void Start()
    {
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

        if (formation == Formation.Abreast)
        {
            return GetAbreastPositions();
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
    void Update()
    {
        // Move followers to maintain formation
        var positions = GetPositions();
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 targetPosition = positions[i];
            pedestriansAgents[i].SetDestination(targetPosition);
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
        Wedge,
        Lane
    }
}
