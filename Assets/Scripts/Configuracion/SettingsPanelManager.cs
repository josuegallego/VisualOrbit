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
        // Buscamos el ConfigObj si no está asignado
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
    }

    public void OpenPanel()
    {
        if (settingsPanel == null) return;
        isPanelOpen = true;
        settingsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (settingsPanel == null) return;
        isPanelOpen = false;
        settingsPanel.SetActive(false);
    }
}