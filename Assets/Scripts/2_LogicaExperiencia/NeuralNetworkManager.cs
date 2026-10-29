using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor del minijuego "Conexiones Neuronales".
/// - Oculta la red al cargar la escena.
/// - Al pulsar Continuar (Button > On Click > StartGame): lee el desempeno, determina la ruta
///   (Buena / Intermedia / Mala), activa su indicador, arranca el temporizador segun el estado
///   cognitivo y muestra la red.
/// - Gana cuando NeuronProgress reporta todas las neuronas encendidas; pierde si se acaba el tiempo.
/// Ponlo en un objeto vacio en la raiz de la escena (NO dentro de NeuralNetwork).
/// </summary>
public class NeuralNetworkManager : MonoBehaviour
{
    public static NeuralNetworkManager Instance { get; private set; }

    public enum RutaDificultad { Buena, Intermedia, Mala }

    [Header("Referencias")]
    [Tooltip("Objeto NeuralNetwork con todos los conductos. Queda oculto hasta pulsar Continuar.")]
    public GameObject networkRoot;
    [Tooltip("Contador de neuronas encendidas. Vacio = se busca solo en la escena.")]
    public NeuronProgress neuronProgress;

    [Header("Indicadores Visuales de Ruta")]
    [Tooltip("Se activa si el jugador esta en la ruta buena.")]
    public GameObject indicadorRutaBuena;
    [Tooltip("Se activa si el jugador esta en la ruta intermedia.")]
    public GameObject indicadorRutaIntermedia;
    [Tooltip("Se activa si el jugador esta en la ruta mala.")]
    public GameObject indicadorRutaMala;

    [Header("Pruebas")]
    [Tooltip("Si esta activo, ignora PlayerStats y usa el valor de abajo.")]
    public bool useDebugOverride = false;
    [Range(0f, 1f)] public float debugPerformance01 = 1f;

    [Header("Calibracion del desempeno")]
    [Tooltip("Desempeno CRUDO medido con todas las opciones malas. 0 = sin ajustar.")]
    [Range(0f, 1f)] public float rawWorst = 0f;
    [Tooltip("Desempeno CRUDO medido con todas las opciones buenas. 1 = sin ajustar.")]
    [Range(0f, 1f)] public float rawBest = 1f;

    /// <summary>Desempeno tal como lo calcula PlayerStats (1 - Difficulty01), sin estirar.</summary>
    public float RawPerformance { get; private set; } = 1f;

    [Header("Redondeo a extremos (temporal)")]
    [Tooltip("Si esta activo, el desempeno final es solo 0 (malo) o 1 (bueno) segun el umbral.")]
    public bool roundToExtremes = false;
    [Tooltip("Valor a partir del cual cuenta como bueno. Ponlo ENTRE el desempeno crudo de las opciones malas y el de las buenas.")]
    [Range(0f, 1f)] public float extremeThreshold = 0.5f;

    [Header("Escena de prueba")]
    [Tooltip("Si esta activo, el minijuego arranca solo al cargar la escena (sin boton Continuar).")]
    public bool autoStart = false;

    // ------------------------------------------------------------------ temporizador

    [Header("Tiempo segun estado cognitivo")]
    [Tooltip("Estado optimo: a partir de este porcentaje.")]
    public float umbralOptimo = 70f;
    [Tooltip("Estado deficiente: hasta este porcentaje.")]
    public float umbralDeficiente = 39f;
    public float tiempoOptimo = 120f;
    public float tiempoMedio = 90f;
    public float tiempoDeficiente = 60f;

    [Header("UI del Temporizador")]
    public TMP_Text textoTiempo;
    public Image barraTiempo;
    [Tooltip("Color de la cuenta regresiva.")]
    public Color colorTiempo = Color.white;
    [Tooltip("Brillo del texto (0.9 = 90 %).")]
    [Range(0f, 1f)] public float brilloTiempo = 0.9f;
    [Tooltip("Apaga el brillo cian (TextGlowPulse / Glow del material) del texto del timer para que se vea blanco.")]
    public bool forzarTimerBlanco = true;

    [Header("UI del Progreso (opcional)")]
    public TMP_Text textoProgreso;
    public Image barraProgreso;

    [Header("Paneles de resultado")]
    [Tooltip("Panel de 'correcto'. Se muestra si completas la red a tiempo. Déjalo desactivado al inicio.")]
    public GameObject panelCorrecto;
    [Tooltip("Panel de 'incorrecto'. Se muestra si se acaba el tiempo. Déjalo desactivado al inicio.")]
    public GameObject panelIncorrecto;
    [Tooltip("Segundos de espera antes de mostrar el panel (para que se vea la última neurona encenderse).")]
    [Min(0f)] public float retardoPanel = 0.8f;
    [Tooltip("Segundos del fundido de opacidad al aparecer el panel. 0 = sin transición.")]
    [Min(0f)] public float duracionFundidoPanel = 0.3f;

    [Header("Eventos")]
    public UnityEvent alIniciar;
    public UnityEvent alGanar;
    public UnityEvent alPerderPorTiempo;

    // ------------------------------------------------------------------ estado

    /// <summary>0 = desempeno malo, 1 = desempeno bueno.</summary>
    public float Performance01 { get; private set; } = 1f;

    /// <summary>0 = facil, 1 = dificil (complemento de Performance01).</summary>
    public float Difficulty01 => 1f - Performance01;

    public bool IsStarted { get; private set; }
    public bool IsFinished { get; private set; }
    public RutaDificultad Ruta { get; private set; } = RutaDificultad.Intermedia;
    public float TiempoTotal { get; private set; }
    public float TiempoRestante { get; private set; }

