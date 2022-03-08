using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    [SerializeField] public List<Direction> directions = new List<Direction>();
    [SerializeField] LayerMask roadMask;
    public TypeOfRoad typeOfRoad = TypeOfRoad.None;
    [SerializeField] public NumDirection numDirection = NumDirection.None;
    public TrafficLight trafficLight;
    public TrafficLightEvents trafficLightEvents;
    [Range(1, 2)]
    public int numberOfLanes;
    [SerializeField] public List<Road> connections = new List<Road>();

    List<Vector3> rayPositions = new List<Vector3>();
    Renderer renderer;
    BoxCollider boxCollider;

    private void Awake()
    {
        trafficLightEvents = GetComponent<TrafficLightEvents>();
        renderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
        SetTypeOfRoad();
        SetConnections();

    }

    private void SetTypeOfRoad()
    {
        if (directions.Count == 1)
        {
            // Important, the order of enums must be the same in order for the cast to work properly
            typeOfRoad = (TypeOfRoad)directions[0];
        }
        else
        {
            if (directions.Contains(Direction.Up))
            {
                if (directions.Contains(Direction.Left))
                {
                    typeOfRoad = TypeOfRoad.UpToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
            }
            else if (directions.Contains(Direction.Down))
            {
                if (directions.Contains(Direction.Left))
                {
                    typeOfRoad = TypeOfRoad.DownToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.DownToRight;
                }
            }
            else if (directions.Contains(Direction.Right))
            {
                if (directions.Contains(Direction.Up))
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.DownToRight;
                }
            }
            else if (directions.Contains(Direction.Left))
            {
                if (directions.Contains(Direction.Up))
                {
                    typeOfRoad = TypeOfRoad.UpToLeft;
                }
                else
                {
                    typeOfRoad = TypeOfRoad.UpToRight;
                }
            }
        }
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
        rayPositions.Add(ray1newPos);
        rayPositions.Add(ray2newPos);
        rayPositions.Add(ray3newPos);
        rayPositions.Add(ray4newPos);
        
        foreach(Vector3 rayPos in rayPositions)
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

    private bool IsCompatible(Road road)
    {
        if (connections.Contains(road))
            return false;

        //if(numDirection == NumDirection.One && road.numDirection == NumDirection.One && )
        return true;
    }

    //private void OnDrawGizmos()
    //{
    //    foreach(Vector3 rayPos in rayPositions)
    //    {
    //        Gizmos.DrawRay(rayPos, new Vector3(0, 5, 0));
    //    }
    //}
}

public enum TypeOfRoad
{
    None = -1,
    Right,
    Up, 
    Down,
    Left,
    DownToLeft,
    DownToRight,
    UpToRight,
    UpToLeft
}

public enum Direction
{
    None = -1,
    Right,
    Up,
    Down,
    Left
}

public enum NumDirection
{
    None = -1,
    One,
    Two
}
