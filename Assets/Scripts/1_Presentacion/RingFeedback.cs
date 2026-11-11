using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HU 1.2.3 - Retroalimentacion visual y sonora inmediata al conectar un anillo.
///
///  - Anillo resuelto (bien)            -> bordes de la vision en VERDE (+ sonido bueno opcional).
///  - Anillo soltado mal colocado       -> bordes de la vision en ROJO + sonido de error.
///  - Anillo agarrado fuera de orden    -> bordes de la vision en ROJO (el sonido ya lo reproduce RingSequence).
///
/// Los bordes se dibujan con un Canvas pequeno pegado a la camara (se crea solo en tiempo de ejecucion),
/// asi funciona en VR sin post-procesado.
///
/// USO: ponlo en un objeto vacio (por ejemplo "RingFeedback") de la escena del minijuego.
///      Anade tambien un AudioSource (Spatial Blend = 0) y arrastralo a 'Audio Source'.
///      'Sequence' y 'Rings' se rellenan solos si los dejas vacios.
/// </summary>
public class RingFeedback : MonoBehaviour
{
    [Header("Origen (opcional: se buscan solos)")]
    public RingSequence sequence;
    public TubeRingManipulator[] rings;

    [Header("Bordes de la vision")]
    public bool useEdgeFlash = true;
    public Color goodColor = new Color(0.15f, 1f, 0.4f);
    public Color badColor = new Color(1f, 0.15f, 0.15f);
    [Range(0f, 1f)] public float flashIntensity = 0.7f;
    [Tooltip("Duracion total del destello.")]
    public float flashSeconds = 0.9f;

    [Header("VINETA: posicion y tamano (todo junto)")]
    [Tooltip("ACTIVALO para dejar la vineta FIJA y visible todo el tiempo y acomodarla con el visor puesto. " +
             "Todo lo de esta seccion se ve al instante. DESACTIVALO al terminar: vuelve a salir solo al acertar/fallar.")]
    public bool ajustarVineta = false;
    [Tooltip("Color del modo ajuste: marcado = ROJO (mal), desmarcado = VERDE (bien).")]
    public bool ajusteEnRojo = false;
    [Tooltip("Posicion respecto a la mirada, en metros: X = izquierda(-)/derecha(+), Y = abajo(-)/arriba(+), Z = distancia hacia delante.")]
    public Vector3 vinetaPosicion = new Vector3(0f, 0f, 0.47f);
    // Compatibilidad: VisionImpairment lee este valor
    public float canvasDistance => vinetaPosicion.z;
    [Tooltip("Tamano fijo del filtro (metros). Solo se usa si 'Auto Fit Fov' esta desactivado.")]
    public float overlaySize = 1f;
    [Tooltip("Ajusta solo el tamano del filtro al campo de vision real del visor.")]
    public bool autoFitFov = true;
    [Tooltip("Margen sobre el campo de vision (1.15 = 15 % mas grande). Solo con 'Auto Fit Fov'.")]
    public float fitMargin = 1.15f;
    [Tooltip("Tamano del centro transparente (mas alto = mas despejado).")]
    [Range(0.1f, 0.9f)] public float vignetteInner = 0.45f;
    [Tooltip("Posicion Z interna del objeto 'Vignette' dentro del Canvas. 147 funciono al probar en Play.")]
    public float vignetteZ = 147f;
    [Tooltip("Experimental: dibujar por encima de todo. Dejalo desactivado si ya se ve bien.")]
    public bool drawOnTop = false;

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Opcional. Si queda vacio no suena nada extra: RingSequence ya reproduce su 'Success Clip'.")]
    public AudioClip goodClip;
    [Tooltip("Suena al soltar un anillo mal colocado.")]
    public AudioClip badClip;
    [Tooltip("Ganancia del sonido de acierto. Se puede pasar de 1 para amplificar (mas de 3 puede distorsionar).")]
    [Range(0f, 5f)] public float volume = 1f;
    [Tooltip("Ganancia del sonido de error.")]
    [Range(0f, 5f)] public float badVolume = 3f;
    public float badCooldown = 0.5f;