    bool temporizadorActivo;
    Coroutine rutinaBarraProgreso;

    // ------------------------------------------------------------------ ciclo de vida

    void Awake()
    {
        Instance = this;

        if (networkRoot != null) networkRoot.SetActive(false);
        else Debug.LogWarning("[NeuralNetworkManager] 'Network Root' esta vacio en el Inspector.", this);

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

    // ------------------------------------------------------------------ inicio / fin

    /// <summary>Conectar en el On Click del boton Continuar.</summary>
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
        else
        {
            Debug.LogWarning("[NeuralNetworkManager] No hay NeuronProgress en la escena: no se podra detectar la victoria.", this);
        }

        if (networkRoot != null) networkRoot.SetActive(true);

        alIniciar?.Invoke();
        Debug.Log($"[NeuralNetworkManager] Minijuego iniciado. Ruta {Ruta}, tiempo {TiempoTotal:F0} s.", this);
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
        Debug.Log($"[NeuralNetworkManager] GANASTE con {TiempoRestante:F1} s restantes.", this);
        alGanar?.Invoke();
        MostrarPanel(panelCorrecto);
    }

    void Perder()
    {
        if (IsFinished) return;
        IsFinished = true;
        Debug.Log("[NeuralNetworkManager] Se acabo el tiempo.", this);
        alPerderPorTiempo?.Invoke();
        MostrarPanel(panelIncorrecto);
    }

    // ------------------------------------------------------------------ paneles de resultado

    void MostrarPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[NeuralNetworkManager] Falta asignar el panel de resultado en el Inspector.", this);
            return;
        }
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

    /// <summary>Vuelve a arrancar el temporizador y el estado (no apaga las neuronas: eso depende de tu NeuronController).</summary>
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

    // ------------------------------------------------------------------ desempeno

    /// <summary>
    /// Estira el rango real del desempeno a 0-1. Con rawWorst = 0 y rawBest = 1 no cambia nada.
    /// Ej.: si en la practica solo se obtiene entre 0.45 y 0.80, poner esos valores hace que
    /// 0.45 cuente como 0 (peor caso) y 0.80 como 1 (mejor caso).
    /// </summary>
    float Calibrate(float raw)
    {
        if (rawBest - rawWorst < 0.05f) return raw;   // rango invalido: no ajusta
        return Mathf.Clamp01(Mathf.InverseLerp(rawWorst, rawBest, raw));
    }

    /// <summary>Lee el desempeno. Tambien se puede lanzar desde el menu del componente (clic derecho).</summary>
    [ContextMenu("Leer desempeno (prueba)")]
    public void ReadPerformance()
    {
        if (useDebugOverride)
        {
            Performance01 = debugPerformance01;
            Debug.Log($"[NeuralNetworkManager] Desempeno FORZADO = {Performance01:F2}", this);
            return;
        }

        if (NutritionPerformance.TryRead(out float performance, out float difficulty))
        {
            RawPerformance = performance;
            Performance01 = Calibrate(performance);
            if (roundToExtremes) Performance01 = Performance01 >= extremeThreshold ? 1f : 0f;
            Debug.Log($"[NeuralNetworkManager] Desempeno crudo = {performance:F2} -> calibrado = {Performance01:F2} (dificultad {difficulty:F2}) | {NutritionPerformance.DescribeStats()} | {NutritionPerformance.DescribeDecisions()}", this);
        }
        else
        {
            Performance01 = 1f;
            Debug.LogWarning("[NeuralNetworkManager] No hay PlayerStats en la escena. Uso desempeno = 1. " +
                             "Si pruebas la escena sola es normal; usa 'Use Debug Override' para simular valores.", this);
        }
    }

    // ------------------------------------------------------------------ ruta e indicadores

    /// <summary>Misma regla que el Memorama; sin PlayerStats (o con override) se deduce del desempeno.</summary>
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
                Debug.Log("<color=green>[NeuralNetworkManager] Ruta BUENA detectada.</color>", this);
                break;
            case RutaDificultad.Intermedia:
                if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(true);
                Debug.Log("<color=yellow>[NeuralNetworkManager] Ruta INTERMEDIA detectada.</color>", this);
                break;
            case RutaDificultad.Mala:
                if (indicadorRutaMala != null) indicadorRutaMala.SetActive(true);
                Debug.Log("<color=red>[NeuralNetworkManager] Ruta MALA detectada.</color>", this);
                break;
        }
    }

    // ------------------------------------------------------------------ temporizador

    /// <summary>Estado cognitivo 0-100: promedio de energia, memoria e interferencia invertida.</summary>
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
        if (estado >= umbralOptimo) t = tiempoOptimo;               // optimo
        else if (estado <= umbralDeficiente) t = tiempoDeficiente;  // deficiente
        else t = tiempoMedio;                                       // medio

        Debug.Log($"[NeuralNetworkManager] Estado cognitivo {estado:F0}% -> {t:F0} s", this);
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

    // Algo mas (TextGlowPulse, glow o color de cara del material) puede pintar el texto de cian
    void QuitarEfectosDeColorAlTimer()
    {
        var pulse = textoTiempo.GetComponent<TextGlowPulse>();
        if (pulse != null) pulse.enabled = false;

        Material m = textoTiempo.fontMaterial;   // copia propia: no toca el Font Asset
        m.DisableKeyword("GLOW_ON");
        if (m.HasProperty("_FaceColor")) m.SetColor("_FaceColor", Color.white);
    }

    // ------------------------------------------------------------------ UI de progreso

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