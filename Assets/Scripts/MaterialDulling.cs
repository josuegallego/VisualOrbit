using UnityEngine;

/// <summary>
/// Hace que tubos y anillos se vean opacos / sin vida cuando el desempeno es malo
/// (decisiones equivocadas). Desatura, oscurece y apaga la emision.
/// Respeta renderers con varios materiales: guarda y modifica cada slot por separado.
/// </summary>
public class MaterialDulling : MonoBehaviour
{
    [Header("Objetivos")]
    [Tooltip("Renderers de tubos y anillos. Si se deja vacio, busca en los hijos de este objeto.")]
    public Renderer[] targets;

    [Header("Efecto en el peor caso")]
    [Range(0f, 1f)] public float desaturation = 0.85f;
    [Range(0f, 1f)] public float darken = 0.45f;
    [Range(0f, 1f)] public float emissionLoss = 0.9f;
    public Color deadTint = new Color(0.35f, 0.37f, 0.42f);

    [Header("Origen")]
    public bool useManagerPerformance = true;
    [Range(0f, 1f)] public float testPerformance01 = 1f;
    public float smoothSeconds = 1.2f;

    class SlotData
    {
        public int colorId = -1, emissionId = -1;
        public Color baseColor, emission;
    }

    class Entry
    {
        public Renderer r;
        public SlotData[] slots;
    }

    Entry[] entries;
    MaterialPropertyBlock block;
    float dullness;
    float lastApplied = -1f;

    void Awake()
    {
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Renderer>();

        block = new MaterialPropertyBlock();
        entries = new Entry[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            var e = new Entry { r = targets[i] };
            if (e.r != null)
            {
                var mats = e.r.sharedMaterials;
                e.slots = new SlotData[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    var s = new SlotData();
                    var mat = mats[m];
                    if (mat != null)
                    {
                        if (mat.HasProperty("_BaseColor")) s.colorId = Shader.PropertyToID("_BaseColor");
                        else if (mat.HasProperty("_Color")) s.colorId = Shader.PropertyToID("_Color");
                        if (s.colorId != -1) s.baseColor = mat.GetColor(s.colorId);

                        if (mat.HasProperty("_EmissionColor"))
                        {
                            s.emissionId = Shader.PropertyToID("_EmissionColor");
                            s.emission = mat.GetColor(s.emissionId);
                        }
                    }
                    e.slots[m] = s;
                }
            }
            entries[i] = e;
        }
    }

    float TargetDullness()
    {
        float perf = (useManagerPerformance && NeuralNetworkManager.Instance != null)
            ? NeuralNetworkManager.Instance.Performance01
            : testPerformance01;

        return Mathf.Clamp01(1f - perf);
    }

    void Update()
    {
        dullness = Mathf.MoveTowards(dullness, TargetDullness(),
                                     Time.deltaTime / Mathf.Max(0.01f, smoothSeconds));

        // Sin cambios desde el ultimo frame: no tocar nada.
        if (Mathf.Approximately(dullness, lastApplied)) return;
        lastApplied = dullness;

        foreach (var e in entries)
        {
            if (e.r == null) continue;

            for (int m = 0; m < e.slots.Length; m++)
            {
                var s = e.slots[m];

                if (dullness <= 0.0001f)
                {
                    // Estado original: quitar el override de este slot.
                    e.r.SetPropertyBlock(null, m);
                    continue;
                }

                e.r.GetPropertyBlock(block, m);

                if (s.colorId != -1)
                {
                    float gray = s.baseColor.grayscale;
                    Color desat = Color.Lerp(s.baseColor, new Color(gray, gray, gray, s.baseColor.a), desaturation);
                    Color dead = Color.Lerp(desat, deadTint * gray * 1.5f, 0.4f);
                    dead *= 1f - darken * dullness;
                    dead.a = s.baseColor.a;
                    block.SetColor(s.colorId, Color.Lerp(s.baseColor, dead, dullness));
                }

                if (s.emissionId != -1)
                    block.SetColor(s.emissionId, s.emission * (1f - emissionLoss * dullness));

                e.r.SetPropertyBlock(block, m);
            }
        }
    }
}