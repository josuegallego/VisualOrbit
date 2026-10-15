using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ponlo en cualquier boton (Continuar, Iniciar experiencia, Ver opciones...).
/// Al presionarlo oculta el panel donde esta el boton y muestra 'Next Panel'.
/// No hace falta tocar el On Click: se conecta solo al Button.
/// </summary>
public class GoToPanel : MonoBehaviour
{
    [Tooltip("Panel que se muestra al presionar el boton")]
    public GameObject nextPanel;

    void Awake()
    {
        var b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(Go);
    }

    // Tambien se puede llamar desde un On Click si el script no esta en el Button
    public void Go()
    {
        if (nextPanel == null)
        {
            Debug.LogWarning("[GoToPanel] Falta asignar 'Next Panel' en " + name, this);
            return;
        }
        PanelFader.Switch(PanelFader.RootPanelOf(transform), nextPanel);
    }
}
