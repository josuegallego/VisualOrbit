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
        if (FindFirstObjectByType<ControllerMenuToggle>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("ControllerMenuToggle (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<ControllerMenuToggle>();
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

        if (presionado && !estabaPresionado && SettingsPanelManager.Instance != null)
            SettingsPanelManager.Instance.TogglePanel();

        estabaPresionado = presionado;

#if ENABLE_INPUT_SYSTEM
        // Atajo para el simulador / editor (configurable en el inspector, por defecto P)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb[teclaSimulador].wasPressedThisFrame && !EscribiendoEnCampo()
            )
        {
            if (SettingsPanelManager.Instance != null) SettingsPanelManager.Instance.TogglePanel();
            else Debug.LogWarning("ControllerMenuToggle: no hay SettingsPanelManager en esta escena.");
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
