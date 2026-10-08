using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Va en el panel de retroalimentacion de CADA escena de decisiones
/// (Decisiones = desayuno, Decisiones 2 = almuerzo, Decisiones 3 = cena...).
/// - Las tarjetas le pasan titulo y texto.
/// - Guarda la decision en el JSON de la sesion.
/// - El boton Continuar (On Click -> FeedbackPanel.Continue) va a la escena o panel siguiente.
/// </summary>
public class FeedbackPanel : MonoBehaviour
{
    [Header("Textos del panel")]
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Tooltip("Pausa corta tras elegir una opcion, antes de abrir este panel")]
    public float openDelay = 0.6f;

    [Header("Registro")]
    [Tooltip("0 = desayuno, 1 = almuerzo, 2 = cena")]
    public int decisionIndex = 0;

    [Header("Al presionar Continuar (usa UNO de los dos)")]
    [Tooltip("Nombre exacto de la escena siguiente (debe estar en Build Settings). Ej: Decisiones 2")]
    public string nextScene = "";
    [Tooltip("O bien, un panel de esta misma escena")]
    public GameObject nextPanel;

    // Efectos pendientes: se aplican cuando el panel aparece, para que las barras
    // se muevan (y suenen) aqui y no en el panel de decisiones.
    bool hasPending;
    float pEnergy, pInterference, pMemory;

    void OnEnable()
    {
        if (!hasPending) return;
        hasPending = false;

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ApplyEffects(pEnergy, pInterference, pMemory);
        else
            Debug.LogWarning("[FeedbackPanel] No hay PlayerStats en la escena.", this);
    }

    // La tarjeta llama a esto al elegir
    public void Prepare(string title, string body, int optionNumber, float dEnergy, float dInterference, float dMemory)
    {
        hasPending = true;
        pEnergy = dEnergy; pInterference = dInterference; pMemory = dMemory;

        if (titleText == null) Debug.LogWarning("[FeedbackPanel] 'Title Text' esta vacio en el Inspector.", this);
        if (bodyText == null) Debug.LogWarning("[FeedbackPanel] 'Body Text' esta vacio en el Inspector.", this);

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        PlayerDataStore.SetDecision(decisionIndex, optionNumber);
    }

    // La tarjeta llama a esto despues de la pausa: oculta el panel de decisiones y muestra este
    public void Open(GameObject decisionPanel)
    {
        PanelFader.Switch(decisionPanel, gameObject);
    }

    // Conectar en el On Click del boton Continuar
    public void Continue()
    {
        Debug.Log("[FeedbackPanel] Continuar presionado. Escena: '" + nextScene + "' | Panel: " + (nextPanel != null ? nextPanel.name : "ninguno"), this);

        if (!string.IsNullOrEmpty(nextScene))
        {
            if (!Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Debug.LogError("[FeedbackPanel] La escena '" + nextScene + "' no esta en Build Settings (File > Build Profiles > Scene List) o el nombre no coincide.", this);
                return;
            }
            SceneManager.LoadScene(nextScene);
            return;
        }

        if (nextPanel != null)
        {
            PanelFader.Switch(gameObject, nextPanel);
            return;
        }

        Debug.LogWarning("[FeedbackPanel] No hay 'Next Scene' ni 'Next Panel' configurado.", this);
    }
}
