using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transición fade (a negro) entre escenas, incluido el fade-in al iniciar la app.
/// - Se crea solo al iniciar, no hace falta ponerlo en ninguna escena.
/// - Persiste entre escenas (DontDestroyOnLoad).
/// - Funciona en pantalla normal y en VR (Canvas ligado a la cámara principal).
/// - Uso:  SceneTransition.LoadScene("NombreEscena");
///         SceneTransition.LoadScene(2);
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [Header("Ajustes")]
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float holdBlackDuration = 0.2f; // tiempo extra en negro al entrar a una escena
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float planeDistance = 0.5f;     // distancia del canvas a la cámara

    private Canvas canvas;
    private Image fadeImage;
    private bool isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null) return;

        GameObject go = new GameObject("SceneTransition");
        instance = go.AddComponent<SceneTransition>();
        DontDestroyOnLoad(go);
        instance.BuildCanvas();
    }

    private void BuildCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // se cambia a Camera cuando haya cámara
        canvas.sortingOrder = 9999;
        canvas.planeDistance = planeDistance;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(transform, false);

        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f); // empieza en negro
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Fade-in de la primera escena
        StartCoroutine(FadeInAfterLoad());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToCamera();

        // Si el cambio no vino de LoadScene() igual se suaviza la entrada.
        if (!isTransitioning)
        {
            StopAllCoroutines();
            SetAlpha(1f);
            StartCoroutine(FadeInAfterLoad());
        }
    }

    // Liga el canvas a la cámara principal (necesario para que se vea en VR)
    private void AttachToCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = planeDistance;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    // ---------- API pública ----------

    public static void LoadScene(string sceneName)
    {
        if (instance == null) AutoCreate();
        if (instance.isTransitioning) return;
        instance.StartCoroutine(instance.Transition(() => SceneManager.LoadSceneAsync(sceneName)));
    }

    public static void LoadScene(int buildIndex)
    {
        if (instance == null) AutoCreate();
        if (instance.isTransitioning) return;
        instance.StartCoroutine(instance.Transition(() => SceneManager.LoadSceneAsync(buildIndex)));
    }

    // ---------- Lógica interna ----------

    private IEnumerator Transition(System.Func<AsyncOperation> loadOperation)
    {
        isTransitioning = true;
        fadeImage.raycastTarget = true;

        AttachToCamera();
        yield return Fade(0f, 1f, fadeOutDuration);

        AsyncOperation op = loadOperation();
        while (!op.isDone)
            yield return null;

        yield return FadeInAfterLoad();

        fadeImage.raycastTarget = false;
        isTransitioning = false;
    }

    // Mantiene negro unos frames (para que el pico de carga no se coma el fade) y luego aclara
    private IEnumerator FadeInAfterLoad()
    {
        SetAlpha(1f);

        yield return null;
        yield return null;
        AttachToCamera();

        if (holdBlackDuration > 0f)
            yield return new WaitForSecondsRealtime(holdBlackDuration);

        yield return Fade(1f, 0f, fadeInDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            // Tope por frame: si hay un tirón de carga, no se salta el fade
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            SetAlpha(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}