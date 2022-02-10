using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    Graph graph;
    void Awake()
    {
        graph = GetComponent<Graph>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
