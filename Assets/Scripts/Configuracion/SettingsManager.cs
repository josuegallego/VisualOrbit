using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_BRIGHTNESS = "Settings_Brightness";
    private const string KEY_FOV = "Settings_FOV";
    private const string KEY_VOICE_VOLUME = "Settings_VoiceVolume";
    private const string KEY_EFFECTS_VOLUME = "Settings_EffectsVolume";
    private const string KEY_SPATIAL_MUSIC = "Settings_SpatialMusic";

    public float DefaultBrightness = 0.5f;
    public float DefaultFOV = 0.4f;
    public float DefaultVoiceVolume = 1f;
    public float DefaultEffectsVolume = 1f;
    public bool DefaultSpatialMusic = true;

    public float CurrentBrightness { get; private set; }
    public float CurrentFOV { get; private set; }
    public float CurrentVoiceVolume { get; private set; }
    public float CurrentEffectsVolume { get; private set; }
    public bool CurrentSpatialMusic { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySettings();
    }

    public void LoadSettings()
    {
        CurrentBrightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, DefaultBrightness);
        CurrentFOV = PlayerPrefs.GetFloat(KEY_FOV, DefaultFOV);
        CurrentVoiceVolume = PlayerPrefs.GetFloat(KEY_VOICE_VOLUME, DefaultVoiceVolume);
        CurrentEffectsVolume = PlayerPrefs.GetFloat(KEY_EFFECTS_VOLUME, DefaultEffectsVolume);
        CurrentSpatialMusic = PlayerPrefs.GetInt(KEY_SPATIAL_MUSIC, DefaultSpatialMusic ? 1 : 0) == 1;
    }

    public void SaveSettings(float brightness, float fov, float voiceVolume, float effectsVolume, bool spatialMusic)
    {
        CurrentBrightness = brightness;
        CurrentFOV = fov;
        CurrentVoiceVolume = voiceVolume;
        CurrentEffectsVolume = effectsVolume;
        CurrentSpatialMusic = spatialMusic;

        PlayerPrefs.SetFloat(KEY_BRIGHTNESS, brightness);
        PlayerPrefs.SetFloat(KEY_FOV, fov);
        PlayerPrefs.SetFloat(KEY_VOICE_VOLUME, voiceVolume);
        PlayerPrefs.SetFloat(KEY_EFFECTS_VOLUME, effectsVolume);
        PlayerPrefs.SetInt(KEY_SPATIAL_MUSIC, spatialMusic ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Configuración guardada correctamente.");
    }

    public void ApplySettings()
    {
        BrightnessController brightness = FindFirstObjectByType<BrightnessController>();
        if (brightness != null)
        {
            brightness.SetBrightness(CurrentBrightness);
        }

        FOVController fov = FindFirstObjectByType<FOVController>();
        if (fov != null)
        {
            fov.SetFOV(CurrentFOV);
        }

        AudioManager audio = FindFirstObjectByType<AudioManager>();
        if (audio != null)
        {
            audio.SetVoiceVolume(CurrentVoiceVolume);
            audio.SetEffectsVolume(CurrentEffectsVolume);
            audio.SetSpatialMusic(CurrentSpatialMusic);
        }

        UpdateSlidersVisuals();
    }

    private void UpdateSlidersVisuals()
    {
        // Brillo
        GameObject brilloSlider = GameObject.Find("BrilloSlider");
        if (brilloSlider != null)
        {
            var slider = brilloSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = CurrentBrightness;
        }

        // FOV (en tu jerarquía se llama CampoSlider)
        GameObject fovSlider = GameObject.Find("CampoSlider");
        if (fovSlider != null)
        {
            var slider = fovSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = CurrentFOV;
        }

        // Voces
        GameObject vocesSlider = GameObject.Find("VocesSlider");
        if (vocesSlider != null)
        {
            var slider = vocesSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = CurrentVoiceVolume;
        }

        // Efectos
        GameObject efectosSlider = GameObject.Find("EfectosSlider");
        if (efectosSlider != null)
        {
            var slider = efectosSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = CurrentEffectsVolume;
        }
    }
}