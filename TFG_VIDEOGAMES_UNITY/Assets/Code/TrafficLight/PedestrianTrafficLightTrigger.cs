using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightTrigger : MonoBehaviour
{
    private PedestrianIntersectionController intersectionController;
    private TrafficLightWaitingSlotsManager slotsManager;
    [SerializeField] private bool debugTiers = false;
    private void Start()
    {
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

    public List<Slot> GetSlotsForGroup(InvisibleLeader leader, int numPedestrians)
    {
        return slotsManager.GetSlotsForGroup(leader, numPedestrians);
    }

    public Slot GetSlotForPedestrian(Pedestrian pedestrian)
    {
        return slotsManager.GetSlotForPedestrian(pedestrian);
    }
    public void RemoveAssignation(Pedestrian pedestrian)
    {
        slotsManager.RemoveAssignation(pedestrian);
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
                        if (slot.isReserved && !slot.isLocked)
                        {
                            Gizmos.color = Color.green;
                        }
                        else if (slot.isLocked)
                        {
                            Gizmos.color = Color.red;
                        }
                        else
                        {
                            Gizmos.color = Color.cyan;
                        }
                        Gizmos.DrawSphere(slot.position, .16f);
                    }
                }
            }           
        }
    }
}