    [Header("Indicadores propios BIEN / MAL (opcional)")]
    [Tooltip("Objeto de tu Canvas que se muestra al acertar (por ejemplo 'BIEN'). Puede estar desactivado.")]
    public GameObject goodIndicator;
    [Tooltip("Objeto de tu Canvas que se muestra al fallar (por ejemplo 'mal'). Puede estar desactivado.")]
    public GameObject badIndicator;
    [Tooltip("Segundos que se ve el indicador.")]
    public float indicatorSeconds = 1.2f;
    [Tooltip("Coloca el indicador delante de la camara del jugador cada vez que sale. " +
             "Apagalo si ya lo tienes colocado a mano en la escena.")]
    public bool placeInFrontOfCamera = true;
    [Tooltip("Metros hacia delante, en horizontal, desde la cabeza.")]
    public float indicatorDistance = 0.8f;
    [Tooltip("Metros hacia arriba (+) o abajo (-) respecto a los ojos.")]
    public float indicatorHeight = 0f;
    [Tooltip("Metros a la derecha (+) o a la izquierda (-).")]
    public float indicatorSide = 0f;

    [Header("Vibracion de los controles (HU 3.3.3)")]
    public bool useHaptics = true;

    [Header("Depuracion")]
    public bool logEvents = true;

    Camera playerCam;
    Canvas overlayCanvas;
    RawImage overlayImage;
    Texture2D vignetteTexture;
    Material overlayMaterial;
    Color flashColor;
    float flashTimer = -1f;
    float lastBadTime = -999f;
    float lastBadHaptic = -999f;
    float builtInner = -1f;
    bool wasAdjusting;

    const float Attack = 0.08f;

    void OnEnable()
    {
        if (sequence == null) sequence = FindFirstObjectByType<RingSequence>();
        if (rings == null || rings.Length == 0)
            rings = FindObjectsByType<TubeRingManipulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var r in rings)
        {
            if (r == null) continue;
            r.Solved += OnRingSolved;
            r.ReleasedWrong += OnRingReleasedWrong;
        }

