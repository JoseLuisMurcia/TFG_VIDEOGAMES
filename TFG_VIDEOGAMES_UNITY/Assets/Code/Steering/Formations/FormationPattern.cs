using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//abstract class that lets us define multiple formations with just an array of Vector3's
public abstract class FormationPattern : MonoBehaviour
{

    // Get the position of a given slot index
    public abstract Vector3 GetSlotVectorLocation(int slotIndex);

    // The drift offset when characters are in the given set of slots.
    public abstract Transform GetDriftOffset(List<FormationManager.SlotAssignment> slotAssignments);

    //Calculate and return the location of the given slot index.
    public abstract Transform GetSlotLocation(int slotNumber);
    // True if the pattern can support the given number of slots.
    public abstract bool SupportsSlots(int slotCount);

}
