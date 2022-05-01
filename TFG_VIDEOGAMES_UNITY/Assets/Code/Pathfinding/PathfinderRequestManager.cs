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

    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Vector3 carForward, Action<PathfindingResult, bool, Node, Node> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, carForward, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    public static void RequestPath(Node pathStart, Node pathEnd, Vector3 carForward, Action<PathfindingResult, bool, Node, Node> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, carForward, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    public static void RequestPath(Vector3 pathStart, Node pathEnd, Vector3 carForward, Action<PathfindingResult, bool, Node, Node> callback)
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


            if (currentPathRequest.startNode != null)
            {
                pathfinding.StartFindPath(currentPathRequest.startNode, currentPathRequest.endNode);
            }
            else
            {
                if (currentPathRequest.endNode == null)
                {
                    pathfinding.StartFindPath(currentPathRequest.startPos, currentPathRequest.endPos, currentPathRequest.carForward);
                }
                else
                {
                    pathfinding.StartFindPath(currentPathRequest.startPos, currentPathRequest.endNode, currentPathRequest.carForward);
                }
            }
        }
    }

    public void FinishedProcessingPath(PathfindingResult pathResult, bool success, Node startNode, Node endNode)
    {
        currentPathRequest.callback(pathResult, success, startNode, endNode);
        isProcessingPath = false;
        TryProcessNext();
    }

    struct PathRequest
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public Node endNode;
        public Node startNode;
        public Vector3 carForward;
        public Action<PathfindingResult, bool, Node, Node> callback;

        public PathRequest(Vector3 _startPos, Vector3 _endPos, Vector3 _carForward, Action<PathfindingResult, bool, Node, Node> _callback)
        {
            startPos = _startPos;
            endPos = _endPos;
            callback = _callback;
            carForward = _carForward;

            startNode = null;
            endNode = null;
        }

        public PathRequest(Node _nodeStart, Node _nodeEnd, Vector3 _carForward, Action<PathfindingResult, bool, Node, Node> _callback)
        {
            startNode = _nodeStart;
            endNode = _nodeEnd;
            callback = _callback;
            carForward = _carForward;

            startPos = Vector3.zero;
            endPos = Vector3.zero;
        }

        public PathRequest(Vector3 _startPos, Node _nodeEnd, Vector3 _carForward, Action<PathfindingResult, bool, Node, Node> _callback)
        {
            startPos = _startPos;
            endNode = _nodeEnd;
            callback = _callback;
            carForward = _carForward;

            startNode = null;
            endPos = Vector3.zero;
        }
    }
}
