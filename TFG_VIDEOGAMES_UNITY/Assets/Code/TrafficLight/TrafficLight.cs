using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField] LayerMask roadMask;
    [HideInInspector] public TrafficLightColor currentColor = TrafficLightColor.Red;
    private Vector3 rayPos;
    [HideInInspector] public Road road;

    [HideInInspector] public ColorChanger colorChanger;

    private void Awake()
    {
        colorChanger = GetComponentInChildren<ColorChanger>();
        FindRoad();
    }

    void Start()
    {
        colorChanger.SetColor(currentColor);
    }


    public void FindRoad()
    {
        float forwardDistance = 1f;
        float rightDistance = 1f;
        rayPos = transform.forward* forwardDistance + transform.right*rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            Debug.DrawLine(ray.origin, hit.point);
            GameObject roadGameObject = hit.collider.gameObject;
            road = roadGameObject.GetComponent<Road>();
            if(road != null)
            {
                road.trafficLight = this;
                road.CreateTrafficLightTriggers();
            }
        }
    }

}

public enum TrafficLightColor
{
    Green,
    Amber,
    Red
}
