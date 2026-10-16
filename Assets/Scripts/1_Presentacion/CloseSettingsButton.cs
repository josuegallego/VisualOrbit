using UnityEngine;
using UnityEngine.UI;

public class CloseSettingsButton : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnCloseClicked);
        }
    }

    public void OnCloseClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.RevertToSaved();
        }

        if (SettingsPanelManager.Instance != null)
        {
            SettingsPanelManager.Instance.ClosePanel();
            Debug.Log("Panel de configuración cerrado (cambios revertidos).");
        }
        else
        {
            Debug.LogError("CloseSettingsButton: No hay SettingsPanelManager en la escena.");
        }
    }
}