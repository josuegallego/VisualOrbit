using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Boton "Guardar y sincronizar": guarda los ajustes, da feedback (vibracion, pulso del boton y aviso)
/// y cierra el panel de configuracion.
/// </summary>
public class SaveButton : MonoBehaviour
{
    [Header("Referencias")]
    public Slider brightnessSlider;
    public Slider fovSlider;
    public Slider voiceSlider;
    public Slider effectsSlider;
    public Toggle spatialMusicToggle;

    [Header("Aviso")]
    [Tooltip("Objeto con el texto 'Guardado' (opcional). Se muestra mientras dura el feedback.")]
    public GameObject avisoGuardado;

    [Header("Feedback")]
    [Tooltip("Vibracion de acierto en los controles al guardar")]
    public bool vibrar = true;
    [Tooltip("Sonido opcional al guardar")]
    public AudioSource sonidoGuardado;
    [Tooltip("Cuanto crece el boton en el pulso (1 = nada)")]
    public float escalaPulso = 1.12f;
    [Tooltip("Segundos que se ve el feedback antes de cerrar el panel")]
    public float cerrarDespues = 0.9f;
    [Tooltip("Cerrar el panel despues de guardar")]
    public bool cerrarPanel = true;

    [Tooltip("Reemplaza lo que tenga el OnClick del boton (por ejemplo un SetActive(false) viejo) por Guardar + cerrar bien.")]
    public bool autoConectar = true;

    Coroutine rutina;
    Vector3 escalaOriginal;
    bool escalaGuardada;

    void Awake()
    {
        if (!autoConectar) return;
        var boton = GetComponent<Button>();
        if (boton == null) return;
        // new = descarta las llamadas persistentes del inspector (el SetActive(false) viejo apagaba el panel
        // sin avisar al SettingsPanelManager y sin restaurar los paneles anteriores)
        boton.onClick = new Button.ButtonClickedEvent();
        boton.onClick.AddListener(OnSaveClicked);
    }

    void OnDisable()
    {
        // Deja el boton en su tamano normal si el panel se cierra a mitad del feedback
        if (escalaGuardada) transform.localScale = escalaOriginal;
        if (avisoGuardado != null) avisoGuardado.SetActive(false);
        rutina = null;
    }

    public void OnSaveClicked()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SaveButton: No hay SettingsManager en la escena.");
            return;
        }

        float brightness = brightnessSlider != null ? brightnessSlider.value : 0.5f;
        float fov = fovSlider != null ? fovSlider.value : 0.4f;
        float voice = voiceSlider != null ? voiceSlider.value : 1f;
        float effects = effectsSlider != null ? effectsSlider.value : 1f;
        bool spatialMusic = spatialMusicToggle != null ? spatialMusicToggle.isOn : true;

        SettingsManager.Instance.SaveSettings(brightness, fov, voice, effects, spatialMusic);
        Debug.Log("Configuracion guardada y sincronizada.");

        if (vibrar) HapticService.Success();
        if (sonidoGuardado != null) sonidoGuardado.Play();

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Feedback());
    }

    IEnumerator Feedback()
    {
        if (!escalaGuardada) { escalaOriginal = transform.localScale; escalaGuardada = true; }
        if (avisoGuardado != null) avisoGuardado.SetActive(true);

        // Pulso: crece y vuelve a su tamano
        float dur = 0.22f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
            transform.localScale = escalaOriginal * Mathf.Lerp(1f, escalaPulso, k);
            yield return null;
        }
        transform.localScale = escalaOriginal;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, cerrarDespues - dur));

        if (avisoGuardado != null) avisoGuardado.SetActive(false);

        if (cerrarPanel && SettingsPanelManager.Instance != null)
            SettingsPanelManager.Instance.ClosePanel();   // desactiva el panel: es lo ultimo que se hace
        rutina = null;
    }
}
