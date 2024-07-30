using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrossingTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        if (pedestrian != null)
        {
            pedestrian.OnEnterPedestrianCrossing(transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        if (pedestrian != null)
        {     
            pedestrian.OnExitPedestrianCrossing();
        }
    }
}
