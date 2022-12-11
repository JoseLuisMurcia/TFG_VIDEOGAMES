using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationManager : MonoBehaviour
{
    
    public class SlotAssignment
    {
        public Pedestrian pedestrian;
        public int slotNumber;
    }

    List<SlotAssignment> slotAssignments = new List<SlotAssignment>();
    // Rotation and pos: driftOffset; // Drift offset for the currently filled slots
    FormationPattern formationPattern;


    // Update the assignment of characters to slots
    void UpdateSlotAssignments()
    {
        // Assign slot numbers to each pedestrian
        for(int i=0; i<slotAssignments.Count; i++)   
            slotAssignments[i].slotNumber = i;

        // Update the drift offset
        // driftOffset = pattern.GetDriftOffset(slotAssignments);
    }

    bool AddPedestrian(Pedestrian _pedestrian)
    {
        int occupiedSlots = slotAssignments.Count;
        if (formationPattern.SupportsSlots(occupiedSlots + 1))
        {
            SlotAssignment _slotAssignment = new SlotAssignment();
            _slotAssignment.pedestrian = _pedestrian;
            slotAssignments.Add(_slotAssignment);
            UpdateSlotAssignments();
            return true;
        }
        return false;
    }

    //bool RemovePedestrian(Pedestrian _pedestrian)
    //{
    //    slot = charactersInSlots.findIndexOfCharacter(character)
    //    slotAssignments.removeAt(slot)
    //    UpdateSlotAssignments();
    //}

    // Send new target locations to each character
    void UpdateSlots()
    {
        //// Find the anchor point.
        //anchor Static = GetAnchorPoint();
        //orientationMatrix: Matrix = anchor.orientation.asMatrix()
        
        //// Go through each character in turn.
        //for i in 0..slotAssignments.length():
        //slotNumber: int = slotAssignments[i].slotNumber
        //slot: Static = pattern.getSlotLocation(slotNumber)

        //// Transform by the anchor point position and orientation.
        //location = new Static()
        //location.position = anchor.position +
        //orientationMatrix * slot.position
        //location.orientation = anchor.orientation +
        //slot.orientation

        //// And add the drift component.
        //location.position -= driftOffset.position
        //location.orientation -= driftOffset.orientation

        //// Send the static to the character.
        //slotAssignments[i].character.setTarget(location)
    }

    void GetAnchorPoint()
    {

    }
}
