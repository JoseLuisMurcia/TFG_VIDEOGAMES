using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNavigator : MonoBehaviour
{
    CarMovementAI AIController;
    public Waypoint currentWaypoint;
    private bool noMoreWaypoints = true;
    private void Awake()
    {
        AIController = GetComponent<CarMovementAI>();
    }
    void Start()
    {
        SetTargetToAI();
    }

    

    //void Update()
    //{
    //    if (noMoreWaypoints) return;

    //    if(AIController.GetTargetReached())
    //    {
    //        bool shouldBranch = false;
    //        // Randomization
    //        if(currentWaypoint.branches.Count > 0)
    //        {
    //            //shouldBranch = Random.Range(0f, 1f) <= currentWaypoint.branchRatio ? true : false;
    //            shouldBranch = true;
    //        }

    //        if(shouldBranch)
    //        {
    //            int branchId = Random.Range(0, currentWaypoint.branches.Count);
    //            currentWaypoint = currentWaypoint.branches[branchId];
    //        }
    //        else
    //        {
    //            currentWaypoint = currentWaypoint.nextWaypoint;
    //            if (currentWaypoint == null)
    //            {
    //                noMoreWaypoints = true;
    //                return;
    //            }
    //        }



    //        SetTargetToAI();
    //    }
    //}

    void Update()
    {
        if (noMoreWaypoints) return;

        if (AIController.GetTargetReached())
        {
            currentWaypoint = currentWaypoint.nextWaypoint;
            if (currentWaypoint == null)
            {
                noMoreWaypoints = true;
                return;
            }
            SetTargetToAI();
        }
    }

    private void SetTargetToAI()
    {
        if (currentWaypoint.nextWaypoint != null)
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), false);
        }
        else
        {
            AIController.SetTargetPosition(currentWaypoint.GetPosition(), true);
        }
    }
}
