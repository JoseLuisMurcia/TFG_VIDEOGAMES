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
        Vector3 boxPos = road.transform.position;        
        // Create the object
        GameObject newGameObject = new GameObject("Signal Priority Trigger");
        newGameObject.transform.position = boxPos;
        //newGameObject.transform.parent = gameObject.transform;
        // Create the boxCollider
        BoxCollider box = newGameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(5f, 2f, 2f);

        SignalTrigger trigger = newGameObject.AddComponent<SignalTrigger>();
        trigger.signal = this;
    }
}
