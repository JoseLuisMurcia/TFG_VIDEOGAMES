using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class PedestrianSignalTrigger : MonoBehaviour
{
    public PedestrianCrossingSignal signal;
    private Vector3 crossingPos = Vector3.zero;

    public void Start()
    {
        crossingPos = signal.transform.parent.position;
    }
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
            float dot = Vector3.Dot(carForward, dirFromCarToSignal);
            if (angleFromCarToSignal < 45f && dot > 0f)
            {
                // Tell the priorityBehavior what it needs
                carManager.pedestrianCrossingInSight = true;
                carManager.SetCrossingPos(crossingPos);
            }
        }
    }
}
