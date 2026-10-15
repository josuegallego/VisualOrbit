using System.Collections.Generic;
using UnityEngine;

public class HandsEmissionPulse : MonoBehaviour
{
    public string materialName = "Manos";
    [ColorUsage(false, true)] public Color glowColor = Color.cyan;
    public float minIntensity = 0.2f;
    public float maxIntensity = 3f;
    public float speed = 2f;
    public float startDelay = 2f;

    [Header("Al detener el pulso")]
    public float stopIntensity = 0f;   // brillo final (0 = apagado)
    public float fadeOutTime = 0.5f;   // segundos para apagarse

    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    readonly List<Material> mats = new List<Material>();
    readonly HashSet<Renderer> seen = new HashSet<Renderer>();
    float startTime, nextScan;

    bool stopping;
    float stopStartTime;
    float intensityAtStop;
    float currentIntensity;

    void OnEnable() { startTime = Time.time; }

    // Llama a esto desde el botón
    public void StopPulse()
    {
        if (stopping) return;
        stopping = true;
        stopStartTime = Time.time;
        intensityAtStop = currentIntensity;
    }

    void Scan()
    {
        var all = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in all)
        {
            if (seen.Contains(r)) continue;
            foreach (var m in r.materials)
            {
                if (m != null && m.name.StartsWith(materialName))
                {
                    mats.Add(m);
                    seen.Add(r);
                }
            }
        }
    }

    void Update()
    {
        if (!stopping && Time.time >= nextScan) { Scan(); nextScan = Time.time + 1f; }

        if (stopping)
        {
            float k = fadeOutTime <= 0f ? 1f : Mathf.Clamp01((Time.time - stopStartTime) / fadeOutTime);
            currentIntensity = Mathf.Lerp(intensityAtStop, stopIntensity, k);
        }
        else
        {
            float elapsed = Time.time - startTime;
            currentIntensity = minIntensity;
            if (elapsed >= startDelay)
            {
                float t = (1f - Mathf.Cos((elapsed - startDelay) * speed)) * 0.5f;
                currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            }
        }

        Color c = glowColor * currentIntensity;
        c.a = 1f;
        foreach (var m in mats)
        {
            if (m == null) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor(EmissionID, c);
        }
    }
}