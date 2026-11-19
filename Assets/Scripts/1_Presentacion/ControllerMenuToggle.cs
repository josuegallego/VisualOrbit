using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class ControllerMenuToggle : MonoBehaviour
{
    [Header("Atajo para simulador / editor")]
    [Tooltip("Tecla que abre y cierra la configuracion en el PC. Se ignora mientras escribes en un campo de texto.")]
#if ENABLE_INPUT_SYSTEM
    public UnityEngine.InputSystem.Key teclaSimulador = UnityEngine.InputSystem.Key.P;
#endif

    // Si la escena en la que arrancas no trae este componente (solo esta en "inicio"),
    // se crea solo y persiste entre escenas. Asi el atajo funciona desde cualquier escena.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CrearSiHaceFalta()
    {
        if (instancia != null || FindFirstObjectByType<ControllerMenuToggle>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("ControllerMenuToggle (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<ControllerMenuToggle>();
    }

    static ControllerMenuToggle instancia;
    static int ultimoFrameToggle = -1;

    // Si hay dos copias (la de la escena + la auto-creada), cada pulsacion abria y cerraba el panel
    // en el mismo fotograma. Solo vive una; y nunca se alterna dos veces en el mismo fotograma.
    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(this); return; }
        instancia = this;
    }

    static void Alternar()
    {
        if (Time.frameCount == ultimoFrameToggle) return;
        ultimoFrameToggle = Time.frameCount;
        if (SettingsPanelManager.Instance != null) SettingsPanelManager.Instance.TogglePanel();
        else Debug.LogWarning("ControllerMenuToggle: no hay SettingsPanelManager en esta escena.");
    }

    bool estabaPresionado;
    readonly List<InputDevice> dispositivos = new List<InputDevice>();

    void Update()
    {
        dispositivos.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, dispositivos);

        bool presionado = false;
        foreach (var d in dispositivos)
        {
            if (d.TryGetFeatureValue(CommonUsages.menuButton, out bool menu) && menu) presionado = true;
            if (d.TryGetFeatureValue(CommonUsages.secondaryButton, out bool y) && y) presionado = true;
        }

        if (presionado && !estabaPresionado)
        {
            Debug.Log("[ControllerMenuToggle] Boton Y/menu pulsado. SettingsPanelManager: " + (SettingsPanelManager.Instance != null));
            Alternar();
        }

        estabaPresionado = presionado;

#if ENABLE_INPUT_SYSTEM
        // Atajo para el simulador / editor (configurable en el inspector, por defecto P)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb[teclaSimulador].wasPressedThisFrame && !EscribiendoEnCampo()
            )
        {
            Alternar();
        }
#endif
    }

    // Evita que la tecla se active cuando el jugador esta escribiendo su nombre.
    static bool EscribiendoEnCampo()
    {
        var es = EventSystem.current;
        if (es == null || es.currentSelectedGameObject == null) return false;
        var tmp = es.currentSelectedGameObject.GetComponent<TMP_InputField>();
        if (tmp != null && tmp.isFocused) return true;
        var ui = es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>();
        return ui != null && ui.isFocused;
    }
}
