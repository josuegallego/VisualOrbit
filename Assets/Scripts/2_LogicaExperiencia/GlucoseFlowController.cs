using System;
using UnityEngine;

/// <summary>
/// Controla el flujo de particulas de glucosa de UN conducto recto (Etapa 5).
/// Va en el objeto GlucoseFlow (el que tiene el ParticleSystem), hijo de Socket_A.
///
/// - Las particulas salen de startPoint y viajan por el eje Z local hasta endPoint.
/// - La fluidez depende del desempeno nutricional (0 = mala, 1 = buena):
///     buena -> mas rapido y casi continuo; mala -> mas lento y "entrecortado".
/// - Importante: el flujo SIEMPRE se ve; la dificultad real no depende de este script.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class GlucoseFlowController : MonoBehaviour
{
    [Header("Extremos del conducto")]
    [Tooltip("Socket_A: donde nacen las particulas")]
    public Transform startPoint;
    [Tooltip("Socket_B: donde deben desaparecer")]
    public Transform endPoint;

    [Header("Fluidez (0 = mala, 1 = buena)")]
    [Tooltip("Si esta activo y existe NeuralNetworkManager, usa su Performance01.")]
    public bool useManagerPerformance = true;
    [Range(0f, 1f)] public float testPerformance01 = 1f;

    [Header("Velocidad de las particulas (m/s)")]
    public float speedWorst = 0.5f;
    public float speedBest = 0.9f;

    [Header("Separacion entre particulas (m). Menos = flujo mas continuo")]
    public float spacingWorst = 0.06f;
    public float spacingBest = 0.025f;

    [Header("Minimo visible")]
    [Tooltip("Particulas por segundo como minimo, para que el flujo malo nunca parezca roto.")]
    public float minRate = 3f;

    [Header("Prueba")]
    [Tooltip("Si esta activo, el flujo arranca al activarse el objeto.")]
    public bool flowOnStart = true;
    [Tooltip("Escribe en la consola los valores calculados cada vez que se aplican.")]
    public bool logValues = true;

    ParticleSystem ps;

    public bool IsFlowing { get; private set; }

    /// <summary>Segundos que tarda una particula en ir de un extremo al otro (depende de la fluidez).</summary>
    public float TravelTime { get; private set; }

    /// <summary>Se dispara cuando el flujo se enciende (true) o se apaga (false).</summary>
    public event Action<bool> FlowChanged;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        ApplyFluidity(CurrentPerformance());
        SetFlow(flowOnStart);
    }

    /// <summary>Enciende o apaga el flujo. Al apagar, las particulas que ya salieron terminan su recorrido.</summary>
    public void SetFlow(bool on)
    {
        bool changed = IsFlowing != on;
        IsFlowing = on;

        if (on)
        {
            if (!ps.isPlaying) ps.Play();
        }
        else
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (changed) FlowChanged?.Invoke(on);
    }

    /// <summary>Ajusta velocidad, vida y densidad segun el desempeno (0 a 1).</summary>
    public void ApplyFluidity(float performance01)
    {
        float p = Mathf.Clamp01(performance01);

        float length = 1f;
        if (startPoint != null && endPoint != null)
            length = Vector3.Distance(startPoint.position, endPoint.position);
        else
            Debug.LogWarning("[GlucoseFlow] Faltan 'Start Point' o 'End Point'. Uso largo = 1 m.", this);

        float speed = Mathf.Max(0.05f, Mathf.Lerp(speedWorst, speedBest, p));
        float spacing = Mathf.Max(0.005f, Mathf.Lerp(spacingWorst, spacingBest, p));

        float lifetime = length / speed;                      // llegan justo al otro extremo
        float rate = Mathf.Max(minRate, speed / spacing);     // separacion = velocidad / tasa

        TravelTime = lifetime;

        var main = ps.main;
        main.startSpeed = speed;
        main.startLifetime = lifetime;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        if (logValues)
            Debug.Log($"[GlucoseFlow] desempeno={p:F2} | velocidad={speed:F2} m/s | separacion={spacing:F3} m | " +
                      $"tasa={rate:F1}/s | vida={lifetime:F2} s | largo={length:F2} m", this);
    }

    float CurrentPerformance()
    {
        if (useManagerPerformance && NeuralNetworkManager.Instance != null)
            return NeuralNetworkManager.Instance.Performance01;
        return testPerformance01;
    }

    // Permite ajustar valores en el Inspector mientras el juego esta en Play.
    void OnValidate()
    {
        if (Application.isPlaying && ps != null)
            ApplyFluidity(CurrentPerformance());
    }

    [ContextMenu("Iniciar flujo")]
    void MenuStart() { SetFlow(true); }

    [ContextMenu("Detener flujo")]
    void MenuStop() { SetFlow(false); }

    void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}