
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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

    [Header("Confirmacion con Enter del teclado")]
    [Tooltip("El boton se activa solo despues de escribir el nombre Y pulsar Enter en el teclado VR.")]
    [SerializeField] private bool requireKeyboardSubmit = true;
    [SerializeField] private string submitHintText = "Presiona Enter en el teclado para continuar";
    [Tooltip("Opacidad del boton mientras esta bloqueado (0.1 = casi invisible).")]
    [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.08f;

    [Header("Eventos")]
    public UnityEvent onConfirmed;

    private string confirmedName;
    private KeyboardSubmitHandler submitHandler;
    private CanvasGroup startGroup;
    private float targetAlpha = 1f;

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

        // El boton se ve atenuado mientras esta bloqueado
        startGroup = startButton.GetComponent<CanvasGroup>();
        if (startGroup == null) startGroup = startButton.gameObject.AddComponent<CanvasGroup>();

        if (requireKeyboardSubmit)
        {
            // Se confirma por cualquiera de estas vias: Enter del teclado VR, Enter normal, o al cerrar/dejar el campo
            submitHandler = nameInput.GetComponent<KeyboardSubmitHandler>();
            if (submitHandler != null) submitHandler.onSubmit.AddListener(OnKeyboardSubmitted);
            nameInput.onSubmit.AddListener(OnKeyboardSubmitted);
            nameInput.onEndEdit.AddListener(OnKeyboardSubmitted);
        }

        nameInput.onValueChanged.AddListener(OnNameChanged);
        startButton.onClick.AddListener(OnStartClicked);

        RefreshState();
        startGroup.alpha = targetAlpha;
    }

    private void Update()
    {
        if (startGroup == null) return;
        startGroup.alpha = Mathf.MoveTowards(startGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 4f);
    }

    private void Start()
    {
        if (!useSavedName && requireKeyboardSubmit) StartCoroutine(HookXRKeyboard());
    }

    // El teclado VR de ejemplo vive en otro "assembly" con sus propios simbolos, asi que se conecta
    // por reflexion: funciona igual aunque este script no pueda ver sus tipos.
    private Component xrKeyboard;
    private readonly List<KeyValuePair<object, Delegate>> xrSubscriptions =
        new List<KeyValuePair<object, Delegate>>();

    private IEnumerator HookXRKeyboard()
    {
        float t = 0f;
        while (xrKeyboard == null && t < 5f)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "XRKeyboard") { xrKeyboard = mb; break; }
            }

            t += Time.unscaledDeltaTime;
            if (xrKeyboard == null) yield return null;
        }

        if (xrKeyboard == null)
        {
            Debug.LogWarning("[NameInputGate] No encuentro el teclado VR (XRKeyboard) en la escena.", this);
            yield break;
        }

        bool a = SubscribeXR("onTextSubmitted");
        bool c = SubscribeXR("onClosed");
        Debug.Log($"[NameInputGate] Conectado al teclado VR '{xrKeyboard.name}' (Enviar={a}, Cerrar={c}).", this);
    }

    private bool SubscribeXR(string eventProperty)
    {
        try
        {
            var prop = xrKeyboard.GetType().GetProperty(eventProperty);
            object evt = prop != null ? prop.GetValue(xrKeyboard) : null;
            if (evt == null) return false;

            // UnityEvent<TArgs>: se busca TArgs subiendo por la herencia
            Type argType = null;
            for (Type tp = evt.GetType(); tp != null; tp = tp.BaseType)
            {
                if (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(UnityEvent<>))
                {
                    argType = tp.GetGenericArguments()[0];
                    break;
                }
            }
            if (argType == null) return false;

            var method = typeof(NameInputGate)
                .GetMethod(nameof(OnXRKeyboardArgs), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(argType);
            Delegate del = Delegate.CreateDelegate(typeof(UnityAction<>).MakeGenericType(argType), this, method);

            evt.GetType().GetMethod("AddListener").Invoke(evt, new object[] { del });
            xrSubscriptions.Add(new KeyValuePair<object, Delegate>(evt, del));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NameInputGate] No pude conectar '{eventProperty}': {e.Message}", this);
            return false;
        }
    }

    private void OnXRKeyboardArgs<T>(T args)
    {
        string text = null;

        if (args != null)
        {
            var p = typeof(T).GetProperty("keyboardText");
            if (p != null) text = p.GetValue(args) as string;
        }

        if (string.IsNullOrEmpty(text) && xrKeyboard != null)
        {
            var p = xrKeyboard.GetType().GetProperty("text");
            if (p != null) text = p.GetValue(xrKeyboard) as string;
        }

        if (string.IsNullOrEmpty(text) && nameInput != null) text = nameInput.text;

        if (nameInput != null && !string.IsNullOrEmpty(text) && nameInput.text != text)
            nameInput.text = text;

        OnKeyboardSubmitted(text);
    }

    private void UnhookXRKeyboard()
    {
        foreach (var pair in xrSubscriptions)
        {
            try { pair.Key.GetType().GetMethod("RemoveListener").Invoke(pair.Key, new object[] { pair.Value }); }
            catch (Exception) { /* el teclado ya fue destruido */ }
        }
        xrSubscriptions.Clear();
    }

    // Se llama cuando el usuario pulsa Enter en el teclado VR
    private void OnKeyboardSubmitted(string text)
    {
        string v = trim ? (text ?? string.Empty).Trim() : text;
        if (string.IsNullOrEmpty(v)) return;

        confirmedName = v;
        Debug.Log($"[NameInputGate] Nombre confirmado: '{v}'", this);
        RefreshState();
    }

    private bool IsConfirmed(string value)
    {
        if (!requireKeyboardSubmit) return true;
        return confirmedName != null && confirmedName == value;
    }

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameChanged);

        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);

        if (submitHandler != null)
            submitHandler.onSubmit.RemoveListener(OnKeyboardSubmitted);

        UnhookXRKeyboard();

        if (nameInput != null)
        {
            nameInput.onSubmit.RemoveListener(OnKeyboardSubmitted);
            nameInput.onEndEdit.RemoveListener(OnKeyboardSubmitted);
        }
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
        bool lengthOk = value.Length >= minLength;
        bool valid = lengthOk && IsConfirmed(value);

        startButton.interactable = valid;
        targetAlpha = valid ? 1f : lockedAlpha;

        // Bloqueado: no recibe el rayo ni muestra hover, para que no parezca pulsable
        if (startGroup != null)
        {
            startGroup.interactable = valid;
            startGroup.blocksRaycasts = valid;
        }

        if (warningLabel != null)
        {
            warningLabel.text = valid ? string.Empty : (lengthOk ? submitHintText : warningText);
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

        if (value.Length < minLength || !IsConfirmed(value))
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