        if (sequence != null) sequence.OutOfOrderAttempt += OnOutOfOrder;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        Application.onBeforeRender += Reposition;
        if (logEvents) Debug.Log($"[RingFeedback] Escuchando {rings.Length} anillos.", this);
    }

    void OnDisable()
    {
        if (goodIndicator != null && hideGood != null) goodIndicator.SetActive(false);
        if (badIndicator != null && hideBad != null) badIndicator.SetActive(false);

        Application.onBeforeRender -= Reposition;
        if (rings != null)
            foreach (var r in rings)
            {
                if (r == null) continue;
                r.Solved -= OnRingSolved;
                r.ReleasedWrong -= OnRingReleasedWrong;
            }

        if (sequence != null) sequence.OutOfOrderAttempt -= OnOutOfOrder;
    }

    void OnDestroy()
    {
        if (vignetteTexture != null) Destroy(vignetteTexture);
        if (overlayMaterial != null) Destroy(overlayMaterial);
        if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
    }

    // ------------------------------------------------------------ Eventos

    void OnRingSolved(TubeRingManipulator ring)
    {
        if (logEvents) Debug.Log($"[RingFeedback] '{ring.name}' BIEN -> verde", this);
        Flash(goodColor);
        ShowIndicator(goodIndicator, true);
        Play(goodClip, volume);
        if (useHaptics) HapticService.Success();
    }

    void OnRingReleasedWrong(TubeRingManipulator ring, float error)
    {
        if (logEvents) Debug.Log($"[RingFeedback] '{ring.name}' MAL ({error:F0} grados) -> rojo", this);
        Flash(badColor);
        ShowIndicator(badIndicator, false);

        if (Time.time - lastBadTime >= badCooldown)
        {
            lastBadTime = Time.time;
            Play(badClip, badVolume);
            if (useHaptics) HapticService.Error();
        }
    }

    void OnOutOfOrder(TubeRingManipulator ring)
    {
        if (logEvents) Debug.Log($"[RingFeedback] '{ring.name}' fuera de orden -> rojo", this);
        Flash(badColor);
        ShowIndicator(badIndicator, false);
        if (useHaptics && Time.time - lastBadHaptic >= badCooldown)
        {
            lastBadHaptic = Time.time;
            HapticService.Error();
        }
    }

    // ------------------------------------------------------------ Uso desde otros minijuegos (ej. memorama)

    /// <summary>Destello verde en los bordes + vibracion de acierto. Llamar desde fuera (UnityEvent o codigo).</summary>
    public void FlashBien()
    {
        Flash(goodColor);
        if (useHaptics) HapticService.Success();
    }

    /// <summary>Destello rojo en los bordes + vibracion de error. Llamar desde fuera (UnityEvent o codigo).</summary>
    public void FlashMal()
    {
        Flash(badColor);
        if (useHaptics && Time.time - lastBadHaptic >= badCooldown)
        {
            lastBadHaptic = Time.time;
            HapticService.Error();
        }
    }

    // ------------------------------------------------------------ Audio

    void Play(AudioClip clip, float gain)
    {
        if (clip == null || audioSource == null) return;
        audioSource.volume = 1f;
        audioSource.PlayOneShot(clip, gain);
    }

    // ------------------------------------------------------------ Bordes de la vision

    void Flash(Color color)
    {
        if (!useEdgeFlash) return;
        if (ajustarVineta) return;          // en modo ajuste la vineta ya esta fija
        if (!EnsureOverlay()) return;

        flashColor = color;
        flashTimer = 0f;
        overlayCanvas.enabled = true;
        Reposition();
    }

    void Update()
    {
        if (ajustarVineta)
        {
            AdjustPreview();
            return;
        }
        if (wasAdjusting)
        {
            wasAdjusting = false;
            flashTimer = -1f;
            if (overlayCanvas != null) overlayCanvas.enabled = false;
        }

        if (flashTimer < 0f || overlayImage == null) return;

        flashTimer += Time.deltaTime;
        float total = Mathf.Max(0.2f, flashSeconds);

        float k;
        if (flashTimer < Attack) k = flashTimer / Attack;                         // sube rapido
        else k = 1f - Mathf.SmoothStep(0f, 1f, (flashTimer - Attack) / (total - Attack)); // baja suave

        overlayImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Clamp01(k) * flashIntensity);

        if (flashTimer >= total)
        {
            flashTimer = -1f;
            overlayCanvas.enabled = false;   // no dibujar nada cuando no hay destello
        }
    }

    // Modo ajuste: vineta fija y visible; todos los valores se aplican en vivo
    void AdjustPreview()
    {
        wasAdjusting = true;
        flashTimer = -1f;
        if (!EnsureOverlay()) return;

        if (!Mathf.Approximately(builtInner, vignetteInner))
        {
            if (vignetteTexture != null) Destroy(vignetteTexture);
            vignetteTexture = null;
            overlayImage.texture = GetVignetteTexture();
        }

        Color c = ajusteEnRojo ? badColor : goodColor;
        overlayImage.color = new Color(c.r, c.g, c.b, flashIntensity);
        overlayCanvas.enabled = true;
        Reposition();
    }

    // ------------------------------------------------------------ Indicadores BIEN / MAL

    Coroutine hideGood, hideBad;

    void ShowIndicator(GameObject indicator, bool good)
    {
        if (indicator == null) return;

        // Solo uno a la vez
        GameObject other = good ? badIndicator : goodIndicator;
        if (other != null) other.SetActive(false);

        if (placeInFrontOfCamera)
        {
            var cam = FindPlayerCamera();
            if (cam != null)
            {
                Transform c = cam.transform;
                Vector3 fwd = Vector3.ProjectOnPlane(c.forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(c.up, Vector3.up);
                fwd.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, fwd);

                Vector3 pos = c.position + fwd * indicatorDistance + right * indicatorSide + Vector3.up * indicatorHeight;
                indicator.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
            }
        }

        indicator.SetActive(true);

        if (good)
        {
            if (hideGood != null) StopCoroutine(hideGood);
            hideGood = StartCoroutine(HideAfter(indicator, true));
        }
        else
        {
            if (hideBad != null) StopCoroutine(hideBad);
            hideBad = StartCoroutine(HideAfter(indicator, false));
        }
    }

    System.Collections.IEnumerator HideAfter(GameObject indicator, bool good)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, indicatorSeconds));
        if (indicator != null) indicator.SetActive(false);
        if (good) hideGood = null; else hideBad = null;
    }

    [ContextMenu("Capturar posicion del indicador BIEN")]
    void CaptureGood() => CaptureIndicator(goodIndicator);

    [ContextMenu("Capturar posicion del indicador MAL")]
    void CaptureBad() => CaptureIndicator(badIndicator);

    // Mide donde esta el indicador respecto a la camara (dale Play, activa el objeto y colocalo a mano)
    void CaptureIndicator(GameObject indicator)
    {
        var cam = FindPlayerCamera();
        if (indicator == null || cam == null)
        {
            Debug.LogWarning("[RingFeedback] Falta el indicador o no encuentro la camara. Dale Play y coloca el objeto antes de capturar.", this);
            return;
        }

        Transform c = cam.transform;
        Vector3 fwd = Vector3.ProjectOnPlane(c.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(c.up, Vector3.up);
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 to = indicator.transform.position - c.position;

        indicatorDistance = Vector3.Dot(to, fwd);
        indicatorSide = Vector3.Dot(to, right);
        indicatorHeight = to.y;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[RingFeedback] Capturado: distancia {indicatorDistance:F2} m, altura {indicatorHeight:F2} m, lado {indicatorSide:F2} m. " +
                  "Ahora Copy Component, detén Play y Paste Component Values.", this);
    }

    // Camara REAL del jugador (la del visor), no cualquiera con el tag MainCamera
    Camera FindPlayerCamera()
    {
        var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null && origin.Camera != null && origin.Camera.isActiveAndEnabled)
            return origin.Camera;

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled) return main;

        foreach (var c in Camera.allCameras)
            if (c.stereoTargetEye != StereoTargetEyeMask.None) return c;

        return null;
    }

    // Crea el filtro (si hace falta) y lo ajusta al campo de vision de la camara del jugador.
    // NO es hijo de la camara: en cada fotograma se coloca delante de ella (ver Reposition).
    bool EnsureOverlay()
    {
        var cam = FindPlayerCamera();
        if (cam == null)
        {
            if (logEvents) Debug.LogWarning("[RingFeedback] No encuentro la camara del jugador: no se muestran los bordes.", this);
            return false;
        }

        if (cam != playerCam && logEvents) Debug.Log($"[RingFeedback] Camara del jugador: '{cam.name}'.", this);
        playerCam = cam;

        if (overlayCanvas == null)
        {
            int layer = LayerMask.NameToLayer("UI");
            if (layer < 0 || (cam.cullingMask & (1 << layer)) == 0) layer = cam.gameObject.layer;

            var go = new GameObject("RingFeedback_Edges", typeof(RectTransform));
            go.layer = layer;

            overlayCanvas = go.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.WorldSpace;
            overlayCanvas.sortingOrder = 1000;

            ((RectTransform)go.transform).sizeDelta = new Vector2(1000f, 1000f);

            var imgGo = new GameObject("Vignette", typeof(RectTransform));
            imgGo.layer = layer;
            imgGo.transform.SetParent(go.transform, false);

            overlayImage = imgGo.AddComponent<RawImage>();
            overlayImage.raycastTarget = false;
            overlayImage.texture = GetVignetteTexture();
            overlayImage.color = new Color(1f, 1f, 1f, 0f);

            if (drawOnTop)
            {
                overlayMaterial = new Material(overlayImage.defaultMaterial);
                overlayMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                overlayImage.material = overlayMaterial;
            }

            var irt = overlayImage.rectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;

            overlayCanvas.enabled = false;
        }

        overlayCanvas.worldCamera = cam;
        ApplySize();
        Reposition();
        return true;
    }

    // Distancia del Canvas a la camara
    float EffectiveDistance()
    {
        return Mathf.Max(0.05f, vinetaPosicion.z);
    }

    // Tamano del filtro = lo justo para cubrir el campo de vision real del visor
    void ApplySize()
    {
        float dist = EffectiveDistance();
        float size = overlaySize;

        if (autoFitFov && playerCam != null)
        {
            Matrix4x4 p = playerCam.stereoEnabled
                ? playerCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left)
                : playerCam.projectionMatrix;

            float halfW = dist / Mathf.Max(0.01f, Mathf.Abs(p.m00));
            float halfH = dist / Mathf.Max(0.01f, Mathf.Abs(p.m11));
            size = 2f * Mathf.Max(halfW, halfH) * fitMargin;
        }

        overlayCanvas.transform.localScale = Vector3.one * (Mathf.Max(0.05f, size) / 1000f);

        // Mismo valor que se probo a mano en Play (Pos Z del objeto Vignette)
        overlayImage.rectTransform.anchoredPosition3D = new Vector3(0f, 0f, vignetteZ);
    }

    // Coloca el filtro delante de la mirada. Se llama justo antes de dibujar cada fotograma
    // para que siga a la cabeza sin retraso.
    void Reposition()
    {
        if (overlayCanvas == null || playerCam == null || !overlayCanvas.enabled) return;

        Transform c = playerCam.transform;
        overlayCanvas.transform.SetPositionAndRotation(
            c.position + c.forward * EffectiveDistance() + c.right * vinetaPosicion.x + c.up * vinetaPosicion.y, c.rotation);
    }

    // Textura blanca: transparente en el centro y opaca hacia los bordes (se tine con el color del destello)
    Texture2D GetVignetteTexture()
    {
        if (vignetteTexture != null) return vignetteTexture;

        const int size = 128;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;
        vignetteTexture.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;   // 0 centro, 1 esquinas
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(vignetteInner, 1f, d));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        builtInner = vignetteInner;
        vignetteTexture.SetPixels32(pixels);
        vignetteTexture.Apply(false, true);
        return vignetteTexture;
    }
}
