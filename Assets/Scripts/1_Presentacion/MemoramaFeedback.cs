using UnityEngine;

/// <summary>
/// Retroalimentacion del MEMORAMA igual que en "Arma la molecula":
///   - Pareja acertada  -> bordes de la vision en VERDE (+ vibracion de acierto)
///   - Pareja fallada   -> bordes de la vision en ROJO  (+ vibracion de error)
///   - Memorama ganado  -> confeti
///
/// USO: ponlo en un objeto vacio de la escena del memorama (junto a un objeto con RingFeedback).
///      Se conecta solo a MemoramaManager (alAcertar / alFallar / alGanar); no hay que tocar eventos.
/// </summary>
public class MemoramaFeedback : MonoBehaviour
{
    [Header("Origen (se buscan solos)")]
    public MemoramaManager manager;
    [Tooltip("RingFeedback de ESTA escena: de aqui salen las vinetas verde/roja. Copia el de Arma la molecula (Copy Component / Paste Values) para conservar la posicion.")]
    public RingFeedback vineta;

    [Header("Vineta")]
    public bool vinetaAlAcertar = true;
    public bool vinetaAlFallar = true;

    [Header("Confeti")]
    public bool confetiAlGanar = true;
    [Tooltip("Tambien suelta confeti (mas pequeno) en cada pareja acertada.")]
    public bool confetiEnCadaPareja = false;
    [Tooltip("Sistema de particulas propio (Play On Awake desactivado). Vacio = se crea uno igual al de Arma la molecula.")]
    public ParticleSystem confeti;
    [Tooltip("Metros hacia delante de la cabeza.")]
    public float distancia = 1.4f;
    [Tooltip("Metros respecto a los ojos (negativo = mas abajo; el confeti sube desde ahi).")]
    public float altura = -0.45f;
    public float lado = 0f;

    [Header("Sonido de logro al ganar (opcional)")]
    public AudioClip sonidoLogro;
    [Range(0f, 5f)] public float volumenLogro = 2f;

    AudioSource audioFuente;

    void OnEnable()
    {
        if (manager == null) manager = FindFirstObjectByType<MemoramaManager>();
        if (vineta == null) vineta = FindFirstObjectByType<RingFeedback>();

        if (manager == null) { Debug.LogWarning("[MemoramaFeedback] No encuentro MemoramaManager.", this); return; }
        if (vineta == null) Debug.LogWarning("[MemoramaFeedback] No hay RingFeedback en la escena: no habra vineta (el confeti si).", this);

        manager.alAcertar.AddListener(AlAcertar);
        manager.alFallar.AddListener(AlFallar);
        manager.alGanar.AddListener(AlGanar);
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.alAcertar.RemoveListener(AlAcertar);
        manager.alFallar.RemoveListener(AlFallar);
        manager.alGanar.RemoveListener(AlGanar);
    }

    void AlAcertar()
    {
        if (vinetaAlAcertar && vineta != null) vineta.FlashBien();
        if (confetiEnCadaPareja) Confeti(0.5f);
    }

    void AlFallar()
    {
        if (vinetaAlFallar && vineta != null) vineta.FlashMal();
    }

    void AlGanar()
    {
        if (confetiAlGanar) Confeti(1f);
        if (sonidoLogro != null)
        {
            if (audioFuente == null)
            {
                audioFuente = gameObject.AddComponent<AudioSource>();
                audioFuente.playOnAwake = false;
                audioFuente.spatialBlend = 0f;
            }
            audioFuente.PlayOneShot(sonidoLogro, volumenLogro);
        }
    }

    // ------------------------------------------------------------ Confeti

    void Confeti(float escala)
    {
        if (confeti == null) confeti = CrearConfeti();
        if (confeti == null) return;

        var cam = BuscarCamara();
        if (cam != null)
        {
            Transform c = cam.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(c.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(c.up, Vector3.up);
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            confeti.transform.position = c.position + fwd * distancia + right * lado + Vector3.up * altura;
        }

        confeti.gameObject.SetActive(true);
        var em = confeti.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(10, Mathf.RoundToInt(150 * escala))) });
        confeti.Clear(true);
        confeti.Play(true);
    }

    Camera BuscarCamara()
    {
        var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null && origin.Camera != null && origin.Camera.isActiveAndEnabled) return origin.Camera;
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled) return main;
        return null;
    }

    // Mismo confeti que ContinueOnNeuronLit (Arma la molecula)
    ParticleSystem CrearConfeti()
    {
        var go = new GameObject("Confetti_Memorama");
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // dispara hacia arriba

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.gravityModifier = 0.35f;
        main.maxParticles = 300;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.25f, 0.3f), new Color(0.2f, 0.6f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 150) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.1f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh != null) renderer.material = new Material(sh);
        else Debug.LogWarning("[MemoramaFeedback] No encuentro shader para el confeti: asigna tu propio sistema de particulas.", this);

        return ps;
    }
}
