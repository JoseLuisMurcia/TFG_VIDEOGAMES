using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Building : MonoBehaviour
    {
        public BuildingInfo buildingInfo;

        [System.Serializable]
        public class BuildingInfo
        {
            public GameObject prefab;
            public int xValue;  // How many grid nodes in the X direction
            public int yValue; // How many grid nodes in the Y direction
        }
    }
}

