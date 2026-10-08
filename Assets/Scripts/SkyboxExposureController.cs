using UnityEngine;

/// <summary>
/// Ajusta el _Exposure de un material de skybox asignado manualmente desde Assets.
/// Va en un GameObject vacío de la escena (por ejemplo, "ExposureManager").
///
/// USO:
///   1. Arrastra el material del skybox (desde Assets, no desde la escena) al campo 'materialSkybox'.
///   2. Ajusta exposureMejor y exposurePeor.
///   3. Al cambiar PlayerStats, el material se modifica automáticamente.
/// </summary>
public class SkyboxExposureController : MonoBehaviour
{
    [Header("Material a modificar (arrastrar desde Assets)")]
    [Tooltip("Material del skybox que se va a modificar. Debe estar en Assets, no en la escena.")]
    public Material materialSkybox;

    [Header("Rango de exposición")]
    [Tooltip("Exposure cuando el jugador está bien")]
    public float exposureMejor = 0.55f;

    [Tooltip("Exposure cuando el jugador está mal")]
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

    private float exposicionActual;
    private float exposicionObjetivo;
    private float velocidad;
    private bool listo;

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

        exposicionActual = exposureMejor;
        exposicionObjetivo = exposureMejor;
        materialSkybox.SetFloat("_Exposure", exposicionActual);
        listo = true;

        if (mostrarLogs)
            Debug.Log($"[Skybox] Material asignado: '{materialSkybox.name}' | Exposure inicial: {exposicionActual}");

        SuscribirAPlayerStats();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= Recalcular;
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
        exposicionObjetivo = Mathf.Lerp(exposureMejor, exposurePeor, malEstado);

        float distancia = Mathf.Abs(exposicionActual - exposicionObjetivo);
        velocidad = duracionTransicion > 0f ? distancia / duracionTransicion : distancia;

        if (mostrarLogs)
            Debug.Log($"[Skybox] Recalcular → malEstado: {malEstado:F2} | " +
                      $"objetivo: {exposicionObjetivo:F2} | actual: {exposicionActual:F2}");
    }

    /// <summary>Método público para forzar un valor desde otro script o botón.</summary>
    public void ForzarExposure(float valor)
    {
        exposicionObjetivo = valor;
        float distancia = Mathf.Abs(exposicionActual - exposicionObjetivo);
        velocidad = duracionTransicion > 0f ? distancia / duracionTransicion : distancia;
    }
}