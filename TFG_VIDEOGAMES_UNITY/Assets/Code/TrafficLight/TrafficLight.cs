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
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (currentColor)
        {
            case TrafficLightColor.Green:
                Gizmos.color = Color.green;
                break;
            case TrafficLightColor.Amber:
                Gizmos.color = Color.yellow;
                break;
            case TrafficLightColor.Red:
                Gizmos.color = Color.red;
                break;
        }
        Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.7f);

        //Gizmos.color = Color.cyan;
        //Gizmos.DrawSphere(rayPos, .5f);
    }

}

public enum TrafficLightColor
{
    Green,
    Amber,
    Red
}
