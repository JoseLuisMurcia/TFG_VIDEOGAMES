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
        if (!navMeshSurface) return;
        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();
        Debug.Log("NavMesh has been built");
    }
}
