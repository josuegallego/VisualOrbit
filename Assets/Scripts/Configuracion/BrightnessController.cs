using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessController : MonoBehaviour
{
    public Volume brightnessVolume;

    private ColorAdjustments colorAdjustments;

    void Start()
    {
        brightnessVolume.profile.TryGet(out colorAdjustments);
    }

    public void SetBrightness(float value)
    {
        colorAdjustments.postExposure.value = value;
    }
}