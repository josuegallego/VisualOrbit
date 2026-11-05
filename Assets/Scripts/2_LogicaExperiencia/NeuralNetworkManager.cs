using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class NeuralNetworkManager : MonoBehaviour
{
    public static NeuralNetworkManager Instance { get; private set; }

    public enum RutaDificultad { Buena, Intermedia, Mala }

    [Header("Referencias")]
    public GameObject networkRoot;
    public NeuronProgress neuronProgress;

    [Header("Indicadores Visuales de Ruta")]
    public GameObject indicadorRutaBuena;
    public GameObject indicadorRutaIntermedia;
    public GameObject indicadorRutaMala;

    [Header("Pruebas")]
    public bool useDebugOverride = false;
    [Range(0f, 1f)] public float debugPerformance01 = 1f;

    [Header("Calibracion del desempeno")]
    [Range(0f, 1f)] public float rawWorst = 0f;
    [Range(0f, 1f)] public float rawBest = 1f;

    public float RawPerformance { get; private set; } = 1f;

    [Header("Redondeo a extremos (temporal)")]
    public bool roundToExtremes = false;
    [Range(0f, 1f)] public float extremeThreshold = 0.5f;

    [Header("Escena de prueba")]
    public bool autoStart = false;

    [Header("Tiempo segun estado cognitivo")]
    public float umbralOptimo = 70f;
    public float umbralDeficiente = 39f;
    public float tiempoOptimo = 120f;
    public float tiempoMedio = 90f;
    public float tiempoDeficiente = 60f;

    [Header("UI del Temporizador")]
    public TMP_Text textoTiempo;
    public Image barraTiempo;
    public Color colorTiempo = Color.white;
    [Range(0f, 1f)] public float brilloTiempo = 0.9f;
    public bool forzarTimerBlanco = true;

    [Header("UI del Progreso (opcional)")]
    public TMP_Text textoProgreso;
    public Image barraProgreso;

    [Header("Paneles de resultado")]
    public GameObject panelCorrecto;
    public GameObject panelIncorrecto;
    [Min(0f)] public float retardoPanel = 0.8f;
    [Min(0f)] public float duracionFundidoPanel = 0.3f;

    [Header("Eventos")]
    public UnityEvent alIniciar;
    public UnityEvent alGanar;
    public UnityEvent alPerderPorTiempo;

    public float Performance01 { get; private set; } = 1f;
    public float Difficulty01 => 1f - Performance01;
    public bool IsStarted { get; private set; }
    public bool IsFinished { get; private set; }
    public RutaDificultad Ruta { get; private set; } = RutaDificultad.Intermedia;
    public float TiempoTotal { get; private set; }
    public float TiempoRestante { get; private set; }

    bool temporizadorActivo;
    Coroutine rutinaBarraProgreso;

    void Awake()
    {
        Instance = this;
        if (networkRoot != null) networkRoot.SetActive(false);
        ApagarIndicadores();
        OcultarPaneles();
        if (neuronProgress == null)
            neuronProgress = FindFirstObjectByType<NeuronProgress>(FindObjectsInactive.Include);
    }

    void Start()
    {
        if (autoStart) StartGame();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        temporizadorActivo = false;
    }

    void OnDestroy()
    {
        if (neuronProgress != null) neuronProgress.ProgressChanged -= AlCambiarProgreso;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!temporizadorActivo) return;

        TiempoRestante -= Time.deltaTime;
        if (TiempoRestante < 0f) TiempoRestante = 0f;

        ActualizarUITiempo();

        if (TiempoRestante <= 0f)
        {
            temporizadorActivo = false;
            Perder();
        }
    }

    public void StartGame()
    {
        if (IsStarted) return;
        IsStarted = true;
        IsFinished = false;

        ReadPerformance();
        Ruta = DeterminarRuta();
        ConfigurarIndicadores(Ruta);
        IniciarTemporizador();

        if (neuronProgress != null)
        {
            neuronProgress.ProgressChanged -= AlCambiarProgreso;
            neuronProgress.ProgressChanged += AlCambiarProgreso;
            ActualizarUIProgreso(neuronProgress.Progress01, false);
        }

        if (networkRoot != null) networkRoot.SetActive(true);

        alIniciar?.Invoke();
    }

    void AlCambiarProgreso(float progreso01)
    {
        if (!IsStarted || IsFinished) return;
        ActualizarUIProgreso(progreso01, true);
        if (neuronProgress != null && neuronProgress.IsComplete)
            Ganar();
    }

    void Ganar()
    {
        if (IsFinished) return;
        IsFinished = true;
        temporizadorActivo = false;
        alGanar?.Invoke();
        MostrarPanel(panelCorrecto);

        // GUARDAR TIEMPO Y ESTADO (GANADO) EN EL JSON
        if (PlayerDataStore.ActiveSession != null)
        {
            float tiempoJugado = TiempoTotal - TiempoRestante;
            PlayerDataStore.SetTiempoMolecula(tiempoJugado, true);
        }
    }

    void Perder()
    {
        if (IsFinished) return;
        IsFinished = true;
        alPerderPorTiempo?.Invoke();
        MostrarPanel(panelIncorrecto);

        // GUARDAR TIEMPO Y ESTADO (FALLIDO) EN EL JSON
        if (PlayerDataStore.ActiveSession != null)
            PlayerDataStore.SetTiempoMolecula(TiempoTotal, false);
    }

    void MostrarPanel(GameObject panel)
    {
        if (panel == null) return;
        StartCoroutine(PanelRutina(panel));
    }

    IEnumerator PanelRutina(GameObject panel)
    {
        if (retardoPanel > 0f) yield return new WaitForSecondsRealtime(retardoPanel);

        var grupo = panel.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = panel.AddComponent<CanvasGroup>();

        grupo.alpha = duracionFundidoPanel > 0f ? 0f : 1f;
        panel.SetActive(true);

        float t = 0f;
        while (t < duracionFundidoPanel)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Clamp01(t / duracionFundidoPanel);
            yield return null;
        }
        grupo.alpha = 1f;
    }

    void OcultarPaneles()
    {
        if (panelCorrecto != null) panelCorrecto.SetActive(false);
        if (panelIncorrecto != null) panelIncorrecto.SetActive(false);
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        OcultarPaneles();
        IsFinished = false;
        ReadPerformance();
        Ruta = DeterminarRuta();
        ConfigurarIndicadores(Ruta);
        IniciarTemporizador();
        if (neuronProgress != null) ActualizarUIProgreso(neuronProgress.Progress01, false);
    }

    float Calibrate(float raw)
    {
        if (rawBest - rawWorst < 0.05f) return raw;
        return Mathf.Clamp01(Mathf.InverseLerp(rawWorst, rawBest, raw));
    }

    [ContextMenu("Leer desempeno (prueba)")]
    public void ReadPerformance()
    {
        if (useDebugOverride)
        {
            Performance01 = debugPerformance01;
            return;
        }

        if (NutritionPerformance.TryRead(out float performance, out float difficulty))
        {
            RawPerformance = performance;
            Performance01 = Calibrate(performance);
            if (roundToExtremes) Performance01 = Performance01 >= extremeThreshold ? 1f : 0f;
        }
        else
        {
            Performance01 = 1f;
        }
    }

    RutaDificultad DeterminarRuta()
    {
        var s = PlayerStats.Instance;

        if (!useDebugOverride && s != null)
        {
            bool buena = s.energy > 70f && s.interference < 30f && s.memory > 70f;
            if (buena) return RutaDificultad.Buena;

            bool mala = s.energy < 40f || s.interference > 60f || s.memory < 40f;
            if (mala) return RutaDificultad.Mala;

            return RutaDificultad.Intermedia;
        }

        float estado = Performance01 * 100f;
        if (estado >= umbralOptimo) return RutaDificultad.Buena;
        if (estado <= umbralDeficiente) return RutaDificultad.Mala;
        return RutaDificultad.Intermedia;
    }

    void ApagarIndicadores()
    {
        if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(false);
        if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(false);
        if (indicadorRutaMala != null) indicadorRutaMala.SetActive(false);
    }

    void ConfigurarIndicadores(RutaDificultad ruta)
    {
        ApagarIndicadores();

        switch (ruta)
        {
            case RutaDificultad.Buena:
                if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(true);
                break;
            case RutaDificultad.Intermedia:
                if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(true);
                break;
            case RutaDificultad.Mala:
                if (indicadorRutaMala != null) indicadorRutaMala.SetActive(true);
                break;
        }
    }

    float EstadoCognitivoPorcentaje()
    {
        if (useDebugOverride) return Performance01 * 100f;

        var s = PlayerStats.Instance;
        if (s == null) return Performance01 * 100f;
        return (s.energy + s.memory + (100f - s.interference)) / 3f;
    }

    float CalcularTiempoPorEstado()
    {
        float estado = EstadoCognitivoPorcentaje();
        float t;
        if (estado >= umbralOptimo) t = tiempoOptimo;
        else if (estado <= umbralDeficiente) t = tiempoDeficiente;
        else t = tiempoMedio;
        return t;
    }

    void IniciarTemporizador()
    {
        TiempoTotal = CalcularTiempoPorEstado();
        TiempoRestante = TiempoTotal;
        temporizadorActivo = true;

        if (barraTiempo != null) barraTiempo.fillAmount = 1f;
        if (textoTiempo != null)
        {
            if (forzarTimerBlanco) QuitarEfectosDeColorAlTimer();
            textoTiempo.color = new Color(colorTiempo.r * brilloTiempo, colorTiempo.g * brilloTiempo, colorTiempo.b * brilloTiempo, 1f);
            textoTiempo.text = $"{TiempoTotal:F0}s";
        }
    }

    void ActualizarUITiempo()
    {
        if (textoTiempo != null) textoTiempo.text = $"{TiempoRestante:F0}s";
        if (barraTiempo != null && TiempoTotal > 0f) barraTiempo.fillAmount = TiempoRestante / TiempoTotal;
    }

    void QuitarEfectosDeColorAlTimer()
    {
        var pulse = textoTiempo.GetComponent<TextGlowPulse>();
        if (pulse != null) pulse.enabled = false;

        Material m = textoTiempo.fontMaterial;
        m.DisableKeyword("GLOW_ON");
        if (m.HasProperty("_FaceColor")) m.SetColor("_FaceColor", Color.white);
    }

    void ActualizarUIProgreso(float progreso01, bool animar)
    {
        if (textoProgreso != null)
            textoProgreso.text = $"Neuronas: {progreso01 * 100f:F0}%";

        if (barraProgreso != null)
        {
            if (rutinaBarraProgreso != null) StopCoroutine(rutinaBarraProgreso);

            if (animar && isActiveAndEnabled) rutinaBarraProgreso = StartCoroutine(AnimarBarraProgreso(progreso01));
            else barraProgreso.fillAmount = progreso01;
        }
    }

    IEnumerator AnimarBarraProgreso(float objetivo)
    {
        float inicial = barraProgreso.fillAmount;
        float t = 0f;
        const float duracion = 0.5f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            barraProgreso.fillAmount = Mathf.Lerp(inicial, objetivo, t / duracion);
            yield return null;
        }
        barraProgreso.fillAmount = objetivo;
        rutinaBarraProgreso = null;
    }
}