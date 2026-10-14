using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SettingsPanelManager : MonoBehaviour
{
    public static SettingsPanelManager Instance { get; private set; }

    [Header("Referencias")]
    [Tooltip("Arrastra aquí el ConfigObj que ya está en la escena")]
    public GameObject settingsPanel;

    [Header("Configuración")]
    [Tooltip("Si está marcado, el panel empieza oculto")]
    public bool startHidden = true;

    private bool isPanelOpen = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("ConfigObj");
        }

        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanelManager: No se encontró el ConfigObj en la escena.");
            return;
        }

        if (startHidden)
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }
        else
        {
            settingsPanel.SetActive(true);
            isPanelOpen = true;
        }
    }

    public void TogglePanel()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("ConfigObj");
            if (settingsPanel == null) return;
        }

        isPanelOpen = !isPanelOpen;
        settingsPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            RefreshSliders();
        }
    }

    public void OpenPanel()
    {
        if (settingsPanel == null) return;
        isPanelOpen = true;
        settingsPanel.SetActive(true);
        RefreshSliders();
    }

    public void RefreshSliders()
    {
        if (SettingsManager.Instance == null) return;

        GameObject brilloSlider = GameObject.Find("BrilloSlider");
        if (brilloSlider != null)
        {
            var slider = brilloSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = SettingsManager.Instance.CurrentBrightness;
        }

        GameObject fovSlider = GameObject.Find("CampoSlider");
        if (fovSlider != null)
        {
            var slider = fovSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = SettingsManager.Instance.CurrentFOV;
        }

        GameObject vocesSlider = GameObject.Find("VocesSlider");
        if (vocesSlider != null)
        {
            var slider = vocesSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = SettingsManager.Instance.CurrentVoiceVolume;
        }

        GameObject efectosSlider = GameObject.Find("EfectosSlider");
        if (efectosSlider != null)
        {
            var slider = efectosSlider.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null) slider.value = SettingsManager.Instance.CurrentEffectsVolume;
        }

        GameObject toggleMusic = GameObject.Find("ToggleMusic");
        if (toggleMusic != null)
        {
            var toggle = toggleMusic.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null) toggle.isOn = SettingsManager.Instance.CurrentSpatialMusic;
        }
    }

    public void ClosePanel()
    {
        if (settingsPanel == null) return;
        isPanelOpen = false;
        settingsPanel.SetActive(false);
    }
}