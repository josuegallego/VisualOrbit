using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el BrilloSlider")]
    public Slider brightnessSlider;

    [Tooltip("Arrastra aquí el CampoSlider (FOV)")]
    public Slider fovSlider;

    [Tooltip("Arrastra aquí el VocesSlider")]
    public Slider voiceSlider;

    [Tooltip("Arrastra aquí el EfectosSlider")]
    public Slider effectsSlider;

    [Tooltip("Arrastra aquí el ToggleMusic")]
    public Toggle spatialMusicToggle;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnSaveClicked);
        }
    }

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
    }
}