using UnityEngine;

/// <summary>
/// Oscurece el skybox segun las decisiones del jugador, PARTIENDO de la exposicion que ya tiene el material.
///
///   - Con buen estado el skybox queda EXACTAMENTE como lo dejaste en el material (no se toca).
///   - Con mal estado baja su _Exposure hasta (1 - reduccionMaxima) de ese valor.
///   - Al salir de la escena / dejar de jugar se restaura la exposicion original del material.
///
/// Va en un GameObject vacio de la escena (por ejemplo, "ExposureManager").
///
/// USO:
///   1. Arrastra el material del skybox (desde Assets) al campo 'materialSkybox'.
///   2. Ajusta 'reduccionMaxima' (cuanto puede oscurecerse como maximo).
///   3. Al cambiar PlayerStats, el material se modifica automaticamente.
/// </summary>
public class SkyboxExposureController : MonoBehaviour
{
    [Header("Material a modificar (arrastrar desde Assets)")]
    [Tooltip("Material del skybox que se va a modificar. Debe estar en Assets, no en la escena.")]
    public Material materialSkybox;

    [Header("Rango de exposicion")]
    [Tooltip("Si esta activo, parte de la exposicion que YA tiene el material (recomendado). " +
             "Si esta apagado, usa los valores manuales de abajo.")]
    public bool usarExposureDelMaterial = true;

    [Tooltip("Cuanto puede oscurecerse el skybox con el peor estado. 0.4 = baja hasta el 60 % de su exposicion. " +
             "Mantenlo moderado para que los textos sigan viendose.")]
    [Range(0f, 0.9f)] public float reduccionMaxima = 0.4f;

    [Header("Valores manuales (solo si 'Usar Exposure Del Material' esta apagado)")]
    [Tooltip("Exposure cuando el jugador esta bien")]
    public float exposureMejor = 0.55f;

    [Tooltip("Exposure cuando el jugador esta mal")]
    public float exposurePeor = 0.20f;

    [Header("Pesos de cada indicador")]
    [Range(0f, 1f)] public float pesoInterferencia = 0.5f;
    [Range(0f, 1f)] public float pesoMemoria = 0.3f;
    [Range(0f, 1f)] public float pesoEnergia = 0.2f;

    [Header("Gradualidad (HU 2.2.3)")]
    [Tooltip("Segundos que tarda el exposure en llegar al valor objetivo")]
    public float duracionTransicion = 2f;

    [Header("Debug")]
    public bool mostrarLogs = true;

    private float exposicionOriginal;   // la que tenia el material al empezar
    private float mejor, peor;          // rango realmente usado
    private float exposicionActual;
    private float exposicionObjetivo;
    private float velocidad;
    private bool listo;
    private bool restaurado;

    void Start()
    {
        if (materialSkybox == null)
        {
            Debug.LogError("[Skybox] No asignaste el material del skybox en el Inspector.", this);
            enabled = false;
            return;
        }

        if (!materialSkybox.HasProperty("_Exposure"))
        {
            Debug.LogError($"[Skybox] El material '{materialSkybox.name}' no tiene la propiedad '_Exposure'. " +
                           "Revisa el shader o usa otro material.", this);
            enabled = false;
            return;
        }

        // Exposicion con la que viene el material: de aqui parte todo
        exposicionOriginal = materialSkybox.GetFloat("_Exposure");

        if (usarExposureDelMaterial)
        {
            mejor = exposicionOriginal;
            peor = exposicionOriginal * (1f - reduccionMaxima);
        }
        else
        {
            mejor = exposureMejor;
            peor = exposurePeor;
        }

        exposicionActual = mejor;
        exposicionObjetivo = mejor;
        materialSkybox.SetFloat("_Exposure", exposicionActual);
        listo = true;

        if (mostrarLogs)
            Debug.Log($"[Skybox] Material '{materialSkybox.name}' | exposure original: {exposicionOriginal:F2} | " +
                      $"rango usado: {mejor:F2} (bien) -> {peor:F2} (mal)");

        SuscribirAPlayerStats();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= Recalcular;
        Restaurar();
    }

    void OnApplicationQuit()
    {
        Restaurar();
    }

    /// <summary>Devuelve el material a la exposicion que tenia antes de jugar.</summary>
    void Restaurar()
    {
        if (restaurado || !listo || materialSkybox == null) return;
        materialSkybox.SetFloat("_Exposure", exposicionOriginal);
        restaurado = true;
    }

    void Update()
    {
        if (!listo) return;

        if (Mathf.Abs(exposicionActual - exposicionObjetivo) > 0.001f)
        {
            exposicionActual = Mathf.MoveTowards(
                exposicionActual, exposicionObjetivo, velocidad * Time.deltaTime);

            materialSkybox.SetFloat("_Exposure", exposicionActual);
        }
    }

    private void SuscribirAPlayerStats()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += Recalcular;
            Recalcular();
        }
        else
        {
            // El PlayerStats puede tardar en aparecer (DontDestroyOnLoad). Esperamos.
            StartCoroutine(EsperarPlayerStats());
        }
    }

    private System.Collections.IEnumerator EsperarPlayerStats()
    {
        while (PlayerStats.Instance == null)
            yield return null;

        PlayerStats.Instance.OnStatsChanged += Recalcular;
        if (mostrarLogs) Debug.Log("[Skybox] PlayerStats encontrado. Suscrito al evento.");
        Recalcular();
    }

    private void Recalcular()
    {
        var s = PlayerStats.Instance;
        if (s == null || !listo) return;

        float malEstado =
            pesoInterferencia * (s.interference / 100f) +
            pesoMemoria * (1f - s.memory / 100f) +
            pesoEnergia * (1f - s.energy / 100f);

        malEstado = Mathf.Clamp01(malEstado);
        exposicionObjetivo = Mathf.Lerp(mejor, peor, malEstado);

        float distancia = Mathf.Abs(exposicionActual - exposicionObjetivo);
        velocidad = duracionTransicion > 0f ? distancia / duracionTransicion : distancia;

        if (mostrarLogs)
            Debug.Log($"[Skybox] Recalcular -> malEstado: {malEstado:F2} | " +
                      $"objetivo: {exposicionObjetivo:F2} | actual: {exposicionActual:F2}");
    }

    /// <summary>Metodo publico para forzar un valor desde otro script o boton.</summary>
    public void ForzarExposure(float valor)
    {
        exposicionObjetivo = valor;
        float distancia = Mathf.Abs(exposicionActual - exposicionObjetivo);
        velocidad = duracionTransicion > 0f ? distancia / duracionTransicion : distancia;
    }
}
