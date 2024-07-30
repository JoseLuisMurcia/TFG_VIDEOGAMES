using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightTrigger : MonoBehaviour
{
    private PedestrianIntersectionController intersectionController;
    private TrafficLightWaitingSlotsManager slotsManager;
    private bool debugTiers = false;
    private void Start()
    {
        if (!gameObject.CompareTag("IntersectionPedestrianTrigger")) return;
        Vector3 bottomLeft = Vector3.zero;
        Vector3 bottomRight = Vector3.zero;

        foreach (Transform child in transform)
        {
            if(child.gameObject.name == "UR")
            {
                bottomRight = child.transform.position;
            }
            else
            {
                bottomLeft = child.transform.position;
            }
        }

        slotsManager = new TrafficLightWaitingSlotsManager(bottomLeft, bottomRight, transform.forward, transform.right, transform.parent.position);
    }
    public PedestrianIntersectionController GetIntersectionController()
    {
        return intersectionController;
    }


    public void SetIntersectionController(PedestrianIntersectionController _intersectionController)
    {
        intersectionController = _intersectionController;
    }

    public List<Slot> GetSlotsForGroup(Vector3 groupPos, int numPedestrians)
    {
        return slotsManager.GetSlotsForGroup(groupPos, numPedestrians);
    }

    public Slot GetSlotForPedestrian(Vector3 pedestrianPos)
    {
        return slotsManager.GetSlotForPedestrian(pedestrianPos);
    }

    public Slot GetBestSlot()
    {
        return slotsManager.GetBestSlot();
    }
    private void OnDrawGizmos()
    {
        if (slotsManager != null)
        {
            if (debugTiers)
            {
                Gizmos.color = Color.cyan;
                foreach (var lane in slotsManager.GetSlots())
                {
                    foreach (var slot in lane)
                    {
                        Gizmos.DrawSphere(slot.position, .15f + slot.tier * .04f);
                    }
                }
            }
            else
            {
                foreach (var lane in slotsManager.GetSlots())
                {
                    foreach (var slot in lane)
                    {
                        Gizmos.color = slot.isLocked ? Color.red : Color.cyan;
                        Gizmos.DrawSphere(slot.position, .16f);
                    }
                }
            }           
        }
    }
}
