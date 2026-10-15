using System;
using UnityEngine;

/// <summary>
/// Guarda los 3 indicadores del jugador (0-100) y avisa cuando cambian.
/// Ponlo en el objeto GameManager (o en uno propio) de la escena inicial.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Valores iniciales")]
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float interference = 0f;   // interferencia cognitiva: 0 = bien, 100 = mal
    [Range(0, 100)] public float memory = 100f;

    [Tooltip("Si es true, sobrevive al cambiar de escena")]
    public bool persistBetweenScenes = true;

    public event Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (persistBetweenScenes)
        {
            // DontDestroyOnLoad exige que el objeto este en la raiz de la jerarquia
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ApplyEffects(float dEnergy, float dInterference, float dMemory)
    {
        energy = Mathf.Clamp(energy + dEnergy, 0f, 100f);
        interference = Mathf.Clamp(interference + dInterference, 0f, 100f);
        memory = Mathf.Clamp(memory + dMemory, 0f, 100f);
        OnStatsChanged?.Invoke();
    }

    public void ResetStats()
    {
        energy = 100f; interference = 0f; memory = 100f;
        StatsBarUI.ForgetLastValues();
        OnStatsChanged?.Invoke();
    }

    // ---------- Ayudas para los minijuegos (ej. memorama) ----------

    /// <summary>0 = vision nitida, 1 = muy borrosa. Usalo para el efecto de blur.</summary>
    public float BlurLevel01()
    {
        float lowEnergy = 1f - energy / 100f;
        float inter = interference / 100f;
        return Mathf.Clamp01(0.5f * inter + 0.3f * lowEnergy + 0.2f * (1f - memory / 100f));
    }

    /// <summary>Tiempo para memorizar las cartas: menos memoria = menos segundos.</summary>
    public float MemorizeSeconds(float maxSeconds = 10f, float minSeconds = 3f)
    {
        return Mathf.Lerp(minSeconds, maxSeconds, memory / 100f);
    }

    /// <summary>Dificultad general 0 (facil) a 1 (dificil).</summary>
    public float Difficulty01() => BlurLevel01();
}
