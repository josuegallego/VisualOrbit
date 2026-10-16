using UnityEngine;
using System.Collections.Generic;

public class PanelHider : MonoBehaviour
{
    public static PanelHider Instance { get; private set; }

    private List<GameObject> hiddenPanels = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Oculta todos los paneles activos excepto el panel de configuración.
    /// </summary>
    public void HideActivePanels(GameObject configPanel)
    {
        hiddenPanels.Clear();

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.gameObject == configPanel) continue;
                if (child.name == "ConfigObj") continue;
                if (child.name == "B.Menu") continue;

                if (child.gameObject.activeSelf)
                {
                    hiddenPanels.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log($"[PanelHider] Paneles ocultados: {hiddenPanels.Count}");
    }

    /// <summary>
    /// Vuelve a mostrar los paneles que se ocultaron.
    /// </summary>
    public void RestoreHiddenPanels()
    {
        foreach (GameObject panel in hiddenPanels)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        Debug.Log($"[PanelHider] Paneles restaurados: {hiddenPanels.Count}");
        hiddenPanels.Clear();
    }
}