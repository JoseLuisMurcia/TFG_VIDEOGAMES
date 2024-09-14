using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class FenceRandomizer : MonoBehaviour
    {
        [SerializeField] private List<Material> materials; // List of possible material sets
        [SerializeField] private List<GameObject> fencePrefabs; // List of possible fences
        private MeshRenderer[] meshRenderers;

        private void Awake()
        {
            GameObject selectedPrefab = fencePrefabs[Random.Range(0, fencePrefabs.Count)];
            Instantiate(selectedPrefab, transform);
        }
        void Start()
        {
            // Get all the mesh renderers in the building
            meshRenderers = GetComponentsInChildren<MeshRenderer>();

            // Randomly select a material set
            Material selectedMaterial = materials[Random.Range(0, materials.Count)];

            // Apply the selected material set to the building's mesh renderers
            ApplyMaterialSet(selectedMaterial);
        }
        private void ApplyMaterialSet(Material selectedMaterial)
        {
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Material[] materials = new Material[] { selectedMaterial };
                meshRenderer.materials = materials;
            }
        }
    }
}