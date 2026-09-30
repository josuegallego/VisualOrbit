using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class KeyboardSubmitHandler : MonoBehaviour
{
    [Tooltip("Se dispara cuando el usuario confirma con Enter en el teclado.")]
    public UnityEvent<string> onSubmit;

    private TMP_InputField input;

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
    }

    /// <summary>
    /// Este método lo llama el evento "On Text Submitted" del XRKeyboardDisplay.
    /// Recibe el texto confirmado y evita que el campo pierda el foco visual.
    /// </summary>
    public void OnKeyboardSubmit(string submittedText)
    {
        if (string.IsNullOrEmpty(submittedText)) return;

        Debug.Log($"[KeyboardSubmitHandler] ✅ Texto confirmado: '{submittedText}'");

        // 1. Forzar que el texto se quede escrito
        input.text = submittedText;
        input.ForceLabelUpdate();

        // 2. Volver a forzar el foco para que el Placeholder no aparezca
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(input.gameObject);
        }
        input.Select();
        input.ActivateInputField();
        input.caretPosition = input.text.Length;
        input.ForceLabelUpdate();

        // 3. Disparar el evento
        onSubmit?.Invoke(submittedText);
    }
}