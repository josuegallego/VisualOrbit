using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SettingsPanelManager : MonoBehaviour
{
    public static SettingsPanelManager Instance { get; private set; }

    [Header("Prefab del Panel")]
    [Tooltip("Arrastra aquí el prefab ConfigObj")]
    public GameObject settingsPanelPrefab;

    [Header("Input (opcional)")]
    [Tooltip("Acción de input para abrir/cerrar el panel (botón de menú del controlador)")]
    public InputActionReference menuButtonAction;

    [Header("Configuración")]
    [Tooltip("Si está marcado, el panel empieza oculto")]
    public bool startHidden = true;

    private GameObject currentPanelInstance;
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

        if (menuButtonAction != null && menuButtonAction.action != null)
        {
            menuButtonAction.action.performed -= OnMenuButtonPressed;
        }
    }

    void Start()
    {
        // Suscribimos al botón de menú si está asignado
        if (menuButtonAction != null && menuButtonAction.action != null)
        {
            menuButtonAction.action.performed += OnMenuButtonPressed;
            menuButtonAction.action.Enable();
        }

        SpawnPanel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPanel();
    }

    private void SpawnPanel()
    {
        if (settingsPanelPrefab == null)
        {
            Debug.LogError("SettingsPanelManager: No se ha asignado el prefab del panel.");
            return;
        }

        if (currentPanelInstance != null)
        {
            Destroy(currentPanelInstance);
        }

        currentPanelInstance = Instantiate(settingsPanelPrefab);

        // Posicionamos el panel frente a la cámara del jugador
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Distancia a la que aparecerá el panel (en metros)
            float distance = 2f;

            // Altura relativa a la cámara (0 = misma altura)
            float heightOffset = 0f;

            // Calculamos la posición delante de la cámara
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0; // Ignoramos la inclinación vertical
            forward.Normalize();

            Vector3 position = mainCamera.transform.position + forward * distance + Vector3.up * heightOffset;

            currentPanelInstance.transform.position = position;

            // Hacemos que el panel mire hacia el jugador
            currentPanelInstance.transform.rotation = Quaternion.LookRotation(forward);
        }
        else
        {
            Debug.LogWarning("SettingsPanelManager: No se encontró Main Camera.");
        }

        // Escala del panel (ajusta si es necesario)
        currentPanelInstance.transform.localScale = Vector3.one * 0.005f;

        if (startHidden)
        {
            currentPanelInstance.SetActive(false);
            isPanelOpen = false;
        }
        else
        {
            currentPanelInstance.SetActive(true);
            isPanelOpen = true;
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        TogglePanel();
    }

    public void TogglePanel()
    {
        if (currentPanelInstance == null) return;

        isPanelOpen = !isPanelOpen;
        currentPanelInstance.SetActive(isPanelOpen);
    }

    public void OpenPanel()
    {
        if (currentPanelInstance == null) return;
        isPanelOpen = true;
        currentPanelInstance.SetActive(true);
    }

    public void ClosePanel()
    {
        if (currentPanelInstance == null) return;
        isPanelOpen = false;
        currentPanelInstance.SetActive(false);
    }
}