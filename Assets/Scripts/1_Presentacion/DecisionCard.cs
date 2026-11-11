using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en cada opcion (tarjeta). Define: efectos en las barras + texto de retroalimentacion
/// + que opcion es (para guardarla en el JSON y que la enfermera la nombre al final).
/// Todo lo demas (bloquear las otras tarjetas, abrir el panel y decidir a donde va Continuar)
/// lo resuelve solo o lo decide el FeedbackPanel.
/// </summary>
public class DecisionCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    [Header("Boton de la tarjeta")]
    public Button button;

    [Header("Efectos sobre las barras (positivo sube, negativo baja)")]
    public float energyChange = 0f;
    public float interferenceChange = 0f;
    public float memoryChange = 0f;

    [Header("Registro de la decision (lo que dira la enfermera)")]
    [Tooltip("Momento del dia: desayuno, almuerzo, cena... Si lo dejas vacio se usa el nombre del panel.")]
    public string momento = "";
    [Tooltip("Nombre de lo que eligio: cafe, jugo de naranja, huevos con papa... Si lo dejas vacio se usa el titulo de la retroalimentacion.")]
    public string nombreOpcion = "";
    [Tooltip("Si esta activo, se calcula solo: es saludable si sube energia/memoria y baja interferencia en conjunto.")]
    public bool calcularSaludableAutomatico = true;
    [Tooltip("Solo se usa si 'Calcular Saludable Automatico' esta apagado.")]
    public bool esSaludable = true;

    [Header("Vibracion (HU 4.2.1)")]
    [Tooltip("Vibracion suave en los controles al elegir (clic) esta decision.")]
    public bool vibrarAlElegir = true;
    [Tooltip("Roce muy suave en los controles al apuntar a la tarjeta (mientras se puede elegir).")]
    public bool vibrarAlPasarPuntero = true;

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

    // El puntero (rayo del control) entra en la tarjeta o en cualquiera de sus hijos
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (!vibrarAlPasarPuntero || used) return;
        if (button != null && !button.IsInteractable()) return;
        HapticService.Tick();
    }

    public void Choose()
    {
        if (used) return;       // evita doble click
        used = true;

        if (vibrarAlElegir) HapticService.Soft();

        // Guardar QUE eligio el jugador (queda en el JSON)
        RegistrarEnStore();

        if (feedback == null)
        {
            Debug.LogWarning("[DecisionCard] Falta asignar el FeedbackPanel.");
            return;
        }

        // Numero de esta opcion (1, 2 o 3) segun su posicion entre las tarjetas hermanas
        int option = 1;
        if (transform.parent != null)
        {
            var cards = transform.parent.GetComponentsInChildren<DecisionCard>(true);
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] == this) { option = i + 1; break; }
        }

        feedback.Prepare(feedbackTitle, feedbackBody, option, energyChange, interferenceChange, memoryChange);

        SetSiblingsInteractable(false);   // no se puede elegir otra opcion

        // Si otro evento del boton ya cerro este panel, no se puede usar la corrutina:
        // en ese caso el panel de retroalimentacion ya lo abrio ese otro evento.
        if (isActiveAndEnabled) StartCoroutine(OpenAfterDelay());
    }

    void RegistrarEnStore()
    {
        string m = momento;
        if (string.IsNullOrWhiteSpace(m))
        {
            var root = PanelFader.RootPanelOf(transform);
            m = root != null ? root.name : "decision";
        }

        string o = string.IsNullOrWhiteSpace(nombreOpcion) ? feedbackTitle : nombreOpcion;
        if (string.IsNullOrWhiteSpace(o)) o = gameObject.name;

        bool saludable = calcularSaludableAutomatico
            ? (energyChange + memoryChange - interferenceChange) >= 0f
            : esSaludable;

        PlayerDataStore.RegistrarDecision(m.Trim(), o.Trim(), saludable);
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