using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightTrigger : MonoBehaviour
{
    private PedestrianIntersectionController intersectionController;
    private TrafficLightWaitingSlotsManager slotsManager;
    private BoxCollider boxCollider;
    private bool debugTiers = true;
    private void Start()
    {
        if (!gameObject.CompareTag("IntersectionPedestrianTrigger")) return;
        boxCollider = GetComponent<BoxCollider>();
        Vector3 extents = boxCollider.bounds.extents;
        Vector3 center = boxCollider.bounds.center;
        Vector3 bottomLeft = new Vector3(
            center.x - (extents.x * transform.right.x),
            center.y - (extents.y * transform.up.y),
            center.z + (extents.z * transform.forward.z)
            );

        Vector3 bottomRight = new Vector3(
            center.x + (extents.x * transform.right.x),
            center.y - (extents.y * transform.up.y),
            center.z + (extents.z * transform.forward.z)
            );

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

    public List<Slot> GetSlotsForGroup(int numPedestrians)
    {
        return slotsManager.GetSlotsForGroup(numPedestrians);
    }

    public Slot GetSlotForPedestrian(Vector3 pedestrianPos)
    {
        return slotsManager.GetSlotForPedestrian(pedestrianPos);
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
            return;
            Gizmos.color = Color.cyan;
            foreach (var lane in slotsManager.GetSlots())
            {
                foreach(var slot in lane)
                {
                    Gizmos.DrawSphere(slot.position, .16f);
                }
            }
        }
    }
}
