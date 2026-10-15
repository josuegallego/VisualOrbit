using UnityEngine;

/// <summary>
/// Gestor del minijuego "Conexiones Neuronales" (version inicial, Etapa 4).
/// - Oculta la red al cargar la escena.
/// - Al pulsar Continuar (Button > On Click > StartGame) lee el desempeno nutricional
///   y muestra la red.
/// Ponlo en un objeto vacio en la raiz de la escena (NO dentro de NeuralNetwork).
/// </summary>
public class NeuralNetworkManager : MonoBehaviour
{
    public static NeuralNetworkManager Instance { get; private set; }

    [Header("Referencias")]
    [Tooltip("Objeto NeuralNetwork con todos los conductos. Queda oculto hasta pulsar Continuar.")]
    public GameObject networkRoot;

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

    /// <summary>0 = desempeno malo, 1 = desempeno bueno.</summary>
    public float Performance01 { get; private set; } = 1f;

    /// <summary>0 = facil, 1 = dificil (complemento de Performance01).</summary>
    public float Difficulty01 => 1f - Performance01;

    public bool IsStarted { get; private set; }

    void Awake()
    {
        Instance = this;

        if (networkRoot != null) networkRoot.SetActive(false);
        else Debug.LogWarning("[NeuralNetworkManager] 'Network Root' esta vacio en el Inspector.", this);
    }

    void Start()
    {
        if (autoStart) StartGame();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

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

    /// <summary>Conectar en el On Click del boton Continuar.</summary>
    public void StartGame()
    {
        if (IsStarted) return;
        IsStarted = true;

        ReadPerformance();

        if (networkRoot != null) networkRoot.SetActive(true);
        Debug.Log("[NeuralNetworkManager] Minijuego iniciado.", this);
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
}