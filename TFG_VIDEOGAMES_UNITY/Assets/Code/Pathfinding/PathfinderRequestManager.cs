using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PathfinderRequestManager : MonoBehaviour
{
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathfinderRequestManager instance;
    Pathfinding pathfinding;

    bool isProcessingPath;

    private void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
    }

    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Vector3 carForward, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, carForward, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    public static void RequestPath(Vector3 pathStart, Node pathEnd, Vector3 carForward, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, carForward, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if (!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            if (currentPathRequest.pathEnd != Vector3.zero)
            {
                pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd, currentPathRequest.carForward);
            }
            else
            {
                pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.nodeEnd, currentPathRequest.carForward);
            }
        }
    }

    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        currentPathRequest.callback(path, success);
        isProcessingPath = false;
        TryProcessNext();
    }

    struct PathRequest
    {
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public Node nodeEnd;
        public Vector3 carForward;
        public Action<Vector3[], bool> callback;

        public PathRequest(Vector3 _start, Vector3 _end, Vector3 _carForward, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
            carForward = _carForward;
            nodeEnd = null;
        }

        public PathRequest(Vector3 _start, Node _nodeEnd, Vector3 _carForward, Action<Vector3[], bool> _callback)
        {
            pathStart = _start;
            nodeEnd = _nodeEnd;
            callback = _callback;
            carForward = _carForward;
            pathEnd = Vector3.zero;
        }
    }
}
