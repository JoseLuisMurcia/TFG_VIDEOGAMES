using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightCarController : MonoBehaviour
{
    public Path path;

    public List<Node> nodes;

    PathFollower pathFollower;
    private void Start()
    {
        pathFollower = GetComponent<PathFollower>();
    }
}
