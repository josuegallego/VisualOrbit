using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el BrilloSlider")]
    public Slider brightnessSlider;

    [Tooltip("Arrastra aquí el FOVSlider")]
    public Slider fovSlider;

    [Tooltip("Arrastra aquí el VocesSlider")]
    public Slider voiceSlider;

    [Tooltip("Arrastra aquí el EfectosSlider")]
    public Slider effectsSlider;

    [Tooltip("Arrastra aquí el ToggleMusic")]
    public Toggle spatialMusicToggle;

    void Start()
    {
        // Nos aseguramos de que el botón tenga el listener
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnSaveClicked);
        }
    }

    private void OnSaveClicked()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SaveButton: No hay SettingsManager en la escena.");
            return;
        }

        // Recogemos los valores actuales de los sliders
        float brightness = brightnessSlider != null ? brightnessSlider.value : 0.5f;
        float fov = fovSlider != null ? fovSlider.value : 0.4f;
        float voice = voiceSlider != null ? voiceSlider.value : 1f;
        float effects = effectsSlider != null ? effectsSlider.value : 1f;
        bool spatialMusic = spatialMusicToggle != null ? spatialMusicToggle.isOn : true;

        // Guardamos los valores
        SettingsManager.Instance.SaveSettings(brightness, fov, voice, effects, spatialMusic);

        Debug.Log("Configuración guardada y sincronizada.");
    }
}