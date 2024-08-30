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
    
    [HideInInspector] public List<Road> connections = new List<Road>();
    [HideInInspector] public List<Lane> lanes;
    [HideInInspector] public List<Vector3> laneReferencePoints = new List<Vector3>();
    [HideInInspector] public Transform leftBottom;
    [SerializeField] public int numberOfLanes = 1;
    public PathCreator pathCreator;

    List<Vector3> rayPositions = new List<Vector3>();
    [HideInInspector] public BoxCollider boxCollider;

    [HideInInspector] public List<Line> curveRoadLines = new List<Line>();

    [HideInInspector] public List<Node> entryNodes = new List<Node>();
    [HideInInspector] public List<Node> exitNodes = new List<Node>();
    [SerializeField] public bool invertPath;

    [HideInInspector] public float boxColliderSize;
    [HideInInspector] public Bounds bounds;
    public bool procedural = false;
    public List<CarTrafficLight> trafficLights = new List<CarTrafficLight>();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        pathCreator = GetComponent<PathCreator>();
        bounds = GetComponent<MeshFilter>().mesh.bounds;
        leftBottom = gameObject.transform.Find("LeftBottom");
        SetConnections();
        SetLanes();
        SortReferencePoints();
        boxColliderSize = boxCollider.bounds.size.x;
    }
    public IEnumerator Restart()
    {
        yield return new WaitForSeconds(.5f);
        connections.Clear();
        rayPositions.Clear();
        boxCollider = GetComponent<BoxCollider>();
        pathCreator = GetComponent<PathCreator>();
        bounds = GetComponent<MeshFilter>().mesh.bounds;
        leftBottom = gameObject.transform.Find("LeftBottom");
        SetConnections();
        SetLanes();
        boxColliderSize = boxCollider.bounds.size.x;

        if (typeOfRoad != TypeOfRoad.Roundabout)
        {
            Destroy(boxCollider);
        }
        if (typeOfRoad == TypeOfRoad.Intersection)
        {
            CreateIntersectionPriorityTriggers();
        }
    }

    void Start()
    {
        if (procedural)
            return;

        if (typeOfRoad != TypeOfRoad.Roundabout)
        {
            Destroy(boxCollider);
        }

        if (typeOfRoad == TypeOfRoad.Intersection)
        {
            CreateIntersectionPriorityTriggers();
        }
    }

    public void CreateTrafficLightTriggers(CarTrafficLight trafficLight)
    {
        trafficLights.Add(trafficLight);
        Vector3 entryPos;
        Vector3 exitPos;

        if(laneReferencePoints.Count - 1 < 0)
        {
            Debug.LogError("NO TIENE LANEREFERENCEPOINTS WTF");
        }
        if(Vector3.Distance(trafficLight.transform.position, laneReferencePoints[0]) < Vector3.Distance(trafficLight.transform.position, laneReferencePoints[laneReferencePoints.Count - 1]))
        {
            entryPos = laneReferencePoints[laneReferencePoints.Count - 1];
            exitPos = laneReferencePoints[0];
        }
        else
        {
            entryPos = laneReferencePoints[0];
            exitPos = laneReferencePoints[laneReferencePoints.Count - 1];
        }

        entryPos = (entryPos + transform.position) * 0.5f;
        // Create the object
        GameObject entryTrigger = new GameObject("TrafficLight Entry Trigger");
        entryTrigger.transform.position = entryPos;
        entryTrigger.transform.parent = trafficLight.transform;
        EnterTriggerArea entryTriggerArea = entryTrigger.AddComponent<EnterTriggerArea>();
        if (entryTriggerArea != null)
        {
            entryTriggerArea.SetTrafficLight(trafficLight);
        }
        BoxCollider box = entryTrigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = (Mathf.Abs(transform.eulerAngles.y) == 90f || Mathf.Abs(transform.eulerAngles.y) == 270f) ? 
            new Vector3(transform.localScale.z * .9f, 2f, transform.localScale.x * .4f) :
            new Vector3(transform.localScale.x * .4f, 2f, transform.localScale.z * .9f);

        GameObject exitTrigger = new GameObject("TrafficLight Exit Trigger");
        exitTrigger.transform.position = exitPos;
        exitTrigger.transform.parent = trafficLight.transform;
        ExitTriggerArea exitTriggerArea = exitTrigger.AddComponent<ExitTriggerArea>();
        if (exitTriggerArea != null)
        {
            exitTriggerArea.SetTrafficLight(trafficLight);
        }
        box = exitTrigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = Vector3.one * 2.5f;
    }
    private void CreateIntersectionPriorityTriggers()
    {
        // Create boxCollider trigger to exit intersection and disable intersection whiskers
        BoxCollider exitTrigger = gameObject.AddComponent<BoxCollider>();
        exitTrigger.isTrigger = true;

        if (connections.Count > 2)
        {
            exitTrigger.size = exitTrigger.size * 0.8f;
        }
        else
        {
            exitTrigger.size = Vector3.Scale(exitTrigger.size, new Vector3(0.3f, 1f, 0.6f));
        }
        if (transform.gameObject.CompareTag("PedestrianCrossing") || transform.Cast<Transform>().Any(child => child.gameObject.CompareTag("PedestrianSignal")))
            return;

        // If not pedestrian crossing, create IntersectionTriggers
        int num = 0;
        foreach (Road neighbour in connections)
        {
            Vector3 boxPos = neighbour.transform.position;
            // Create the object
            GameObject newGameObject = new GameObject("Intersection Trigger " + num);
            newGameObject.transform.position = boxPos;
            newGameObject.transform.parent = gameObject.transform;
            // Create the boxCollider
            BoxCollider box = newGameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 3;
            IntersectionTriggers trigger = newGameObject.AddComponent<IntersectionTriggers>();
            trigger.parentRoad = this;
            trigger.belongingRoad = neighbour;
            num++;
        }

       

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

        int i = 0;
        foreach (Vector3 rayPos in rayPositions)
        {
            Vector3 rayOrigin = rayPos + Vector3.up * 25f;
            Ray ray = new Ray(rayOrigin, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 55f, roadMask))
            {
                Road road = hit.collider.gameObject.GetComponent<Road>();
                if (road != null && IsCompatible(road) && road != this)
                {          
                    connections.Add(road);
                }
            }
            i++;
        }
    }

    // Aqui solo se entra si eres una interseccion
    private void OnTriggerEnter(Collider other)
    {
        WhiskersManager carManager = other.GetComponent<WhiskersManager>();
        if (carManager != null)
        {
            carManager.intersectionInSight = false;

            if (carManager.pedestrianCrossingInSight)
            {
                carManager.ExitPedestriansPriority();
            }
        }
    }

    private void SetLanes()
    {
        lanes = new List<Lane>();
        if (typeOfRoad == TypeOfRoad.Deviation || typeOfRoad == TypeOfRoad.Split)
        {
            if (numberOfLanes < 2)
                numberOfLanes = 2;
        }
        for (int i = 0; i < numberOfLanes; i++)
        {
            lanes.Add(new Lane());
        }
    }
    private bool IsCompatible(Road road)
    {
        if (connections.Contains(road))
            return false;

        if (typeOfRoad == TypeOfRoad.Straight && road.typeOfRoad == TypeOfRoad.Straight)
        {
            // SI forward es igual, el angulo entre la direccion entre uno y otro y el forward debe ser de 90
            Vector3 dir = (road.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(dir, transform.forward);
            if (transform.forward == road.transform.forward && angle < 85f && angle > 95f)
                return false;
        }

        return true;
    }

    public bool IsExternalRoundaboutLane(Node node)
    {
        if (lanes.Count < 2 || typeOfRoad != TypeOfRoad.Roundabout)
            return false;

        if (lanes[1].nodes.Contains(node))
            return true;

        return false;
    }
}

public enum TypeOfRoad
{
    Straight,
    Intersection,
    Curve,
    Roundabout,
    Split,
    Deviation,
    Slant,
    Bridge,
    StraightSlant
}

public enum NumDirection
{
    OneDirectional,
    TwoDirectional,
    ZERO
}
public class Lane
{
    public List<Node> nodes = new List<Node>();
}

