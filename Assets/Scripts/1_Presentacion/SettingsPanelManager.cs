using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SettingsPanelManager : MonoBehaviour
{
    public static SettingsPanelManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("Si está marcado, el panel empieza oculto")]
    public bool startHidden = true;

    private GameObject settingsPanel;
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Start()
    {
        settingsPanel = FindConfigObjInScene();

        if (settingsPanel != null && startHidden)
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FindPanelNextFrame());
    }

    private IEnumerator FindPanelNextFrame()
    {
        yield return null;
        yield return null;

        settingsPanel = FindConfigObjInScene();

        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsPanelManager: No se encontró ConfigObj en la escena " + SceneManager.GetActiveScene().name);
            yield break;
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

    private GameObject FindConfigObjInScene()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "ConfigObj" && obj.scene == activeScene)
            {
                return obj;
            }
        }

        return null;
    }

    public void TogglePanel()
    {
        if (settingsPanel == null)
        {
            settingsPanel = FindConfigObjInScene();
            if (settingsPanel == null)
            {
                Debug.LogWarning("SettingsPanelManager: TogglePanel no encontró ConfigObj.");
                return;
            }
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
        if (settingsPanel == null)
        {
            settingsPanel = FindConfigObjInScene();
            if (settingsPanel == null) return;
        }
        isPanelOpen = true;
        settingsPanel.SetActive(true);
        RefreshSliders();
    }

    public void ClosePanel()
    {
        if (settingsPanel == null) return;
        isPanelOpen = false;
        settingsPanel.SetActive(false);
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
}