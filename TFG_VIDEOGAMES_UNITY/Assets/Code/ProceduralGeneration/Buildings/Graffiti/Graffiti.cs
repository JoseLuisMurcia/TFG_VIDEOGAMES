using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class Graffiti : MonoBehaviour
    {
        public GraffitiInfo graffitiInfo;

        [System.Serializable]
        public class GraffitiInfo
        {
            // Randomize vertical and lateral offset
            public float yMinOffset;
            public float yMaxOffset;
            public float xMinOffset;
            public float xMaxOffset;

            // Randomize how much the graffitis are scaled
            public float maxSmallScale = .5f;
            public float minSmallScale = .25f;
            public float maxBigScale = .25f;
            public float minBigScale = .15f;

            // How often the graffiti will be placed
            public Rarity rarity; 
        }

        public enum Rarity
        {
            Low,
            Medium,
            High
        }
    }
}
