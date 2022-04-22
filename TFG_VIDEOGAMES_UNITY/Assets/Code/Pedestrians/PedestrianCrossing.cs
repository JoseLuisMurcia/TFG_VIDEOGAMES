using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrossing : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        if (pedestrian != null)
        {
            pedestrian.isCrossing = true;
            pedestrian.crossingPos = transform.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Pedestrian pedestrian = other.gameObject.GetComponent<Pedestrian>();
        if (pedestrian != null)
        {
            pedestrian.isCrossing = false;
            pedestrian.crossingPos = Vector3.zero;
        }   
    }
}
