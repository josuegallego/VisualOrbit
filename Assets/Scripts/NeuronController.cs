using System.Collections;
using UnityEngine;

/// <summary>
/// Neurona del minijuego (Etapa 6b). Va en el objeto de la neurona.
/// - Apagada: material gris y luz blanca leve.
/// - Encendida: transicion suave a verde, con una luz mas intensa y verde.
/// Se enciende cuando el GlucoseFlowController de origen empieza a fluir
/// (con un retraso opcional igual al tiempo que tardan las particulas en llegar).
/// </summary>
public class NeuronController : MonoBehaviour
{
    [Header("Origen")]
    [Tooltip("Flujo de glucosa que alimenta esta neurona (GlucoseFlow).")]
    public GlucoseFlowController sourceFlow;
    [Tooltip("Espera a que las particulas lleguen antes de encender.")]
    public bool delayByTravelTime = true;
    [Range(0f, 1.5f)] public float delayFactor = 1f;
    public float extraDelay = 0f;
    [Tooltip("Espera maxima, para que un flujo lento no parezca roto.")]
    public float maxDelay = 4f;

    [Header("Material")]
    [Tooltip("Si queda vacio busca un Renderer en este objeto o en sus hijos.")]
    public Renderer targetRenderer;
    public Color offColor = new Color(0.35f, 0.35f, 0.38f);
    public Color onColor = new Color(0.2f, 1f, 0.4f);
    [ColorUsage(false, true)] public Color offEmission = Color.black;
    [ColorUsage(false, true)] public Color onEmission = new Color(0.3f, 1.6f, 0.6f);
    [Tooltip("Desactivado: el script no toca la emision y se ve la textura del material.")]
    public bool useEmission = false;

    [Header("Luz")]
    [Tooltip("Si queda vacio busca una Light en los hijos.")]
    public Light neuronLight;
    public Color lightOffColor = Color.white;
    public Color lightOnColor = new Color(0.3f, 1f, 0.5f);
    public float lightOffIntensity = 0.4f;
    public float lightOnIntensity = 3f;

    [Header("Transicion")]
    public float transitionSeconds = 1.2f;

    [Header("Depuracion")]
    public bool logEvents = true;

    public bool IsLit => target > 0.5f;

    Material mat;
    int colorId;
    bool hasEmission;
    float t;        // 0 = apagada, 1 = encendida
    float target;
    Coroutine delayRoutine;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (neuronLight == null) neuronLight = GetComponentInChildren<Light>();

        if (targetRenderer != null)
        {
            mat = targetRenderer.material;   // copia propia: no modifica el material del proyecto
            colorId = mat.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            hasEmission = useEmission && mat.HasProperty(EmissionId);
            if (hasEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
        else
        {
            Debug.LogWarning("[Neuron] No hay Renderer: no se vera el cambio de material.", this);
        }

        Apply(0f);
    }

    void OnEnable()
    {
        if (sourceFlow != null)
        {
            sourceFlow.FlowChanged += OnFlowChanged;
            if (sourceFlow.IsFlowing) OnFlowChanged(true);
        }
        else
        {
            Debug.LogWarning("[Neuron] 'Source Flow' esta vacio: la neurona solo se enciende con SetLit().", this);
        }
    }

    void OnDisable()
    {
        if (sourceFlow != null) sourceFlow.FlowChanged -= OnFlowChanged;
        if (delayRoutine != null) { StopCoroutine(delayRoutine); delayRoutine = null; }
    }

    void Update()
    {
        if (Mathf.Approximately(t, target)) return;

        t = Mathf.MoveTowards(t, target, Time.deltaTime / Mathf.Max(0.01f, transitionSeconds));
        Apply(Mathf.SmoothStep(0f, 1f, t));
    }

    void OnFlowChanged(bool on)
    {
        if (delayRoutine != null) { StopCoroutine(delayRoutine); delayRoutine = null; }

        if (!on)
        {
            target = 0f;
            return;
        }

        float delay = extraDelay;
        if (delayByTravelTime && sourceFlow != null) delay += sourceFlow.TravelTime * delayFactor;
        delay = Mathf.Clamp(delay, 0f, maxDelay);

        if (logEvents) Debug.Log($"[Neuron] el flujo llego a la neurona en {delay:F1} s", this);
        delayRoutine = StartCoroutine(LightAfter(delay));
    }

    IEnumerator LightAfter(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        target = 1f;
        delayRoutine = null;
        if (logEvents) Debug.Log("[Neuron] ENCENDIDA", this);
    }

    /// <summary>Enciende o apaga la neurona a mano (pruebas, o la logica de progresion mas adelante).</summary>
    public void SetLit(bool on)
    {
        if (delayRoutine != null) { StopCoroutine(delayRoutine); delayRoutine = null; }
        target = on ? 1f : 0f;
    }

    void Apply(float k)
    {
        if (mat != null)
        {
            mat.SetColor(colorId, Color.Lerp(offColor, onColor, k));
            if (hasEmission) mat.SetColor(EmissionId, Color.Lerp(offEmission, onEmission, k));
        }

        if (neuronLight != null)
        {
            neuronLight.color = Color.Lerp(lightOffColor, lightOnColor, k);
            neuronLight.intensity = Mathf.Lerp(lightOffIntensity, lightOnIntensity, k);
        }
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);   // libera la copia del material
    }

    [ContextMenu("Probar: encender")]
    void MenuOn() { SetLit(true); }

    [ContextMenu("Probar: apagar")]
    void MenuOff() { SetLit(false); }
}