using PG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class BridgePlacer : MonoBehaviour
    {
        private GameObject slantCurve, slantCurve2, slantFlat, slantFlatHigh, slantFlatHigh2, straight;
        private Dictionary<Vector2Int, GameObject> roadDictionary;

        public void SetBridgePrefabs(
            GameObject _slantCurve,
            GameObject _slantCurve2,
            GameObject _slantFlat,
            GameObject _slantFlatHigh,
            GameObject _slantFlatHigh2,
            GameObject _straight,
            Dictionary<Vector2Int, GameObject> _roadDictionary)
        {
            slantCurve = _slantCurve;
            slantCurve2 = _slantCurve2;
            slantFlat = _slantFlat;
            slantFlatHigh = _slantFlatHigh;
            slantFlatHigh2 = _slantFlatHigh2;
            straight = _straight;
            roadDictionary = _roadDictionary;
        }
    }
}
