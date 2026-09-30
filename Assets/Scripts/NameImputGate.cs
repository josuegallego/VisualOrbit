using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Vive en el panel de inicio. Bloquea el botón "Iniciar Experiencia"
/// hasta que el TMP_InputField tenga un nombre válido.
/// Al confirmar, añade una nueva sesión al histórico (JSON).
///
/// IMPORTANTE: NO restaura el nombre guardado al iniciar. Cada sesión
/// empieza con el campo vacío para que se vea el placeholder "Tu nombre".
/// </summary>
public class NameInputGate : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button startButton;

    [Header("Reglas")]
    [Tooltip("Longitud mínima del nombre para permitir continuar.")]
    [SerializeField] private int minLength = 2;
    [Tooltip("Quitar espacios al inicio/final antes de validar.")]
    [SerializeField] private bool trim = true;

    [Header("Feedback (opcional)")]
    [SerializeField] private TMP_Text warningLabel;
    [SerializeField] private string warningText = "Escribe tu nombre para continuar";
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    [Header("Eventos")]
    [Tooltip("Se dispara cuando el nombre es válido y se guardó. Conecta aquí Next/GoTo del VRPanelNavigator.")]
    public UnityEvent onConfirmed;

    // ---------------------------------------------------------------- Ciclo de vida

    private void Awake()
    {
        if (nameInput == null || startButton == null)
        {
            Debug.LogError("[NameInputGate] Faltan referencias (nameInput / startButton).", this);
            enabled = false;
            return;
        }

        // Cada sesión empieza con el campo vacío (se ve el placeholder).
        nameInput.text = string.Empty;

        nameInput.onValueChanged.AddListener(OnNameChanged);
        startButton.onClick.AddListener(OnStartClicked);

        RefreshState();
    }

    private void OnDestroy()
    {
        if (nameInput != null) nameInput.onValueChanged.RemoveListener(OnNameChanged);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
    }

    // ---------------------------------------------------------------- Lógica

    private void OnNameChanged(string _)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        string value = trim ? nameInput.text.Trim() : nameInput.text;
        bool valid = value.Length >= minLength;

        startButton.interactable = valid;

        if (warningLabel != null)
        {
            warningLabel.text = valid ? string.Empty : warningText;
            warningLabel.gameObject.SetActive(!valid);
        }

        var img = startButton.targetGraphic as Image;
        if (img != null && !valid)
            img.color = disabledColor;
    }

    private void OnStartClicked()
    {
        string value = trim ? nameInput.text.Trim() : nameInput.text;

        if (value.Length < minLength)
        {
            RefreshState();
            return;
        }

        // 1) Añadir una nueva sesión al histórico con fecha de inicio.
        var session = PlayerDataStore.AddSession(value);
        Debug.Log($"[NameInputGate] Nueva sesión: '{session.nombre}' ({session.fechaInicio}). Total: {PlayerDataStore.Current.sesiones.Count}");

        // 2) Disparar la navegación (Next/GoTo del VRPanelNavigator).
        onConfirmed?.Invoke();
    }
}