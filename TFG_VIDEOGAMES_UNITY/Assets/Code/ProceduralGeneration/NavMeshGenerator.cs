using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshGenerator : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;
    private void Awake()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
    }
    public void BakeNavMesh()
    {
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();
    }
}
