using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en cada opcion (tarjeta). Solo define: efectos en las barras + texto de retroalimentacion.
/// Todo lo demas (guardar la decision, bloquear las otras tarjetas, abrir el panel y
/// decidir a donde va Continuar) lo resuelve solo o lo decide el FeedbackPanel.
/// </summary>
public class DecisionCard : MonoBehaviour
{
    [Header("Boton de la tarjeta")]
    public Button button;

    [Header("Efectos sobre las barras (positivo sube, negativo baja)")]
    public float energyChange = 0f;
    public float interferenceChange = 0f;
    public float memoryChange = 0f;

    [Header("Retroalimentacion")]
    public FeedbackPanel feedback;
    public string feedbackTitle = "";
    [TextArea(3, 8)] public string feedbackBody = "";

    bool used;

    void Reset() { button = GetComponentInChildren<Button>(); }

    void Awake()
    {
        if (button == null) button = GetComponentInChildren<Button>();
        if (button != null) button.onClick.AddListener(Choose);
    }

    void OnEnable()
    {
        used = false;
        SetSiblingsInteractable(true);
    }

    public void Choose()
    {
        if (used) return;       // evita doble click
        used = true;

        if (feedback == null)
        {
            Debug.LogWarning("[DecisionCard] Falta asignar el FeedbackPanel.");
            return;
        }

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ApplyEffects(energyChange, interferenceChange, memoryChange);
        else
            Debug.LogWarning("[DecisionCard] No hay PlayerStats en la escena.");

        // Numero de esta opcion (1, 2 o 3) segun su posicion entre las tarjetas hermanas
        int option = 1;
        if (transform.parent != null)
        {
            var cards = transform.parent.GetComponentsInChildren<DecisionCard>(true);
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] == this) { option = i + 1; break; }
        }

        feedback.Prepare(feedbackTitle, feedbackBody, option);

        SetSiblingsInteractable(false);   // no se puede elegir otra opcion

        // Si otro evento del boton ya cerro este panel, no se puede usar la corrutina:
        // en ese caso el panel de retroalimentacion ya lo abrio ese otro evento.
        if (isActiveAndEnabled) StartCoroutine(OpenAfterDelay());
    }

    IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSecondsRealtime(feedback.openDelay);
        feedback.Open(PanelFader.RootPanelOf(transform));
    }

    void SetSiblingsInteractable(bool value)
    {
        if (transform.parent == null) return;
        foreach (var c in transform.parent.GetComponentsInChildren<DecisionCard>(true))
            if (c.button != null) c.button.interactable = value;
    }
}
