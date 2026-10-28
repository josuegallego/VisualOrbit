using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Vision borrosa / mareada segun el estado de las barras (PlayerStats.BlurLevel01):
/// mas interferencia cognitiva, menos energia y menos memoria = mas efecto.
///
/// El efecto va y viene en BUCLE para no volver el juego imposible:
///     medio borroso -> muy borroso -> vision limpia -> (repite)
/// Cuanto peor estan las barras, mas largos son los tramos borrosos y mas cortos los limpios.
///
/// Tiene dos capas (usa una o las dos):
///   1) Velo: un filtro lechoso delante de la mirada (como la vineta de los anillos) que tapa un poco
///      y se mece suavemente. Es barato y funciona en Quest.
///   2) Volume de post-proceso (opcional): BLUR real con Depth of Field. Es mas costoso en Quest.
///      Crea un Global Volume con Depth Of Field (Gaussian) y arrastralo a 'Blur Volume'.
///      Este script solo cambia el 'Weight' del Volume entre 0 y 1.
///
/// USO: ponlo en un objeto vacio de la escena del minijuego (memorama y arma la molecula).
/// NO mueve la camara ni la escena: solo el velo, para no provocar mareo real.
/// </summary>
public class VisionImpairment : MonoBehaviour
{
    [Header("Cuando se activa")]
    [Tooltip("Por debajo de este nivel (0-1) no hay efecto.")]
    [Range(0f, 1f)] public float minSeverity = 0.2f;
    [Tooltip("Que tan rapido sigue los cambios de las barras.")]
    public float severitySmoothing = 1.5f;
    [Tooltip("Prueba sin barras: si es mayor que 0 se usa este nivel en vez de las barras.")]
    [Range(0f, 1f)] public float forceSeverity = 0f;

    [Header("Bucle (segundos)")]
    [Tooltip("Tramo medio borroso: con barras en buen estado -> con barras muy mal.")]
    public Vector2 mediumHold = new Vector2(1.5f, 3f);
    [Tooltip("Tramo muy borroso.")]
    public Vector2 strongHold = new Vector2(1f, 4f);
    [Tooltip("Tramo limpio: con barras apenas mal -> con barras muy mal (se acorta).")]
    public Vector2 clearHold = new Vector2(10f, 3f);
    [Tooltip("Cuanto tarda en pasar de un tramo a otro.")]
    public float transitionSeconds = 1.5f;
    [Range(0f, 1f)] public float mediumLevel = 0.5f;

    [Header("Capa 1: velo")]
    public bool useVeil = true;
    public Color veilColor = new Color(0.85f, 0.88f, 0.95f);
    [Tooltip("Opacidad maxima del velo.")]
    [Range(0f, 1f)] public float maxVeilAlpha = 0.75f;
    [Tooltip("Opacidad en el centro respecto a los bordes (mas alto = tapa mas el centro).")]
    [Range(0f, 1f)] public float centerAlpha = 0.35f;
    [Range(0.1f, 0.9f)] public float veilInner = 0.2f;
    [Tooltip("Cabeceo del velo en grados (solo el velo, no la escena).")]
    public float swayDegrees = 3f;
    public float swaySpeed = 0.9f;
    [Tooltip("Latido del tamano del velo (0.04 = 4 %).")]
    [Range(0f, 0.2f)] public float breathe = 0.04f;
    [Tooltip("Si hay un RingFeedback en la escena, copia de el la distancia, el tamano y el Z " +
             "(los valores que ya se ven bien). Recomendado.")]
    public bool copyFromRingFeedback = true;
    [Tooltip("Solo se usan si 'Copy From Ring Feedback' esta apagado o no hay RingFeedback.")]
    public float canvasDistance = 0.47f;
    public float veilZ = 147f;
    public bool autoFitFov = false;
    public float overlaySize = 1.6f;
    public float fitMargin = 1.15f;
    [Tooltip("Agranda el velo para que no queden huecos en los bordes (sobre todo con el cabeceo). 1 = tal cual.")]
    public float veilSizeScale = 1.4f;
    [Tooltip("Ensancha el velo solo en horizontal. 1 = cuadrado.")]
    public float veilWidthScale = 1.5f;

