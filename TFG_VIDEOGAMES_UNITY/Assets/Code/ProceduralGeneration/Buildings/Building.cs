using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingInfo buildingInfo;

    [System.Serializable]
    public class BuildingInfo
    {
        public GameObject prefab;
        public int width;  // How many grid nodes in the X direction
        public int height; // How many grid nodes in the Y direction
    }
}
