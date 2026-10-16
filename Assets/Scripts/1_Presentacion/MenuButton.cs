using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnMenuClicked);
        }
    }

    public void OnMenuClicked()
    {
        if (SettingsPanelManager.Instance != null)
        {
            SettingsPanelManager.Instance.TogglePanel();
        }
        else
        {
            Debug.LogError("MenuButton: No hay SettingsPanelManager en la escena.");
        }
    }
}