    [Header("Capa 2: blur real (Depth of Field)")]
    [Tooltip("Crea solo un Volume con Depth Of Field Gaussian y lo anima. No necesitas crear nada a mano.")]
    public bool useBlurVolume = true;
    [Tooltip("Opcional: tu propio Global Volume. Si lo asignas, no se crea el automatico.")]
    public Volume blurVolume;
    [Range(0f, 1f)] public float maxVolumeWeight = 1f;
    [Tooltip("Desde esta distancia (m) empieza el desenfoque. Mas bajo = desenfoca tambien lo cercano.")]
    public float blurStart = 0.1f;
    [Tooltip("A esta distancia (m) el desenfoque es maximo. Mas bajo = mas borroso.")]
    public float blurEnd = 0.6f;
    [Range(0.5f, 1.5f)] public float blurMaxRadius = 1.5f;
    [Tooltip("Aberracion cromatica (franjas de color en los bordes) para sensacion de mareo.")]
    [Range(0f, 1f)] public float chromatic = 0.5f;

    [Header("Depuracion")]
    public bool logEvents = true;

    // estado del bucle
    enum Stage { Medium, Strong, Clear }
    Stage stage = Stage.Clear;
    float stageTimer;
    float stageDuration;
    float severity;
    float intensity;   // 0..1 efecto actual

    Camera playerCam;
    Canvas canvas;
    RawImage image;
    Texture2D texture;
    VolumeProfile runtimeProfile;
    GameObject runtimeVolumeGo;

    void OnDestroy()
    {
        if (texture != null) Destroy(texture);
        if (runtimeVolumeGo != null) Destroy(runtimeVolumeGo);
        if (runtimeProfile != null) Destroy(runtimeProfile);
        if (canvas != null) Destroy(canvas.gameObject);
    }

    void OnEnable()
    {
        Application.onBeforeRender += Reposition;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= Reposition;
        if (blurVolume != null) blurVolume.weight = 0f;
        if (canvas != null) canvas.enabled = false;
    }

    float TargetSeverity()
    {
        if (forceSeverity > 0f) return forceSeverity;
        var stats = PlayerStats.Instance;
        return stats != null ? stats.BlurLevel01() : 0f;
    }

    void Update()
    {
        float target = TargetSeverity();
        severity = Mathf.MoveTowards(severity, target, Time.deltaTime / Mathf.Max(0.05f, severitySmoothing));

        float goal;
        if (severity < minSeverity)
        {
            stage = Stage.Clear;
            stageTimer = 0f;
            stageDuration = 0f;
            goal = 0f;
        }
        else
        {
            AdvanceStage();
            goal = StageLevel(stage) * Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(minSeverity, 1f, severity));
        }

        intensity = Mathf.MoveTowards(intensity, goal, Time.deltaTime / Mathf.Max(0.05f, transitionSeconds));

