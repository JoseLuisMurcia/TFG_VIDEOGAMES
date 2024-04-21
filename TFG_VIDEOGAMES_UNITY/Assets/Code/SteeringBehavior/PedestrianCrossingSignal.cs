using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianCrossingSignal : MonoBehaviour
{
    [SerializeField] LayerMask roadMask;
    [HideInInspector] public Road road;
    // Start is called before the first frame update
    void Start()
    {
        FindRoad();
        CreateIntersectionPriorityTrigger();
    }

    public void FindRoad()
    {
        float forwardDistance = 2f;
        float rightDistance = 1.5f;
        Vector3 rayPos = transform.forward * forwardDistance + transform.right * rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            //Debug.DrawLine(ray.origin, hit.point, Color.green, 10f);
            GameObject roadGameObject = hit.collider.gameObject;
            road = roadGameObject.GetComponent<Road>();
        }
    }
    private void CreateIntersectionPriorityTrigger()
    {
        // Create one box in the middle of the road
        Vector3 boxPos = road.transform.position;
        // Create the object
        GameObject newGameObject = new GameObject("Pedestrian Trigger");
        newGameObject.transform.position = boxPos;
        // Create the boxCollider
        BoxCollider box = newGameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 2f, 4f);

        PedestrianSignalTrigger trigger = newGameObject.AddComponent<PedestrianSignalTrigger>();
        trigger.signal = this;
    }

}
