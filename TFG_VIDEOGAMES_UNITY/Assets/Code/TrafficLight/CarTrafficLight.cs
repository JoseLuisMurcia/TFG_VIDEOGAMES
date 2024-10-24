using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarTrafficLight : TrafficLight
{
    [SerializeField] LayerMask roadMask;
    private Vector3 rayPos;
    [HideInInspector] public TrafficLightEvents trafficLightEvents;
    [HideInInspector] public ColorChanger colorChanger;
    [SerializeField] bool tryDebug = false;

    private void Awake()
    {
        if (WorldGrid.Instance != null)
        {
            trafficLightEvents = GetComponent<TrafficLightEvents>();
            colorChanger = GetComponentInChildren<ColorChanger>();
            FindRoad();
        }         
    }

    void Start()
    {
        if (WorldGrid.Instance != null)
        {
            if(colorChanger == null) colorChanger = GetComponentInChildren<ColorChanger>();
            if(trafficLightEvents == null) trafficLightEvents = GetComponent<TrafficLightEvents>();
            currentColor = TrafficLightState.Red;
            colorChanger.SetColor(currentColor);    
        }        
    }


    public bool FindRoad()
    {
        // If Procedural
        if (RoadConnecter.Instance != null)
        {
            colorChanger = GetComponentInChildren<ColorChanger>();
            trafficLightEvents = GetComponent<TrafficLightEvents>();
            currentColor = TrafficLightState.Red;
            colorChanger.SetColor(currentColor);
        }
        float forwardDistance = 1f;
        float rightDistance = 1f;
        rayPos = transform.forward * forwardDistance + transform.right * rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.blue, 50f);
            GameObject roadGameObject = hit.collider.gameObject;
            Road road = roadGameObject.GetComponent<Road>();
            if (road != null)
            {
                //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.blue, 50f);
                road.CreateTrafficLightTriggers(this);
                return true;
            }
            else
            {
                //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.black, 50f);
                //Debug.LogWarning("Road found null by Traffic Light: " + gameObject.name);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
        else
        {
            //Debug.DrawRay(ray.origin, ray.direction * 50f, Color.magenta, 50f);
            //Debug.LogWarning("Road not found by Traffic Light: " + gameObject.name);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        return false;
    }
    public void DebugShit()
    {
        float forwardDistance = 1f;
        float rightDistance = 1f;
        rayPos = transform.forward * forwardDistance + transform.right * rightDistance + transform.position;
        Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100, roadMask))
        {
            GameObject roadGameObject = hit.collider.gameObject;
            Road road = roadGameObject.GetComponent<Road>();
            if (road != null)
            {
                Debug.DrawRay(ray.origin, ray.direction * 50f, Color.blue, 50f);
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * 50f, Color.black, 50f);
                Debug.DrawRay(transform.position + Vector3.up * 50f, ray.direction * 50f, Color.black, 50f);
                Debug.LogWarning("Road found null by Traffic Light: " + gameObject.name);
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 50f, Color.magenta, 50f);
            Debug.DrawRay(transform.position + Vector3.up * 50f, ray.direction * 50f, Color.magenta, 50f);
            Debug.LogWarning("Road not found by Traffic Light: " + gameObject.name);
        }
    }

    public void Update()
    {
        if (tryDebug)
        {
            tryDebug = false;
            DebugShit();
        }
    }

    public void DestroyTrafficLight()
    {
        Destroy(gameObject);
    }
}