using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrossingTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        InvisibleLeader leader = other.gameObject.GetComponent<InvisibleLeader>();

        if (pedestrian != null)
        {
            pedestrian.OnEnterPedestrianCrossing();
        }
        else if (leader != null)
        {
            leader.OnEnterPedestrianCrossing();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        InvisibleLeader leader = other.gameObject.GetComponent<InvisibleLeader>();

        if (pedestrian != null)
        {     
            pedestrian.OnExitPedestrianCrossing();
        }
        else if (leader != null)
        {
            leader.OnExitPedestrianCrossing();
        }
    }
}
