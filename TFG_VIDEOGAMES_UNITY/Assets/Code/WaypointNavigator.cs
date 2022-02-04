using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CharacterNavigationController controller;
    public Waypoint currentWaypoint;
    private void Awake()
    {
        controller = GetComponent<CharacterNavigationController>();
    }
    void Start()
    {

    }

    void Update()
    {
        
    }
}
