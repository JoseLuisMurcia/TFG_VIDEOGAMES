using System;
using UnityEngine;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    //Scene References
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightingPreset Preset;
    //Variables
    private int previousHour;
    [SerializeField, Range(0, 24)] public float TimeOfDay;
    public static LightingManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    public event Action<float> onHourChange;

    public void ThrowHourChangeEvent(float hour)
    {
        if (onHourChange != null)
        {
            onHourChange(hour);
        }
    }
    public void SubscribeToHourChangeEvent(Action<float> func)
    {
        onHourChange += func;
    }
    private void Update()
    {
        if (Preset == null)
            return;

        if (Application.isPlaying)
        {
            //(Replace with a reference to the game time)
            TimeOfDay += Time.deltaTime;
            TimeOfDay %= 24;           
            UpdateLighting(TimeOfDay / 24f);
        }
        else
        {
            UpdateLighting(TimeOfDay / 24f);
        }
        CheckAndEmitHourChange();
    }
    private void CheckAndEmitHourChange()
    {
        // Calculate the current hour by flooring the TimeOfDay value
        int currentHour = Mathf.FloorToInt(TimeOfDay);

        // If the hour has changed (or wraps around from 23 to 0), emit the event
        if (currentHour != previousHour)
        {
            // Emit the hour change event
            ThrowHourChangeEvent(currentHour);

            // Update previous hour to the current hour
            previousHour = currentHour;
        }
    }
    private void UpdateLighting(float timePercent)
    {
        //Set ambient and fog
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

        //If the directional light is set then rotate and set it's color, I actually rarely use the rotation because it casts tall shadows unless you clamp the value
        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);

            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
        }

    }

    //Try to find a directional light to use if we haven't set one
    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;

        //Search for lighting tab sun
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        //Search scene for light that fits criteria (directional)
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }
}