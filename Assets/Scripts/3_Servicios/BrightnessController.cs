using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class BrightnessController : MonoBehaviour
{
    [Header("Referencias")]
    public Volume globalVolume;

    [Header("Configuración")]
    public float minExposure = -2f;
    public float maxExposure = 2f;
    public float defaultExposure = 0f;

    private ColorAdjustments colorAdjustments;

    void Start()
    {
        StartCoroutine(InitializeNextFrame());
    }

    private IEnumerator InitializeNextFrame()
    {
        yield return null;

        // Si el Volume no es válido, lo buscamos en la escena
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
            Debug.Log($"[BrightnessController] Volume encontrado: {(globalVolume != null ? globalVolume.name : "NINGUNO")}");
        }

        if (globalVolume == null)
        {
            Debug.LogError("BrightnessController: No se encontró ningún Volume en la escena.");
            yield break;
        }

        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            float initialValue = SettingsManager.Instance != null
                ? SettingsManager.Instance.CurrentBrightness
                : defaultExposure;
            SetBrightness(initialValue);
            Debug.Log($"[BrightnessController] Valor inicial aplicado: {initialValue}");
        }
        else
        {
            Debug.LogError("BrightnessController: El Volume Profile no contiene Color Adjustments.");
        }
    }

    public void SetBrightness(float value)
    {
        // Verificamos que el Volume siga siendo válido
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
            if (globalVolume == null || !globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                return;
            }
        }

        if (colorAdjustments == null) return;

        float exposure = Mathf.Lerp(minExposure, maxExposure, value);
        colorAdjustments.postExposure.value = exposure;
    }
}