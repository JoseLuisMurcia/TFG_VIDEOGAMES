using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roundabout : Road
{
    [HideInInspector] public List<Transform> exits = new List<Transform>();
    [HideInInspector] public List<Transform> entries = new List<Transform>();
    [HideInInspector] public float laneWidth;

    void Start()
    {
        Transform exitParent = gameObject.transform.Find("Exits");
        Transform entryParent = gameObject.transform.Find("Entries");
        foreach (Transform child in exitParent.transform)
        {
            exits.Add(child);
        }
        foreach (Transform child in entryParent.transform)
        {
            entries.Add(child);
        }

        Transform laneWidthTransform = gameObject.transform.Find("LaneWidth");
        Vector3 pos1 = laneWidthTransform.Find("1").position;
        Vector3 pos2 = laneWidthTransform.Find("2").position;
        laneWidth = Mathf.Abs(Vector3.Distance(pos1, pos2));
        typeOfRoad = TypeOfRoad.Roundabout;
        numDirection = NumDirection.OneDirectional;


        Vector3 boxSize = boxCollider.size;
        boxCollider.size = new Vector3(boxSize.x * .8f, boxSize.y, boxSize.z * .8f);
        boxCollider.isTrigger = true;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        WhiskersManager manager = other.gameObject.GetComponent<WhiskersManager>();
        if (manager != null)
        {
            manager.RoundaboutTrigger(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WhiskersManager manager = other.gameObject.GetComponent<WhiskersManager>();
        if (manager != null)
        {
            manager.RoundaboutTrigger(false);
        }
    }
}
