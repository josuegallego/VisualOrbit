using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Vive en el panel de inicio. Bloquea el botón "Iniciar Experiencia"
/// hasta que el TMP_InputField tenga un nombre válido.
/// Al confirmar, guarda el nombre en PlayerDataStore (JSON).
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

        // Al cambiar el texto, re-evaluamos el estado del botón.
        nameInput.onValueChanged.AddListener(OnNameChanged);
        // Al pulsar el botón, primero validamos + guardamos y luego disparamos el evento.
        startButton.onClick.AddListener(OnStartClicked);

        // Restaurar el nombre guardado previamente (si existe).
        var saved = PlayerDataStore.Current.nombre;
        if (!string.IsNullOrEmpty(saved))
            nameInput.text = saved;

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

        // Feedback visual opcional en el botón.
        var img = startButton.targetGraphic as Image;
        if (img != null && !valid)
            img.color = disabledColor;
    }

    private void OnStartClicked()
    {
        string value = trim ? nameInput.text.Trim() : nameInput.text;

        // Doble check por seguridad (por si alguien llama al botón por código).
        if (value.Length < minLength)
        {
            RefreshState();
            return;
        }

        // 1) Guardar en JSON
        PlayerDataStore.Current.nombre = value;
        PlayerDataStore.Current.fechaInicio = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        PlayerDataStore.Save();

        // 2) Disparar la navegación (conecta Next/GoTo en el Inspector)
        onConfirmed?.Invoke();
    }
}