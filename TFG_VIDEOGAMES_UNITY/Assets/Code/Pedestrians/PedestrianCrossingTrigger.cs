using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrossingTrigger : MonoBehaviour
{
    private Road parentRoad = null;
    private void Start()
    {
        parentRoad = GetComponentInParent<Road>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pedestrian"))
        {
            Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
            InvisibleLeader leader = other.gameObject.GetComponent<InvisibleLeader>();

            if (pedestrian != null)
            {
                pedestrian.OnEnterPedestrianCrossing(parentRoad);
            }
            else if (leader != null)
            {
                leader.OnEnterPedestrianCrossing(parentRoad);
            }
        }    
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Pedestrian"))
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
}
