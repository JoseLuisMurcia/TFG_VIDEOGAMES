using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal : MonoBehaviour
{
    [SerializeField] public PriorityLevel priorityLevel;
    [SerializeField] LayerMask roadMask;
    [HideInInspector] public Road road;


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
        GameObject newGameObject = new GameObject("Middle Signal Priority Trigger");
        newGameObject.transform.position = boxPos;
        //newGameObject.transform.parent = gameObject.transform;
        // Create the boxCollider
        BoxCollider box = newGameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 2f, 4f);

        SignalTrigger trigger = newGameObject.AddComponent<SignalTrigger>();
        trigger.signal = this;

        // Create anothe box at the beggining of the road
        Vector3 startBoxPos = FindStartEntryNode() - transform.forward * 2f;
        // Create the object
        GameObject startTrigger = new GameObject("Start Signal Priority Trigger");
        startTrigger.transform.position = startBoxPos;
        //newGameObject.transform.parent = gameObject.transform;
        // Create the boxCollider
        BoxCollider startBox = startTrigger.AddComponent<BoxCollider>();
        startBox.isTrigger = true;
        startBox.size = new Vector3(3.5f, 2f, 3.5f);

        SignalTrigger roadStartTrigger = startTrigger.AddComponent<SignalTrigger>();
        roadStartTrigger.signal = this;
    }

    private Vector3 FindStartEntryNode()
    {
        // We want the furthest node from the signal
        Vector3 pos1 = road.laneReferencePoints[0];
        Vector3 pos2 = road.laneReferencePoints[road.laneReferencePoints.Count-1];

        float distance1 = Vector3.Distance(pos1, transform.position);
        float distance2 = Vector3.Distance(pos2, transform.position);

        if (distance1 > distance2)
            return pos1;

        return pos2;
    }
}
