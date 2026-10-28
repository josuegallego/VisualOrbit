using UnityEngine;

/// <summary>
/// Indicador (triangulo azul) que senala QUE anillo mover ahora.
///
///  - Solo aparece cuando ese anillo es el siguiente de su ruta (el anterior ya esta resuelto).
///  - Mientras esta visible oscila (sube y baja), late de tamano y brilla (luminiscencia pulsante).
///  - Al resolver el anillo se desvanece y aparece el indicador del siguiente.
///
/// USO: ponlo en CADA triangulo azul y arrastra su anillo a 'Ring'.
///      Si dejas 'Ring' vacio, usa el anillo padre o, si no hay, el anillo mas cercano (mira el log).
///      Si el triangulo tenia 'LevitacionIndicador', se desactiva solo para que no pelee con este script.
/// </summary>
public class RingGuideIndicator : MonoBehaviour
{
    [Header("Anillo que senala")]
    public TubeRingManipulator ring;

    [Header("Aparicion")]
    [Tooltip("Segundos en aparecer / desaparecer.")]
    public float fadeSeconds = 0.5f;

    [Header("Oscilacion")]
    [Tooltip("Cuanto sube y baja (metros).")]
    public float bobAmplitude = 0.05f;
    public float bobSpeed = 2.5f;
    [Tooltip("Cuanto late el tamano (0.15 = 15 %).")]
    [Range(0f, 0.5f)] public float pulseScale = 0.15f;

    [Header("Luminiscencia")]
    [ColorUsage(false, true)] public Color glowColor = new Color(0.2f, 0.7f, 1f);
    [Tooltip("Intensidad minima y maxima del brillo.")]
    public float glowMin = 0.3f;
    public float glowMax = 3f;
    public float glowSpeed = 3f;
    [Tooltip("Opcional. Si queda vacio busca una Light en los hijos.")]
    public Light glowLight;
    public float lightMin = 0.2f;
    public float lightMax = 2f;

    [Header("Depuracion")]
    public bool logEvents = true;

    Renderer[] renderers;
    Material[][] materials;
    Color[][] baseColors;
    Vector3 startLocalPos;
    Vector3 baseScale;
    float visible;          // 0 = oculto, 1 = totalmente visible
    float phase;
    bool renderersOn = true;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        // Evita que 'LevitacionIndicador' mueva el mismo objeto a la vez
        var lev = GetComponent("LevitacionIndicador") as Behaviour;
        if (lev != null) lev.enabled = false;

        startLocalPos = transform.localPosition;
        baseScale = transform.localScale;
        phase = Random.Range(0f, 6.28f);

        if (glowLight == null) glowLight = GetComponentInChildren<Light>(true);

        renderers = GetComponentsInChildren<Renderer>(true);
        materials = new Material[renderers.Length][];
        baseColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;   // copias propias: no tocan los materiales del proyecto
            materials[i] = mats;
            baseColors[i] = new Color[mats.Length];

            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;

                if (mats[m].HasProperty(EmissionId))
                {
                    mats[m].EnableKeyword("_EMISSION");
                    mats[m].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                int cid = ColorProperty(mats[m]);
                if (cid != 0) baseColors[i][m] = mats[m].GetColor(cid);
            }
        }

        SetVisible(false);   // empieza oculto
    }

    void Start()
    {
        if (ring == null) ring = FindRing();
    }

    void OnDestroy()
    {
        // Libera las copias de materiales
        if (materials == null) return;
        foreach (var mats in materials)
            foreach (var mat in mats)
                if (mat != null) Destroy(mat);
    }

    void Update()
    {
        bool want = ShouldShow();
        visible = Mathf.MoveTowards(visible, want ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));

        bool on = visible > 0.001f;
        SetVisible(on);
        if (!on) return;

        float t = Time.time + phase;
        float pulse01 = 0.5f + 0.5f * Mathf.Sin(t * glowSpeed);

        // Oscilacion: sube y baja
        transform.localPosition = startLocalPos + new Vector3(0f, Mathf.Sin(t * bobSpeed) * bobAmplitude, 0f);

        // Aparicion suave + latido de tamano
        float appear = Mathf.SmoothStep(0f, 1f, visible);
        transform.localScale = baseScale * (appear * (1f + pulseScale * pulse01));

        // Luminiscencia pulsante
        float intensity = Mathf.Lerp(glowMin, glowMax, pulse01) * visible;
        ApplyGlow(intensity, pulse01);

        if (glowLight != null)
        {
            glowLight.color = glowColor;
            glowLight.intensity = Mathf.Lerp(lightMin, lightMax, pulse01) * visible;
        }
    }

    // ------------------------------------------------------------ Logica

    // Aparece solo si ese anillo es el que toca mover ahora
    bool ShouldShow()
    {
        if (ring == null || ring.IsSolved) return false;
        return ring.sequence == null || ring.sequence.CanSolve(ring);
    }

    TubeRingManipulator FindRing()
    {
        var parentRing = GetComponentInParent<TubeRingManipulator>(true);
        if (parentRing != null)
        {
            if (logEvents) Debug.Log($"[RingGuide] '{name}' usa el anillo padre '{parentRing.name}'.", this);
            return parentRing;
        }

        var all = FindObjectsByType<TubeRingManipulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TubeRingManipulator best = null;
        float bestDist = float.MaxValue;
        foreach (var r in all)
        {
            float d = (r.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = r; }
        }

        if (best != null)
            Debug.LogWarning($"[RingGuide] '{name}' no tenia 'Ring': uso el mas cercano '{best.name}'. " +
                             "Si no es el correcto, arrastra el anillo a mano.", this);
        else
            Debug.LogWarning($"[RingGuide] '{name}': no hay ningun TubeRingManipulator en la escena.", this);

        return best;
    }

    // ------------------------------------------------------------ Visual

    void SetVisible(bool on)
    {
        if (on == renderersOn) return;
        renderersOn = on;

        if (renderers != null)
            foreach (var r in renderers)
                if (r != null) r.enabled = on;

        if (glowLight != null) glowLight.enabled = on;
    }

    static int ColorProperty(Material mat)
    {
        if (mat.HasProperty(BaseColorId)) return BaseColorId;
        if (mat.HasProperty(ColorId)) return ColorId;
        return 0;
    }

    void ApplyGlow(float intensity, float pulse01)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            for (int m = 0; m < materials[i].Length; m++)
            {
                Material mat = materials[i][m];
                if (mat == null) continue;

                if (mat.HasProperty(EmissionId))
                {
                    mat.SetColor(EmissionId, glowColor * intensity);
                }
                else
                {
                    // Material sin emision: se aclara el color base hacia el brillo
                    int cid = ColorProperty(mat);
                    if (cid != 0)
                        mat.SetColor(cid, Color.Lerp(baseColors[i][m], glowColor, pulse01 * 0.6f));
                }
            }
        }
    }
}
