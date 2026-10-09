using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Necesario si usas URP

public class BrightnessController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el Global Volume que contiene el Color Adjustments")]
    public Volume globalVolume;

    [Header("Configuración")]
    [Tooltip("Valor mínimo de Post Exposure")]
    public float minExposure = -2f;
    [Tooltip("Valor máximo de Post Exposure")]
    public float maxExposure = 2f;
    [Tooltip("Valor inicial de Post Exposure")]
    public float defaultExposure = 0f;

    private ColorAdjustments colorAdjustments;

    void Start()
    {
        if (globalVolume == null)
        {
            Debug.LogError("BrightnessController: No se ha asignado el Global Volume.");
            return;
        }

        // Intentamos obtener el override de Color Adjustments del perfil
        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            // Aplicamos el valor inicial
            colorAdjustments.postExposure.value = defaultExposure;
        }
        else
        {
            Debug.LogError("BrightnessController: El Volume Profile no contiene Color Adjustments.");
        }
    }

    /// <summary>
    /// Método que se conecta al On Value Changed (Single) del Slider.
    /// </summary>
    /// <param name="value">Valor del slider (0 a 1 normalmente)</param>
    public void SetBrightness(float value)
    {
        if (colorAdjustments == null) return;

        // Mapeamos el valor del slider (0-1) al rango de exposición (min-max)
        float exposure = Mathf.Lerp(minExposure, maxExposure, value);
        colorAdjustments.postExposure.value = exposure;
    }
}