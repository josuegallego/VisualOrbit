using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SaveButton : MonoBehaviour
{
    [Header("Referencias")]
    public Slider brightnessSlider;
    public Slider fovSlider;
    public Slider voiceSlider;
    public Slider effectsSlider;
    public Toggle spatialMusicToggle;

    [Header("Aviso")]
    [Tooltip("Arrastra aquí el texto AvisoGuardado")]
    public GameObject avisoGuardado;

    [Tooltip("Segundos que se muestra el aviso")]
    public float duracionAviso = 2f;

    private Coroutine avisoCoroutine;

    public void OnSaveClicked()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SaveButton: No hay SettingsManager en la escena.");
            return;
        }

        float brightness = brightnessSlider != null ? brightnessSlider.value : 0.5f;
        float fov = fovSlider != null ? fovSlider.value : 0.4f;
        float voice = voiceSlider != null ? voiceSlider.value : 1f;
        float effects = effectsSlider != null ? effectsSlider.value : 1f;
        bool spatialMusic = spatialMusicToggle != null ? spatialMusicToggle.isOn : true;

        SettingsManager.Instance.SaveSettings(brightness, fov, voice, effects, spatialMusic);

        Debug.Log("Configuración guardada y sincronizada.");

        if (avisoGuardado != null)
        {
            if (avisoCoroutine != null)
            {
                StopCoroutine(avisoCoroutine);
            }
            avisoCoroutine = StartCoroutine(MostrarAviso());
        }
    }

    private IEnumerator MostrarAviso()
    {
        avisoGuardado.SetActive(true);
        yield return new WaitForSeconds(duracionAviso);
        avisoGuardado.SetActive(false);
    }
}