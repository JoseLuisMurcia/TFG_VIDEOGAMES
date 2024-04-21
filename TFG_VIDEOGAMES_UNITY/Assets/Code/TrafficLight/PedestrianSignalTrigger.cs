using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSignalTrigger : MonoBehaviour
{
    public PedestrianCrossingSignal signal;

    private void OnTriggerEnter(Collider other)
    {
        WhiskersManager carManager = other.GetComponent<WhiskersManager>();
        if (carManager != null)
        {
            Vector3 carForward = carManager.transform.forward;
            Vector3 carPos = carManager.transform.position;
            Vector3 signalPos = signal.transform.position;
            Vector3 dirFromCarToSignal = (signalPos - carPos).normalized;
            float angleFromCarToSignal = Vector3.Angle(carForward, dirFromCarToSignal);

            if (angleFromCarToSignal < 45f)
            {
                // Tell the priorityBehavior what it needs
                carManager.pedestrianCrossingInSight = true; ;
            }
        }
    }
}
