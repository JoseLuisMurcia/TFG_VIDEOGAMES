using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class DistanceHandler
{
    private float normalSpeed = 1f;
    private float maxSpeed = 1.3f;
    private float catchUpThreshold = .3f; // Distance threshold to start catching up
    public void CheckDistance(NavMeshAgent agent, Vector3 target)
    {
        float distanceToTarget = Vector3.Distance(agent.transform.position, target);

        if (distanceToTarget > catchUpThreshold)
        {
            // Increase speed proportionally to the distance to catch up
            agent.speed = Mathf.Lerp(normalSpeed, maxSpeed, (distanceToTarget - catchUpThreshold) / catchUpThreshold);
        }
        else
        {
            // Return to normal speed when close to the target
            agent.speed = normalSpeed;
        }
    }
}
