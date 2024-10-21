using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Prop : MonoBehaviour
    {
        public PropInfo propInfo;

        [System.Serializable]
        public class PropInfo
        {
            public int maxInstances = -1;
            public float distanceToInstance;
            public bool isCloseToRoad; // This boolean dictates if the prop is always placed next to the road
            public bool looksToRoad; // If true, the asset will be rotated to look to the road
            public bool isAlley; // If true, can only be placed on an alley
        }
    }
}
