using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextGlowPulse : MonoBehaviour
{
    [Header("Textos que van a pulsar")]
    public TMP_Text[] texts;

    [Header("Glow")]
    [ColorUsage(false, true)] public Color glowColor = Color.cyan;
    [Range(0f, 1f)] public float minPower = 0.1f;
    [Range(0f, 1f)] public float maxPower = 0.9f;
    [Range(0f, 1f)] public float glowOuter = 0.6f;

    [Header("Brillo de la letra")]
    public bool pulseFaceBrightness = true;
    [Range(0f, 1f)] public float minBrightness = 0.6f;

    [Header("Tiempo")]
    public float speed = 2f;
    public float startDelay = 2f;

    [Header("Al detener el pulso")]
    public float fadeOutTime = 0.5f;

    static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    static readonly int GlowPowerID = Shader.PropertyToID("_GlowPower");
    static readonly int GlowOuterID = Shader.PropertyToID("_GlowOuter");
    static readonly int GlowInnerID = Shader.PropertyToID("_GlowInner");
    static readonly int GlowOffsetID = Shader.PropertyToID("_GlowOffset");

    readonly List<Material> mats = new List<Material>();
    readonly List<Color> baseColors = new List<Color>();

    float startTime;
    bool stopping;
    float stopStartTime, masterAtStop, tAtStop;
    float master = 1f, currentT;

    void Start()
    {
        startTime = Time.time;

        foreach (var tmp in texts)
        {
            if (tmp == null) continue;
            Material m = tmp.fontMaterial; // copia propia, no toca el Font Asset
            m.EnableKeyword("GLOW_ON");
            m.SetColor(GlowColorID, glowColor);
            m.SetFloat(GlowOffsetID, 0f);
            m.SetFloat(GlowInnerID, 0.1f);
            m.SetFloat(GlowOuterID, glowOuter);
            m.SetFloat(GlowPowerID, 0f);
            mats.Add(m);
            baseColors.Add(tmp.color);
        }
    }

    // Llama a esto desde el botón
    public void StopPulse()
    {
        if (stopping) return;
        stopping = true;
        stopStartTime = Time.time;
        masterAtStop = master;
        tAtStop = currentT;
    }

    void Update()
    {
        if (stopping)
        {
            float k = fadeOutTime <= 0f ? 1f : Mathf.Clamp01((Time.time - stopStartTime) / fadeOutTime);
            master = Mathf.Lerp(masterAtStop, 0f, k);
            currentT = tAtStop;
        }
        else
        {
            float elapsed = Time.time - startTime;
            currentT = elapsed < startDelay ? 0f : (1f - Mathf.Cos((elapsed - startDelay) * speed)) * 0.5f;
        }

        float power = Mathf.Lerp(minPower, maxPower, currentT) * master;
        float brightness = Mathf.Lerp(1f, Mathf.Lerp(minBrightness, 1f, currentT), master);

        for (int i = 0; i < mats.Count; i++)
        {
            if (mats[i] == null) continue;
            mats[i].SetFloat(GlowPowerID, power);

            if (pulseFaceBrightness)
            {
                Color b = baseColors[i];
                texts[i].color = new Color(b.r * brightness, b.g * brightness, b.b * brightness, b.a);
            }
        }
    }
}