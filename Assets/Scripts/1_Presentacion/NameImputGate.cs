
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NameInputGate : MonoBehaviour
{
    [Header("Modo de funcionamiento")]
    [Tooltip("Actívalo si el nombre ya fue guardado en una escena anterior.")]
    [SerializeField] private bool useSavedName = false;

    [Header("Referencias")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button startButton;

    [Header("Reglas")]
    [SerializeField] private int minLength = 2;
    [SerializeField] private bool trim = true;

    [Header("Feedback (opcional)")]
    [SerializeField] private TMP_Text warningLabel;
    [SerializeField] private string warningText = "Escribe tu nombre para continuar";
    [SerializeField]
    private Color disabledColor =
        new Color(0.5f, 0.5f, 0.5f, 0.6f);

    [Header("Eventos")]
    public UnityEvent onConfirmed;

    private void Awake()
    {
        // MODO 1: El nombre ya se guardó en otra escena.
        if (useSavedName)
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
                startButton.interactable = true;
            }

            if (warningLabel != null)
                warningLabel.gameObject.SetActive(false);

            // No necesitamos nameInput en este modo.
            return;
        }

        // MODO 2: Funcionamiento original con campo de nombre.
        if (nameInput == null || startButton == null)
        {
            Debug.LogError(
                "[NameInputGate] Faltan referencias (nameInput / startButton).",
                this
            );

            enabled = false;
            return;
        }

        nameInput.text = string.Empty;

        nameInput.onValueChanged.AddListener(OnNameChanged);
        startButton.onClick.AddListener(OnStartClicked);

        RefreshState();
    }

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameChanged);

        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);
    }

    private void OnNameChanged(string _)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        if (nameInput == null || startButton == null)
            return;

        string value = trim ? nameInput.text.Trim() : nameInput.text;
        bool valid = value.Length >= minLength;

        startButton.interactable = valid;

        if (warningLabel != null)
        {
            warningLabel.text = valid ? string.Empty : warningText;
            warningLabel.gameObject.SetActive(!valid);
        }

        var img = startButton.targetGraphic as Image;

        if (img != null)
            img.color = valid ? Color.white : disabledColor;
    }

    private void OnStartClicked()
    {
        // MODO 1: Ya existe un nombre guardado.
        if (useSavedName)
        {
            onConfirmed?.Invoke();
            return;
        }

        // MODO 2: Validar y guardar una nueva sesión.
        if (nameInput == null)
            return;

        string value = trim ? nameInput.text.Trim() : nameInput.text;

        if (value.Length < minLength)
        {
            RefreshState();
            return;
        }

        var session = SessionService.Start(value);

        Debug.Log(
            $"[NameInputGate] Nueva sesión: '{session.nombre}' " +
            $"({session.fechaInicio}). " +
            $"Total: {SessionService.TotalSessions}"
        );

        onConfirmed?.Invoke();
    }
}
