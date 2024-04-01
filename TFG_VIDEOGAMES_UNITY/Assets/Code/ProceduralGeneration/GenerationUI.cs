using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PG
{
    public class GenerationUI : MonoBehaviour
    {
        [SerializeField] Button regenerateButton;
        [SerializeField] GameObject carSpawner, pedestrianSpawner;
        private Visualizer visualizer;
        void Start()
        {
            visualizer = GetComponent<Visualizer>();
            regenerateButton.enabled = false;
            ToggleChildren(carSpawner);
            ToggleChildren(pedestrianSpawner);
        }

        public void OnCityCreated()
        {
            regenerateButton.enabled = true;
            ToggleChildren(carSpawner);
            ToggleChildren(pedestrianSpawner);
        }

        public void Regenerate()
        {
            regenerateButton.enabled = false;
            ToggleChildren(carSpawner);
            ToggleChildren(pedestrianSpawner);
            visualizer.grid.Reset();
            visualizer.roadPlacer.Reset();
            visualizer.StartGeneration();
        }
        public void DestroyCityButton()
        {
            regenerateButton.enabled = false;
            ToggleChildren(carSpawner);
            ToggleChildren(pedestrianSpawner);
            visualizer.roadPlacer.DestroyAssets();
        }
        void ToggleChildren(GameObject _gameObject)
        {
            foreach(Transform child in _gameObject.transform)
            {
                Button button = child.gameObject.GetComponent<Button>();
                button.enabled = !button.enabled;
            }
        }
    }
}

