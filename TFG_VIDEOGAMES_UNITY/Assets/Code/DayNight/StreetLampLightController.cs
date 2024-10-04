using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLampLightController : MonoBehaviour
{
    [SerializeField] bool isCar = false;
    private Light streetLight;
    void Start()
    {
        streetLight = GetComponent<Light>();
        // Subscribe to the hour change events
        if (LightingManager.Instance != null)
        {
            LightingManager.Instance.SubscribeToHourChangeEvent(OnHourChange);
            if (IsDayTime())
                SwitchLightsOff();
        }

    }
    private void OnHourChange(float hour)
    {
        if (IsDayTime())
        {
            SwitchLightsOff();
        }
        else
        {
            SwitchLightsOn();
        }
    }
    private bool IsDayTime()
    {
        float time = LightingManager.Instance.TimeOfDay;
        return (time > 6f && time < 21f);
    }
    private void SwitchLightsOff()
    {
        streetLight.enabled = false;
    }
    private void SwitchLightsOn()
    {
        streetLight.enabled = true;
    }
}
