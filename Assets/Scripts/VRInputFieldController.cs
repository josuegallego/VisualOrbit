using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Controlador único y permanente para el TMP_InputField en VR.
/// Maneja:
///   - Selección forzada al hacer clic (para que el XRKeyboardDisplay abra el teclado).
///   - Confirmación desde el XRKeyboardDisplay (Enter).
///   - Bloqueo del caret de TMP (que titila y mueve el texto).
///   - Bloqueo de la posición del Text/Placeholder.
///
/// Es el ÚNICO script que debe tocar el TMP_InputField.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class VRInputFieldController : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("Comportamiento")]
    [Tooltip("Fuerza la selección del InputField al recibir un clic (abre el teclado).")]
    [SerializeField] private bool forceSelectionOnClick = true;

    [Tooltip("Bloquea el caret de TMP para que no titile ni mueva el texto.")]
    [SerializeField] private bool disableCaret = true;

    [Tooltip("Bloquea la posición del Text y Placeholder en su sitio original.")]
    [SerializeField] private bool lockTextPosition = true;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando el usuario confirma el texto (Enter en el teclado).")]
    public UnityEvent<string> onSubmit;

    // ---------------------------------------------------------------- Interno

    private TMP_InputField input;
    private RectTransform textRT;
    private RectTransform placeholderRT;

    private Vector2 lockedTextPos, lockedPlaceholderPos;
    private Vector3 lockedTextScale, lockedPlaceholderScale;
    private bool positionsCaptured;

    private bool caretFound;
    private TMP_SelectionCaret caret;

    // ---------------------------------------------------------------- Ciclo de vida

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
        if (input.textComponent != null) textRT = input.textComponent.rectTransform;
        if (input.placeholder != null) placeholderRT = input.placeholder.rectTransform;
    }

    private void Start()
    {
        // Capturamos posiciones tras un pequeño delay para que el layout se haya calculado.
        Invoke(nameof(CapturePositions), 0.1f);
    }

    private void CapturePositions()
    {
        if (textRT != null)
        {
            lockedTextPos = textRT.anchoredPosition;
            lockedTextScale = textRT.localScale;
        }
        if (placeholderRT != null)
        {
            lockedPlaceholderPos = placeholderRT.anchoredPosition;
            lockedPlaceholderScale = placeholderRT.localScale;
        }
        positionsCaptured = true;
        Debug.Log($"[VRInputField] 🔒 Posiciones capturadas. Text={lockedTextPos}, Placeholder={lockedPlaceholderPos}");
    }

    private void LateUpdate()
    {
        if (lockTextPosition && positionsCaptured)
        {
            if (textRT != null)
            {
                if (textRT.anchoredPosition != lockedTextPos) textRT.anchoredPosition = lockedTextPos;
                if (textRT.localScale != lockedTextScale) textRT.localScale = lockedTextScale;
            }
            if (placeholderRT != null)
            {
                if (placeholderRT.anchoredPosition != lockedPlaceholderPos) placeholderRT.anchoredPosition = lockedPlaceholderPos;
                if (placeholderRT.localScale != lockedPlaceholderScale) placeholderRT.localScale = lockedPlaceholderScale;
            }
        }

        if (disableCaret)
        {
            KillCaret();
        }
    }

    /// <summary>
    /// Desactiva el caret de TMP. Lo busca la primera vez y luego reutiliza la referencia.
    /// </summary>
    private void KillCaret()
    {
        if (!caretFound || caret == null)
        {
            caret = GetComponentInChildren<TMP_SelectionCaret>(true);
            caretFound = caret != null;
        }

        if (caret != null && caret.enabled)
        {
            caret.enabled = false;
        }
    }

    // ---------------------------------------------------------------- Interacción

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!forceSelectionOnClick) return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);

        input.Select();
        input.ActivateInputField();
        input.caretPosition = input.text.Length;
        input.ForceLabelUpdate();
    }

    public void OnSelect(BaseEventData eventData) { }
    public void OnDeselect(BaseEventData eventData) { }

    // ---------------------------------------------------------------- Submit del teclado

    /// <summary>
    /// Conectar en "XRKeyboardDisplay → Keyboard Display Events → On Text Submitted".
    /// </summary>
    public void OnKeyboardSubmit(string submittedText)
    {
        if (string.IsNullOrEmpty(submittedText)) return;

        Debug.Log($"[VRInputField] ✅ Texto confirmado: '{submittedText}'");

        // NO reactivamos el InputField aquí: eso es lo que recreaba el caret.
        // Solo escribimos el texto y disparamos el evento.
        input.text = submittedText;
        input.ForceLabelUpdate();

        onSubmit?.Invoke(submittedText);
    }
}