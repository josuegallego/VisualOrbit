using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Que pasa cuando se acaba el tiempo del minijuego (se engancha SOLO a los eventos
/// "Al Perder Por Tiempo" y "Al Ganar" del NeuralNetworkManager, no hay que conectar nada a mano):
///   1) suena un sonido de fallo,
///   2) consecuencia visual: anillos bloqueados, flujo detenido, neuronas y entorno atenuados,
///   3) el panel de tiempo agotado lo muestra el propio NeuralNetworkManager,
///   4) si aun quedan intentos: la escena se reinicia sola tras 'retryDelay' segundos;
///      si ya se agotaron ('maxFailedAttempts'): NO reinicia, y muestra el boton Continuar
///      para que el jugador siga adelante en vez de quedarse atascado.
///
/// El resultado fallido ya lo guarda el NeuralNetworkManager (PlayerDataStore.SetTiempoMolecula).
/// PlayerStats y PlayerDataStore sobreviven a la recarga: el desempeno nutricional y la sesion
/// del jugador se mantienen en cada intento.
/// Ponlo en un objeto activo fuera de NeuralNetwork (por ejemplo "EnvironmentController").
/// </summary>
public class TimeUpConsequence : MonoBehaviour
{
    [Header("Consecuencias visuales")]
    public bool lockRings = true;
    public bool stopFlows = true;
    public bool dimNeurons = true;
    public bool dimEnvironment = true;

    [Header("Sonido de fallo")]
    [Tooltip("Tu propio sonido. Si queda vacio se genera uno (tono descendente).")]
    public AudioClip failClip;
    [Range(0f, 1f)] public float failVolume = 0.8f;
    [Tooltip("Opcional. Si queda vacio se crea uno en este objeto.")]
    public AudioSource audioSource;

    [Header("Reinicio")]
    [Tooltip("Reinicia solo tras el retraso. Si lo desactivas, usa un boton Reintentar (Retry).")]
    public bool autoRetry = true;
    [Tooltip("Segundos hasta reiniciar. Debe dar tiempo a oir el sonido y leer el panel.")]
    public float retryDelay = 4f;

    [Header("Tras varios fallos: ofrecer continuar")]
    [Tooltip("Intentos fallidos tras los cuales ya NO se reinicia solo y se ofrece continuar. 0 = nunca.")]
    public int maxFailedAttempts = 3;
    [Tooltip("Tu boton Continuar (o el panel que lo contiene). Se muestra al agotar los intentos.")]
    public GameObject continueOffer;
    [Tooltip("Segundos tras el tiempo agotado hasta mostrar el boton.")]
    public float continueOfferDelay = 1.5f;
    [Tooltip("Solo si tu boton llama a ContinueAnyway(). Vacio = siguiente escena de Build Settings.")]
    public string sceneName = "";

    [Header("Referencias (se buscan solas si estan vacias)")]
    public EnvironmentLighting environment;
    public SceneLoader sceneLoader;

    [Header("Depuracion")]
    public bool logValues = true;

    /// <summary>Intentos fallidos de la sesion actual (sobrevive a la recarga de escena).</summary>
    public static int FailedAttempts { get; private set; }

    static object attemptsOwner;   // sesion de jugador a la que pertenece el contador

    bool activated;
    bool retrying;
    bool subscribed;

    void Start()
    {
        ResetIfNewPlayer();

        var manager = NeuralNetworkManager.Instance;
        if (manager != null)
        {
            manager.alPerderPorTiempo.AddListener(Activate);
            manager.alGanar.AddListener(OnWin);
            subscribed = true;
        }
        else
        {
            Debug.LogWarning("[TimeUp] No hay NeuralNetworkManager en la escena: llama a Activate() a mano.", this);
        }
    }

    void OnDestroy()
    {
        if (subscribed && NeuralNetworkManager.Instance != null)
        {
            NeuralNetworkManager.Instance.alPerderPorTiempo.RemoveListener(Activate);
            NeuralNetworkManager.Instance.alGanar.RemoveListener(OnWin);
        }
    }

    // El contador pertenece a un jugador: si empieza otra sesion, se pone a cero
    static void ResetIfNewPlayer()
    {
        object current = PlayerDataStore.ActiveSession;
        if (!ReferenceEquals(attemptsOwner, current))
        {
            attemptsOwner = current;
            FailedAttempts = 0;
        }
    }

    void OnWin()
    {
        FailedAttempts = 0;
        if (logValues) Debug.Log("[TimeUp] Minijuego superado: contador de intentos fallidos a cero.", this);
    }

    /// <summary>Se llama solo al acabarse el tiempo. Tambien se puede conectar a mano.</summary>
    public void Activate()
    {
        if (activated) return;
        activated = true;

        ResetIfNewPlayer();
        FailedAttempts++;

        PlayFailSound();

        if (lockRings)
        {
            foreach (var ring in FindObjectsByType<TubeRingManipulator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var interactable = ring.GetComponent<XRBaseInteractable>();
                if (interactable != null) interactable.enabled = false;   // suelta el anillo si estaba agarrado
                ring.enabled = false;                                      // deja de girar
            }
        }

        if (stopFlows)
        {
            foreach (var flow in FindObjectsByType<GlucoseFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                flow.SetFlow(false);
        }

        if (dimNeurons)
        {
            foreach (var neuron in FindObjectsByType<NeuronController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                neuron.SetLit(false);
        }

        if (dimEnvironment)
        {
            if (environment == null) environment = FindFirstObjectByType<EnvironmentLighting>();
            if (environment != null) environment.SetTimeUp(true);
        }

        bool outOfAttempts = maxFailedAttempts > 0 && FailedAttempts >= maxFailedAttempts;

        if (outOfAttempts && continueOffer != null)
        {
            if (logValues)
                Debug.Log($"[TimeUp] Intento fallido {FailedAttempts}/{maxFailedAttempts}: se agotaron los intentos. " +
                          $"Se ofrece continuar en {continueOfferDelay:F1} s (sin reinicio automatico).", this);
            StartCoroutine(ShowContinueOffer());
            return;
        }

        if (outOfAttempts && continueOffer == null)
            Debug.LogWarning("[TimeUp] Se agotaron los intentos pero 'Continue Offer' esta vacio: sigo reiniciando para no dejar al jugador atascado.", this);

        if (logValues)
            Debug.Log($"[TimeUp] Intento fallido {FailedAttempts}" +
                      (maxFailedAttempts > 0 ? $"/{maxFailedAttempts}" : "") + ". " +
                      (autoRetry ? $"Reinicio automatico en {retryDelay:F1} s." : "Esperando el boton Reintentar."), this);

        if (autoRetry) StartCoroutine(RetryAfterDelay());
    }

    /// <summary>Tambien se puede conectar al On Click de un boton "Reintentar".</summary>
    public void Retry()
    {
        if (retrying) return;   // evita pulsaciones repetidas

        if (!EnsureSceneLoader()) return;

        retrying = true;
        if (logValues) Debug.Log("[TimeUp] Reiniciando la escena.", this);
        sceneLoader.ReloadCurrentScene();
    }

    /// <summary>
    /// Pasa a la siguiente escena aunque se haya fallado. Solo conectalo al On Click si tu boton
    /// NO es el de ContinueOnNeuronLit (ese ya carga la siguiente escena por si solo).
    /// </summary>
    public void ContinueAnyway()
    {
        if (retrying) return;

        if (!EnsureSceneLoader()) return;

        retrying = true;
        FailedAttempts = 0;
        if (logValues) Debug.Log("[TimeUp] Continuando a la siguiente escena.", this);

        if (string.IsNullOrEmpty(sceneName)) sceneLoader.LoadNextScene();
        else sceneLoader.LoadSceneByName(sceneName);
    }

    bool EnsureSceneLoader()
    {
        if (sceneLoader == null) sceneLoader = FindFirstObjectByType<SceneLoader>();
        if (sceneLoader != null) return true;

        Debug.LogError("[TimeUp] No hay ningun SceneLoader en la escena: asigna 'Scene Loader'.", this);
        return false;
    }

    IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, retryDelay));
        Retry();
    }

    IEnumerator ShowContinueOffer()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, continueOfferDelay));
        continueOffer.SetActive(true);
        if (logValues) Debug.Log("[TimeUp] Boton Continuar visible.", this);
    }

    // ------------------------------------------------------------ Sonido

    void PlayFailSound()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // sonido 2D, se oye igual en cualquier posicion
        }

        AudioClip clip = failClip != null ? failClip : MakeFailClip();
        audioSource.PlayOneShot(clip, failVolume);
    }

    // Tono descendente con un poco de "zumbido": suena a fallo sin necesitar ningun archivo
    static AudioClip MakeFailClip()
    {
        const int rate = 44100;
        const float seconds = 0.8f;
        int n = (int)(rate * seconds);
        var data = new float[n];
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float freq = Mathf.Lerp(330f, 110f, t);
            phase += 2f * Mathf.PI * freq / rate;

            float envelope = Mathf.Clamp01(t * 25f) * (1f - t);   // ataque rapido, caida suave
            float tone = Mathf.Sin(phase) * 0.35f + (Mathf.Sin(phase) > 0f ? 0.12f : -0.12f);
            data[i] = tone * envelope;
        }

        var clip = AudioClip.Create("fallo_generado", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}