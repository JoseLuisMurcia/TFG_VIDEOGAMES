using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PathCreation;

public class Road : MonoBehaviour
{
    [SerializeField] LayerMask roadMask;
    public TypeOfRoad typeOfRoad;
    [SerializeField] public NumDirection numDirection;
    [HideInInspector ]public TrafficLight trafficLight;
    [HideInInspector] public TrafficLightEvents trafficLightEvents;
    [HideInInspector] public List<Road> connections = new List<Road>();
    [HideInInspector] public List<Lane> lanes = new List<Lane>();
    [HideInInspector] public List<Vector3> laneReferencePoints = new List<Vector3>();
    [HideInInspector] public Transform leftBottom;
    [SerializeField] public int numberOfLanes = 1;
    public PathCreator pathCreator;

    List<Vector3> rayPositions = new List<Vector3>();
    public BoxCollider boxCollider;
    [HideInInspector]
    public List<Line> curveRoadLines = new List<Line>();

    [HideInInspector] public List<Node> entryNodes = new List<Node>();
    [HideInInspector] public List<Node> exitNodes = new List<Node>();
    [SerializeField] public bool invertPath;

    [HideInInspector] public float boxColliderSize;
    private void Awake()
    {
        trafficLightEvents = GetComponent<TrafficLightEvents>();
        boxCollider = GetComponent<BoxCollider>();
        pathCreator = GetComponent<PathCreator>();
        leftBottom = gameObject.transform.Find("LeftBottom");
        SetConnections();
        SetLanes();
        SortReferencePoints();
        boxColliderSize = boxCollider.bounds.size.x;

        Destroy(boxCollider);
    }

    private void SortReferencePoints()
    {
        if (pathCreator == null)
            return;
        List<Vector3> localPoints = pathCreator.bezierPath.GetAnchorPoints();
        foreach (Vector3 localPoint in localPoints)
            laneReferencePoints.Add(transform.TransformPoint(localPoint));


    }
    private void SetConnections()
    {
        Vector3 center = transform.position + boxCollider.center;
        Vector3 halfSize = boxCollider.bounds.size * 0.5f;
        float offset = 0.3f;
        Vector3 ray1Pos = new Vector3(center.x + halfSize.x + offset, 0, center.z + halfSize.z + offset);
        Vector3 ray2Pos = new Vector3(center.x - halfSize.x - offset, 0, center.z + halfSize.z + offset);
        Vector3 ray3Pos = new Vector3(center.x - halfSize.x - offset, 0, center.z - halfSize.z - offset);
        Vector3 ray4Pos = new Vector3(center.x + halfSize.x + offset, 0, center.z - halfSize.z - offset);

        Vector3 ray1newPos = (ray1Pos + ray2Pos) * 0.5f;
        Vector3 ray2newPos = (ray2Pos + ray3Pos) * 0.5f;
        Vector3 ray3newPos = (ray3Pos + ray4Pos) * 0.5f;
        Vector3 ray4newPos = (ray4Pos + ray1Pos) * 0.5f;

        Vector3 ray5newPos = (ray1Pos + ray1newPos) * 0.5f;
        Vector3 ray6newPos = (ray1newPos + ray2Pos) * 0.5f;
        Vector3 ray7newPos = (ray2Pos + ray2newPos) * 0.5f;
        Vector3 ray8newPos = (ray2newPos + ray3Pos) * 0.5f;
        Vector3 ray9newPos = (ray3Pos + ray3newPos) * 0.5f;
        Vector3 ray10newPos = (ray3newPos + ray4Pos) * 0.5f;
        Vector3 ray11newPos = (ray4Pos + ray4newPos) * 0.5f;
        Vector3 ray12newPos = (ray4newPos + ray1Pos) * 0.5f;

        rayPositions.Add(ray1newPos);
        rayPositions.Add(ray2newPos);
        rayPositions.Add(ray3newPos);
        rayPositions.Add(ray4newPos);

        rayPositions.Add(ray5newPos);
        rayPositions.Add(ray6newPos);
        rayPositions.Add(ray7newPos);
        rayPositions.Add(ray8newPos);

        rayPositions.Add(ray9newPos);
        rayPositions.Add(ray10newPos);
        rayPositions.Add(ray11newPos);
        rayPositions.Add(ray12newPos);

        //rayPositions.Add(ray1Pos);
        //rayPositions.Add(ray2Pos);
        //rayPositions.Add(ray3Pos);
        //rayPositions.Add(ray4Pos);

        foreach (Vector3 rayPos in rayPositions)
        {
            Ray ray = new Ray(rayPos + Vector3.up * 50, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100, roadMask))
            {
                GameObject _gameObject = hit.collider.gameObject;

                Road road = _gameObject.GetComponent<Road>();
                // We have hit a road, set its type to the node
                if (road != null && IsCompatible(road))
                {
                    connections.Add(road);
                }
            }
        }
    }

    private void SetLanes()
    {
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanes.Add(new Lane());
        }
    }
    private bool IsCompatible(Road road)
    {
        if (connections.Contains(road))
            return false;

        //if(numDirection == NumDirection.One && road.numDirection == NumDirection.One && )
        return true;
    }

    //private void OnDrawGizmos()
    //{
    //    foreach (Vector3 rayPos in rayPositions)
    //    {
    //        Gizmos.DrawRay(rayPos, new Vector3(0, 5, 0));
    //    }
    //}
}

public enum TypeOfRoad
{
    Straight,
    Intersection,
    Curve,
    Roundabout,
    Split,
    Deviation
}

public enum NumDirection
{
    OneDirectional,
    TwoDirectional
}
public class Lane
{
    public List<Node> nodes = new List<Node>();
}
