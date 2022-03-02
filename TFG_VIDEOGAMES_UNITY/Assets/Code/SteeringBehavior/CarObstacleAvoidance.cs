using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class could be better if it included node information, dont put a navigation node too close to the border if possible or in a not navigable part.
// With the grid, we can find the node in that position and check if it is walkable and the movement penalty
public class CarObstacleAvoidance : MonoBehaviour
{
    [SerializeField] Transform centerSensor, leftSensor, rightSensor;
    [SerializeField] LayerMask obstacleLayer;
    public bool objectHit = false;
    private PathFollower pathFollower;
    [SerializeField] float rayDistance = 0.75f;
    [SerializeField] float sideReach = 1.5f;
    [SerializeField] float centerReach = 4f;
    [SerializeField] Grid grid;

    void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }

    // Update is called once per frame
    void Update()
    {
        if (objectHit) return;
        Vector3 position = centerSensor.position;
        Ray ray = new Ray(position, leftSensor.forward);
        RaycastHit hit;
        
        // Si se quiere hacer para evitar objetos en movimiento, se debería calcular la trayectoria de ese movimiento para ver si la trayectoria resultaría en una colisión.

        // If the object is hit with the left ray, create a position to go to the right
        if (Physics.Raycast(ray, out hit, sideReach * rayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(position, hit.point, Color.red);
            Vector3 newPoint = hit.point + transform.right;
            pathFollower.SetNewPathByAvoidance(newPoint);
        }
        else
        {
            Debug.DrawLine(position, position + leftSensor.forward, Color.blue);
        }

        // If the object is hit with the middle ray first, go left or right, it does not matter.
        ray = new Ray(position, centerSensor.forward);
        if (Physics.Raycast(ray, out hit, centerReach * rayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(position, hit.point, Color.red);

            Vector3 hitPoint = hit.point;
            Vector3 rightPos = hitPoint + transform.right;
            Vector3 leftPos = hitPoint - transform.right;
            pathFollower.SetNewPathByAvoidance(GetBestCandidate(leftPos,rightPos));
        }
        else
        {
            Debug.DrawLine(position, position + centerSensor.forward, Color.blue);
        }

        // If the object is hit with the right ray, create a position to go to the left
        ray = new Ray(position, rightSensor.forward);
        if (Physics.Raycast(ray, out hit, sideReach * rayDistance, obstacleLayer))
        {
            objectHit = true;
            Debug.DrawLine(position, hit.point, Color.red);
            Vector3 newPoint = hit.point + -transform.right;
            pathFollower.SetNewPathByAvoidance(newPoint);
        }
        else
        {
            Debug.DrawLine(position, position + rightSensor.forward, Color.blue);
        }

    }

   
    Vector3 GetBestCandidate(Vector3 posA, Vector3 posB)
    {
        Vector3 bestCandidate = Vector3.zero;
        Node nodeA = grid.NodeFromWorldPoint(posA);
        Node nodeB = grid.NodeFromWorldPoint(posB);

        // Check walkable

        // If node A is not walkable
        if (!nodeA.walkable)
        {
            if (nodeB.walkable)
            {
                bestCandidate = posB;
            }
        }
        else // If node A is walkable
        {
            if (!nodeB.walkable)
            {
                bestCandidate = posA;
            }
        }
        // If best candidate is not set then both are walkable or none of them is, the second case is highly unlikely so we'll ignore it
        if(bestCandidate != Vector3.zero)
            return bestCandidate;

        // Both are walkable so we should look at the movement penalty
        if (nodeA.movementPenalty < nodeB.movementPenalty)
            return posA;
        else
            return posB;
    }

    
}
