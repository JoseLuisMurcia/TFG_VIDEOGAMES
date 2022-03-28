using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiskersManager : MonoBehaviour
{
    private AvoidanceBehavior avoidanceBehavior;
    private PriorityBehavior priorityBehavior;
    private PathFollower pathFollower;
    private TrafficLightCarController trafficLightCarController;
    [SerializeField] LayerMask obstacleLayer, carLayer, signalLayer;

    private List<Transform> whiskers = new List<Transform>();
    // Start is called before the first frame update
    void Start()
    {
        
        pathFollower = GetComponent<PathFollower>();
        trafficLightCarController = GetComponent<TrafficLightCarController>();

        Transform whiskersParent = transform.Find("Whiskers");
        foreach (Transform sensor in whiskersParent.transform)
        {
            whiskers.Add(sensor);
        }

        avoidanceBehavior = new AvoidanceBehavior(carLayer, obstacleLayer, whiskers, pathFollower, trafficLightCarController);
        priorityBehavior = new PriorityBehavior(carLayer, signalLayer, whiskers, pathFollower);
        pathFollower.avoidanceBehavior = avoidanceBehavior;
    }

    // Update is called once per frame
    void Update()
    {
        avoidanceBehavior.Update(transform);
        priorityBehavior.Update(transform);
    }
}
