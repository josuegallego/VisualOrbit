using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el boton Continuar cuando la neurona se enciende (verde) y, al pulsarlo,
/// carga la siguiente escena con el SceneLoader que ya existe en el proyecto.
///
/// Ponlo en un objeto vacio FUERA de NeuralNetwork (por ejemplo "ContinueGate").
/// El boton NO debe ser hijo de NeuralNetwork ni del propio objeto con este script.
/// No configures nada en el On Click del boton: este script lo conecta solo.
/// </summary>
public class ContinueOnNeuronLit : MonoBehaviour
{
    [Header("Origen (usa UNO de los dos)")]
    [Tooltip("Preferido: el boton aparece cuando TODAS las neuronas estan encendidas.")]
    public NeuronProgress progress;
    [Tooltip("Alternativa: el boton aparece cuando esta neurona se enciende.")]
    public NeuronController neuron;
    [Tooltip("Espera tras encenderse la neurona, para que termine la transicion a verde.")]
    public float showDelay = 1.2f;
    [Tooltip("Si la neurona se apaga, vuelve a ocultar el boton.")]
    public bool hideWhenUnlit = true;

    [Header("Boton Continuar")]
    public Button continueButton;

    [Header("Siguiente escena (usa SceneLoader)")]
    public SceneLoader sceneLoader;
    [Tooltip("Vacio = siguiente escena de Build Settings (LoadNextScene). Con nombre = LoadSceneByName.")]
    public string sceneName = "";

    [Header("Celebracion al aparecer el boton")]
    [Tooltip("Sistema de particulas de confeti (Play On Awake desactivado). Si queda vacio se crea uno basico.")]
    public ParticleSystem confetti;
    [Tooltip("Si esta activo, el confeti se coloca sobre el boton Continuar.")]
    public bool confettiAtButton = true;
    public Vector3 confettiOffset = new Vector3(0f, 0.15f, 0f);
    [Tooltip("Sonido de logro.")]
    public AudioClip successClip;
    [Tooltip("Opcional. Si queda vacio se crea una fuente de audio 2D.")]
    public AudioSource audioSource;
    [Tooltip("Ganancia del sonido. Se puede pasar de 1 para amplificar (mas de 3 puede distorsionar).")]
    [Range(0f, 5f)] public float successVolume = 3f;

    bool loading;
    bool celebrated;
    Coroutine showRoutine;

    void Awake()
    {
        if (continueButton == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Continue Button'.", this);
            return;
        }

        continueButton.gameObject.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnEnable()
    {
        if (progress != null)
        {
            progress.ProgressChanged += OnProgressChanged;
            OnProgressChanged(progress.Progress01);   // evalua el estado actual
            return;
        }

        if (neuron == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Progress' o 'Neuron'.", this);
            return;
        }

        neuron.LitChanged += OnLitChanged;
        if (neuron.IsLit) OnLitChanged(true);
    }

    void OnDisable()
    {
        if (progress != null) progress.ProgressChanged -= OnProgressChanged;
        if (neuron != null) neuron.LitChanged -= OnLitChanged;
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
    }

    void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    void OnProgressChanged(float value)
    {
        OnLitChanged(progress != null && progress.IsComplete);
    }

    void OnLitChanged(bool lit)
    {
        if (continueButton == null) return;

        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }

        if (lit)
        {
            showRoutine = StartCoroutine(ShowAfterDelay());
        }
        else if (hideWhenUnlit && !loading)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    IEnumerator ShowAfterDelay()
    {
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = !loading;
        showRoutine = null;
        Debug.Log("[ContinueOnNeuronLit] Boton Continuar visible.", this);

        Celebrate();
    }

    // Confeti y sonido: una sola vez cuando el boton aparece por primera vez
    void Celebrate()
    {
        if (celebrated) return;
        celebrated = true;

        if (confetti == null) confetti = CreateDefaultConfetti();

        if (confetti != null)
        {
            if (confettiAtButton && continueButton != null)
                confetti.transform.position = continueButton.transform.position + confettiOffset;

            confetti.gameObject.SetActive(true);
            confetti.Clear(true);
            confetti.Play(true);
        }

        if (successClip != null)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
            audioSource.PlayOneShot(successClip, successVolume);
        }
        else
        {
            Debug.LogWarning("[ContinueOnNeuronLit] No hay 'Success Clip': el confeti sale sin sonido.", this);
        }
    }

    ParticleSystem CreateDefaultConfetti()
    {
        var go = new GameObject("Confetti_Auto");
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

        // Colores variados por particula
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
        else Debug.LogWarning("[ContinueOnNeuronLit] No encuentro un shader para el confeti: asigna tu propio sistema de particulas.", this);

        return ps;
    }

    void OnContinueClicked()
    {
        if (loading) return;   // evita pulsaciones repetidas

        if (sceneLoader == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Scene Loader'.", this);
            return;
        }

        loading = true;
        continueButton.interactable = false;

        if (string.IsNullOrEmpty(sceneName)) sceneLoader.LoadNextScene();
        else sceneLoader.LoadSceneByName(sceneName);
    }
}