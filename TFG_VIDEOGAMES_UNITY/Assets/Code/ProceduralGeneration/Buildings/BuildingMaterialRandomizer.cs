using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PG
{
    public class BuildingMaterialRandomizer : MonoBehaviour
    {
        [System.Serializable]
        public class MaterialSet
        {
            public Material[] materials; // Array of materials for this specific set
        }

        [SerializeField] private List<MaterialSet> materialSets; // List of possible material sets
        private MeshRenderer meshRenderer;

        void Start()
        {
            // Get all the mesh renderers in the building
            meshRenderer = GetComponentInParent<MeshRenderer>();

            // Randomly select a material set
            MaterialSet selectedMaterialSet = materialSets[Random.Range(0, materialSets.Count)];

            // Apply the selected material set to the building's mesh renderers
            ApplyMaterialSet(selectedMaterialSet);
        }
        private void ApplyMaterialSet(MaterialSet materialSet)
        {
            // Make sure that the selected material set matches the number of materials on the MeshRenderer
            if (materialSet.materials.Length == meshRenderer.materials.Length)
            {
                Material[] newMaterials = SortMaterials(materialSet.materials, meshRenderer.materials);
                meshRenderer.materials = newMaterials;
            }
            else
            {
                Debug.LogWarning("Material set does not match the number of materials in the MeshRenderer.");
            }

        }
        private Material[] SortMaterials(Material[] newMaterials, Material[] meshMaterials)
        {
            // Create an array to hold the sorted materials
            Material[] sortedMaterials = new Material[meshMaterials.Length];

            // Iterate through the meshMaterials array
            for (int i = 0; i < meshMaterials.Length; i++)
            {
                // Try to find the matching material from newMaterials
                Material matchingMaterial = newMaterials.FirstOrDefault(m => m.name + " (Instance)" == meshMaterials[i].name);

                // If a matching material is found, add it to the sortedMaterials array
                if (matchingMaterial != null)
                {
                    sortedMaterials[i] = matchingMaterial;
                }
                else
                {
                    // If no matching material is found, log a warning and keep the original one
                    Debug.LogWarning($"No matching material found for {meshMaterials[i].name}. Using original.");
                    sortedMaterials[i] = meshMaterials[i];
                }
            }
            return sortedMaterials;
        }
    }
}
