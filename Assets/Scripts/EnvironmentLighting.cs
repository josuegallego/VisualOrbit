using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Iluminacion progresiva del entorno (el video de fondo) segun las neuronas encendidas.
///
///   brillo = Lerp(baseBrightness, maxBrightness, progreso)
///   maxBrightness = Lerp(maxBrightnessBadPerformance, maxBrightnessGoodPerformance, desempeno)
///
/// Con buen desempeno el entorno llega a brillo completo; con mal desempeno se queda mas tenue.
/// El minijuego se puede completar igual: solo cambia el aspecto del entorno.
///
/// Segun el Render Mode del Video Player:
///  - Camera Far/Near Plane -> usa Video Player.targetCameraAlpha (la camara debe tener fondo negro solido).
///  - Material Override o Render Texture -> tiñe el material del objeto donde se ve el video.
/// </summary>
public class EnvironmentLighting : MonoBehaviour
{
    [Header("Origen")]
    public NeuronProgress progress;

    [Header("Entorno (video)")]
    public VideoPlayer video;
    [Tooltip("Solo si el video se ve sobre un objeto (Material Override / Render Texture). " +
             "Vacio = usa el objeto del Video Player.")]
    public Renderer videoSurface;

    [Header("Brillo")]
    [Tooltip("Brillo sin ninguna neurona encendida.")]
    [Range(0f, 1f)] public float baseBrightness = 0.15f;
    [Range(0f, 1f)] public float maxBrightnessBadPerformance = 0.55f;
    [Range(0f, 1f)] public float maxBrightnessGoodPerformance = 1f;
    [Tooltip("Segundos que tarda en llegar al brillo objetivo (subida suave).")]
    public float smoothSeconds = 1.5f;

    [Header("Color del entorno (solo si se tiñe un material)")]
    public Color dimTint = new Color(0.55f, 0.65f, 1f);
    public Color fullTint = Color.white;

    [Header("Luz direccional (opcional)")]
    public Light directionalLight;
    public float lightMinIntensity = 0.2f;
    public float lightMaxIntensity = 1f;

    [Header("Pruebas")]
    public bool useManualProgress = false;
    [Range(0f, 1f)] public float manualProgress = 0f;
    [Tooltip("Si esta activo y existe NeuralNetworkManager, usa su Performance01.")]
    public bool useManagerPerformance = true;
    [Range(0f, 1f)] public float testPerformance01 = 1f;
    public bool logValues = true;

    /// <summary>Multiplicador externo (1 = normal). Util si otro script quiere atenuar el entorno.</summary>
    [HideInInspector] public float brightnessMultiplier = 1f;

    enum Mode { None, CameraAlpha, TintMaterial }

    Mode mode = Mode.None;
    Renderer surface;
    MaterialPropertyBlock block;
    int colorId;
    float current;
    float lastLogged = -1f;

    void Awake()
    {
        if (progress == null)
        {
            progress = FindFirstObjectByType<NeuronProgress>();
            if (progress == null && !useManualProgress)
                Debug.LogWarning("[EnvironmentLighting] No hay NeuronProgress en la escena: el progreso valdra siempre 0.", this);
        }

        if (useManualProgress)
            Debug.LogWarning("[EnvironmentLighting] 'Use Manual Progress' esta ACTIVO: se ignoran las neuronas.", this);

        if (video != null)
        {
            if (videoSurface != null)
            {
                surface = videoSurface;
                mode = Mode.TintMaterial;
            }
            else if (video.renderMode == VideoRenderMode.CameraFarPlane ||
                     video.renderMode == VideoRenderMode.CameraNearPlane)
            {
                mode = Mode.CameraAlpha;
            }
            else if (video.renderMode == VideoRenderMode.MaterialOverride && video.targetMaterialRenderer != null)
            {
                surface = video.targetMaterialRenderer;
                mode = Mode.TintMaterial;
            }
        }

        if (mode == Mode.TintMaterial)
        {
            block = new MaterialPropertyBlock();
            var mat = surface.sharedMaterial;
            if (mat != null && mat.HasProperty("_BaseColor")) colorId = Shader.PropertyToID("_BaseColor");
            else if (mat != null && mat.HasProperty("_Color")) colorId = Shader.PropertyToID("_Color");
            else
            {
                Debug.LogWarning("[EnvironmentLighting] El material del video no tiene _BaseColor ni _Color: " +
                                 "no se puede teñir. Cambia su shader a Universal Render Pipeline/Unlit.", this);
                mode = Mode.None;
            }
        }

        if (mode == Mode.None)
            Debug.LogWarning("[EnvironmentLighting] No se pudo determinar como oscurecer el video. " +
                             "Revisa 'Video' y el Render Mode del Video Player.", this);

        current = baseBrightness;
        Apply(current);

        if (logValues)
            Debug.Log($"[EnvironmentLighting] modo={mode} | progress={(progress != null ? progress.name : "NINGUNO")} | " +
                      $"base={baseBrightness:F2} | max malo={maxBrightnessBadPerformance:F2} | max bueno={maxBrightnessGoodPerformance:F2}", this);
    }

    void OnEnable()
    {
        if (progress != null) progress.ProgressChanged += OnProgressChanged;
    }

    void OnDisable()
    {
        if (progress != null) progress.ProgressChanged -= OnProgressChanged;
    }

    void OnProgressChanged(float value)
    {
        if (logValues)
            Debug.Log($"[EnvironmentLighting] recibi progreso={value:F2} | brillo objetivo={TargetBrightness():F2} | brillo actual={current:F2}", this);
    }

    void Update()
    {
        float target = TargetBrightness();
        current = Mathf.MoveTowards(current, target, Time.deltaTime / Mathf.Max(0.01f, smoothSeconds));
        Apply(current);

        if (logValues && Mathf.Abs(target - lastLogged) > 0.001f)
        {
            lastLogged = target;
            Debug.Log($"[EnvironmentLighting] progreso={CurrentProgress():F2} | desempeno={CurrentPerformance():F2} | brillo objetivo={target:F2}", this);
        }
    }

    float CurrentProgress()
    {
        if (useManualProgress) return manualProgress;
        return progress != null ? progress.Progress01 : 0f;
    }

    float CurrentPerformance()
    {
        if (useManagerPerformance && NeuralNetworkManager.Instance != null)
            return NeuralNetworkManager.Instance.Performance01;
        return testPerformance01;
    }

    float TargetBrightness()
    {
        float maxBrightness = Mathf.Lerp(maxBrightnessBadPerformance, maxBrightnessGoodPerformance, CurrentPerformance());
        float top = Mathf.Max(baseBrightness, maxBrightness);
        return Mathf.Lerp(baseBrightness, top, Mathf.Clamp01(CurrentProgress()));
    }

    void Apply(float brightness)
    {
        float b = Mathf.Clamp01(brightness * brightnessMultiplier);

        switch (mode)
        {
            case Mode.CameraAlpha:
                video.targetCameraAlpha = b;
                break;

            case Mode.TintMaterial:
                surface.GetPropertyBlock(block);
                block.SetColor(colorId, Color.Lerp(dimTint, fullTint, b) * b);
                surface.SetPropertyBlock(block);
                break;
        }

        if (directionalLight != null)
            directionalLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, b);
    }
}