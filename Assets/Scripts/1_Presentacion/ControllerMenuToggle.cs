using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class ControllerMenuToggle : MonoBehaviour
{
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
        // Atajo para el simulador / editor: tecla F1 abre y cierra la configuracion
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame && SettingsPanelManager.Instance != null)
            SettingsPanelManager.Instance.TogglePanel();
#endif
    }
}