using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianTrafficLightTrigger : MonoBehaviour
{
    private PedestrianIntersectionController intersectionController;
    private TrafficLightWaitingSlots waitingSlots;
    private BoxCollider boxCollider;
    private void Start()
    {
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
        waitingSlots = new TrafficLightWaitingSlots(bottomLeft, bottomRight, transform.forward, transform.right);
    }
    public PedestrianIntersectionController GetIntersectionController()
    {
        return intersectionController;
    }


    public void SetIntersectionController(PedestrianIntersectionController _intersectionController)
    {
        intersectionController = _intersectionController;
    }

    private void OnDrawGizmos()
    {
        if (waitingSlots != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var lane in waitingSlots.GetSlots())
            {
                foreach(var slot in lane)
                {
                    Gizmos.DrawSphere(slot.position, .2f);
                }
            }
        }
    }
}
