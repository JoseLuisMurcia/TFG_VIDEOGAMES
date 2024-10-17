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
            public float yMinOffset; // How much it can be moved down
            public float yMaxOffset; // How much it can be moved up
            public float xMinOffset; // How much it can be moved left
            public float xMaxOffset; // How much it can be moved right
            public Rarity rarity; // How often the graffiti will be placed
        }

        public enum Rarity
        {
            Low,
            Medium,
            High
        }
    }
}
