using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightFormation : FormationPattern
{
    List<Vector3> formation = new List<Vector3>();
    float spacing = 3f;
    [SerializeField] int numSlots = 3;
    Vector3 anchorPoint;
    void Start()
    {
        anchorPoint = transform.position;
        List<float> offsets = GetOffsets(numSlots);
        for (int i = 0; i < numSlots; i++)
        {
            formation[i] = new Vector3(offsets[i], 0f, 0f);
        }
    }
    public override Vector3 GetSlotVectorLocation(int slotIndex)
    {
        return formation[slotIndex];
    }

    public List<float> GetOffsets(int _numSlots)
    {
        switch (_numSlots)
        {
            case 2:
                return new List<float>() { -spacing * .5f, spacing * .5f };
            case 3:
                return new List<float>() { -spacing, 0, spacing };
            case 4:
                return new List<float>() { -spacing * 5f - spacing, -spacing * .5f, spacing * .5f, spacing * .5f + spacing };
            default:
                Debug.Log("Not contemplated size for slots");
                return null;
        }
    }



    // The drift offset when characters are in the given set of slots.
    public override Transform GetDriftOffset(List<FormationManager.SlotAssignment> slotAssignments) 
    {
        return null;

    }

    //Calculate and return the location of the given slot index.
    public override Transform GetSlotLocation(int slotNumber)
    {
        return null;
    }
    // True if the pattern can support the given number of slots.
    public override bool SupportsSlots(int slotCount)
    {
        return true;
    }
}