        ApplyVolume();
        ApplyVeil();
    }

    // Medio borroso -> muy borroso -> limpio -> repite. Las duraciones dependen de que tan mal van las barras.
    void AdvanceStage()
    {
        float k = Mathf.InverseLerp(minSeverity, 1f, severity);

        if (stageDuration <= 0f)
        {
            stage = Stage.Medium;
            stageDuration = Hold(mediumHold, k);
            stageTimer = 0f;
            return;
        }

        stageTimer += Time.deltaTime;
        if (stageTimer < stageDuration) return;

        stageTimer = 0f;
        switch (stage)
        {
            case Stage.Medium: stage = Stage.Strong; stageDuration = Hold(strongHold, k); break;
            case Stage.Strong: stage = Stage.Clear;  stageDuration = Hold(clearHold, k);  break;
            default:           stage = Stage.Medium; stageDuration = Hold(mediumHold, k);  break;
        }

        if (logEvents) Debug.Log($"[VisionImpairment] Tramo {stage} ({stageDuration:F1}s), gravedad {severity:F2}", this);
    }

    static float Hold(Vector2 range, float k) => Mathf.Lerp(range.x, range.y, k);

    float StageLevel(Stage s)
    {
        switch (s)
        {
            case Stage.Medium: return mediumLevel;
            case Stage.Strong: return 1f;
            default: return 0f;
        }
    }

    // ------------------------------------------------------------ Capa 2: Volume

    void ApplyVolume()
    {
        if (!useBlurVolume) return;

        if (blurVolume == null)
        {
            if (intensity <= 0.01f) return;   // se crea la primera vez que hace falta
            CreateRuntimeVolume();
            if (blurVolume == null) return;
        }

        blurVolume.weight = intensity * maxVolumeWeight;
    }

    void CreateRuntimeVolume()
    {
        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

        var dof = runtimeProfile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(blurStart);
        dof.gaussianEnd.Override(Mathf.Max(blurStart + 0.01f, blurEnd));
        dof.gaussianMaxRadius.Override(blurMaxRadius);
        dof.highQualitySampling.Override(false);

        var ca = runtimeProfile.Add<ChromaticAberration>(true);
        ca.intensity.Override(chromatic);

        runtimeVolumeGo = new GameObject("VisionImpairment_Volume");
        var vol = runtimeVolumeGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 100;
        vol.weight = 0f;
        vol.sharedProfile = runtimeProfile;
        blurVolume = vol;

        // La camara del jugador debe tener el post-proceso activado
        var cam = FindPlayerCamera();
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && !data.renderPostProcessing)
            {
                data.renderPostProcessing = true;
                if (logEvents) Debug.Log("[VisionImpairment] Active el Post Processing en la camara del jugador.", this);
            }
        }

        if (logEvents) Debug.Log("[VisionImpairment] Volume de desenfoque creado.", this);
    }

    // ------------------------------------------------------------ Capa 1: velo

    void ApplyVeil()
    {
        if (!useVeil) return;

        if (intensity <= 0.01f)
        {
            if (canvas != null && canvas.enabled) canvas.enabled = false;
            return;
        }

        if (!EnsureVeil()) return;

        canvas.enabled = true;
        image.color = new Color(veilColor.r, veilColor.g, veilColor.b, intensity * maxVeilAlpha);
        Reposition();
    }

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

    // Toma los valores de RingFeedback para que el velo se vea igual de bien que la vineta
    void SyncFromRingFeedback()
    {
        if (!copyFromRingFeedback) return;
        var rf = FindFirstObjectByType<RingFeedback>(FindObjectsInactive.Include);
        if (rf == null) return;

        canvasDistance = rf.canvasDistance;
        veilZ = rf.vignetteZ;
        autoFitFov = rf.autoFitFov;
        overlaySize = rf.overlaySize;
        fitMargin = rf.fitMargin;
    }

    bool EnsureVeil()
    {
        var cam = FindPlayerCamera();
        if (cam == null) return false;
        playerCam = cam;

        if (canvas == null)
        {
            SyncFromRingFeedback();

            int layer = LayerMask.NameToLayer("UI");
            if (layer < 0 || (cam.cullingMask & (1 << layer)) == 0) layer = cam.gameObject.layer;

            var go = new GameObject("VisionImpairment_Veil", typeof(RectTransform));
            go.layer = layer;

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 900;   // por debajo de los destellos de RingFeedback (1000)
            ((RectTransform)go.transform).sizeDelta = new Vector2(1000f, 1000f);

            var imgGo = new GameObject("Veil", typeof(RectTransform));
            imgGo.layer = layer;
            imgGo.transform.SetParent(go.transform, false);

            image = imgGo.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = GetTexture();
            image.color = new Color(1f, 1f, 1f, 0f);

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition3D = new Vector3(0f, 0f, veilZ);
        }

        canvas.worldCamera = cam;
        return true;
    }

    float FittedSize()
    {
        float dist = canvasDistance;
        if (!autoFitFov || playerCam == null) return overlaySize;

        Matrix4x4 p = playerCam.stereoEnabled
            ? playerCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left)
            : playerCam.projectionMatrix;

        float halfW = dist / Mathf.Max(0.01f, Mathf.Abs(p.m00));
        float halfH = dist / Mathf.Max(0.01f, Mathf.Abs(p.m11));
        return 2f * Mathf.Max(halfW, halfH) * fitMargin;
    }

    // Delante de la mirada, con cabeceo y latido suaves (solo el velo)
    void Reposition()
    {
        if (canvas == null || playerCam == null || !canvas.enabled) return;

        float t = Time.time * swaySpeed;
        float roll = Mathf.Sin(t) * swayDegrees * intensity;
        float scale = 1f + Mathf.Sin(t * 1.7f) * breathe * intensity;

        Transform c = playerCam.transform;
        canvas.transform.SetPositionAndRotation(
            c.position + c.forward * canvasDistance,
            c.rotation * Quaternion.Euler(0f, 0f, roll));
        float size = Mathf.Max(0.05f, FittedSize() * veilSizeScale * scale) / 1000f;
        canvas.transform.localScale = new Vector3(size * veilWidthScale, size, size);
    }

    // Blanca: translucida en el centro (centerAlpha) y mas opaca hacia los bordes
    Texture2D GetTexture()
    {
        if (texture != null) return texture;

        const int size = 128;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(veilInner, 1f, d));
                float a = Mathf.Lerp(centerAlpha, 1f, edge);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
