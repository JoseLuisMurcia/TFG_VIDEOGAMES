using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class BuildingGenerator : MonoBehaviour
    {
        [SerializeField] private List<GameObject> basePrefabs;
        [SerializeField] private List<GameObject> middlePrefabs;
        [SerializeField] private List<GameObject> topPrefabs;
        private int maxPrefabs = 25;
        private void Build()
        {
            float sampledValue = PerlinNoiseGenerator.Instance.PerlinSteppedPosition(transform.position);

            int targetPieces = Mathf.FloorToInt(maxPrefabs * (sampledValue));
            targetPieces += Random.Range(-5, 10);

            if (targetPieces <= 0)
            {
                return;
            }

            float heightOffset = 0;
            heightOffset += SpawnPieceLayer(basePrefabs, heightOffset);

            for (int i = 2; i < targetPieces; i++)
            {
                heightOffset += SpawnPieceLayer(middlePrefabs, heightOffset);
            }

            SpawnPieceLayer(topPrefabs, heightOffset);
        }

        private float SpawnPieceLayer(List<GameObject> pieceArray, float inputHeight)
        {
            Transform randomTransform = pieceArray[Random.Range(0, pieceArray.Count)].transform;
            GameObject clone = Instantiate(randomTransform.gameObject, this.transform.position + new Vector3(0, inputHeight, 0), transform.rotation) as GameObject;
            Mesh cloneMesh = clone.GetComponentInChildren<MeshFilter>().mesh;
            Bounds baseBounds = cloneMesh.bounds;
            float heightOffset = baseBounds.size.y;

            clone.transform.SetParent(this.transform);

            return heightOffset;
        }
    }
}
