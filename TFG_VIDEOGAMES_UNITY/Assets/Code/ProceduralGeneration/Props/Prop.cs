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
            public bool isExterior; // This boolean dictates if the prop is always placed close to the road
            public bool isInterior; // This boolean dictates if the prop is always placed far from the road
            public bool looksToSidewalk; // If true, the asset will always be rotated to look towards the sidewalk (By default they all look towards the road)
            public bool looksToRoad; // If true and between building and road node, the asset will be rotated to look towards the sidewalk 
            public bool isAlley; // If true, can only be placed on an alley
            public bool requiresRoad; // If true, it can't be placed next to a bridge or roundabout (bus stops)
        }
    }
}
