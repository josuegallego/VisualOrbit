using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animacion de entrada/salida para paneles informativos (login, barras, "Ten en cuenta que...", etc.).
/// Ponlo en el objeto raiz del panel (el que se activa/desactiva). Funciona con:
///   - un panel SpriteRenderer (como "Cuadro") y/o hijos con SpriteRenderer,
///   - paneles de UI (RectTransform): usa un CanvasGroup (lo crea solo).
///
/// ENTRADA (cada vez que el panel se activa): aparece con fundido + "pop" suave + pequena subida.
/// ELEMENTOS ESCALONADOS: arrastra a la lista botones, barras, tarjetas... y entran uno tras otro.
/// SALIDA: llama a SalirYDesactivar() desde un boton (OnClick) si quieres que se desvanezca antes de apagarse.
/// </summary>
[DisallowMultipleComponent]
public class PanelAnimado : MonoBehaviour
{
    [Header("Entrada del panel")]
    public float duracion = 0.55f;
    [Range(0.5f, 1f)] public float escalaInicial = 0.88f;
    [Tooltip("Cuanto sube mientras aparece (unidades locales; en panel de mundo ~0.1, en UI ~40). 0 = sin subida.")]
    public float subida = 0f;
    [Tooltip("Pausa antes de empezar (segundos).")]
    public float retraso = 0f;

    [Header("Elementos que entran uno tras otro")]
    [Tooltip("Botones, barras, tarjetas... Se muestran en este orden despues del panel.")]
    public GameObject[] elementos;
    public float pausaEntreElementos = 0.18f;
    public float duracionElemento = 0.4f;

    [Header("Salida")]
    public float duracionSalida = 0.3f;

    [Header("Respiracion (opcional)")]
    [Tooltip("Cambio de escala continuo. En VR cuesta mas leer el texto si se mueve: dejalo en 0 salvo que lo quieras.")]
    [Range(0f, 0.03f)] public float respiracion = 0f;
    public float velocidadRespiracion = 1.2f;

    // ---- fundido generico sobre renderers y canvas groups ----
    class Fundido
    {
        readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        readonly List<Color> colores = new List<Color>();
        CanvasGroup grupo;

        public static Fundido Crear(Transform raiz, HashSet<Transform> excluir, bool incluirHijos)
        {
            var f = new Fundido();
            var srs = incluirHijos ? raiz.GetComponentsInChildren<SpriteRenderer>(true) : raiz.GetComponents<SpriteRenderer>();
            foreach (var s in srs)
            {
                if (excluir != null && Dentro(s.transform, excluir)) continue;
                f.sprites.Add(s); f.colores.Add(s.color);
            }
            if (raiz is RectTransform)
            {
                f.grupo = raiz.GetComponent<CanvasGroup>();
                if (f.grupo == null) f.grupo = raiz.gameObject.AddComponent<CanvasGroup>();
            }
            return f;
        }

        static bool Dentro(Transform t, HashSet<Transform> raices)
        {
            for (; t != null; t = t.parent) if (raices.Contains(t)) return true;
            return false;
        }

        public void Poner(float a)
        {
            a = Mathf.Clamp01(a);
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null) continue;
                var c = colores[i]; c.a *= a; sprites[i].color = c;
            }
            if (grupo != null) grupo.alpha = a;
        }
    }

    static float EaseOutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1f; t -= 1f; return 1f + c3 * t * t * t + c1 * t * t; }
    static float EaseOut(float t) { t = 1f - t; return 1f - t * t * t; }

    Fundido fPanel;
    readonly List<Fundido> fElementos = new List<Fundido>();
    readonly List<Vector3> escElementos = new List<Vector3>();
    Vector3 escBase, posBase;
    Coroutine rutina, idle;
    bool saliendo;

    void OnEnable()
    {
        saliendo = false;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Entrada());
    }

    void OnDisable()
    {
        // deja todo en su estado normal para la proxima vez
        if (fPanel != null) fPanel.Poner(1f);
        foreach (var f in fElementos) f.Poner(1f);
        if (escBase != Vector3.zero) { transform.localScale = escBase; transform.localPosition = posBase; }
        for (int i = 0; i < elementos.Length && i < escElementos.Count; i++)
            if (elementos[i] != null) elementos[i].transform.localScale = escElementos[i];
    }

    void Preparar()
    {
        var excl = new HashSet<Transform>();
        if (elementos != null) foreach (var e in elementos) if (e != null) excl.Add(e.transform);
        fPanel = Fundido.Crear(transform, excl, true);

        fElementos.Clear(); escElementos.Clear();
        if (elementos != null)
            foreach (var e in elementos)
            {
                if (e == null) { fElementos.Add(null); escElementos.Add(Vector3.one); continue; }
                fElementos.Add(Fundido.Crear(e.transform, null, true));
                escElementos.Add(e.transform.localScale);
            }
    }

    IEnumerator Entrada()
    {
        escBase = transform.localScale; posBase = transform.localPosition;
        Preparar();

        fPanel.Poner(0f);
        transform.localScale = escBase * escalaInicial;
        transform.localPosition = posBase - new Vector3(0f, subida, 0f);
        for (int i = 0; i < fElementos.Count; i++)
        {
            if (fElementos[i] == null) continue;
            fElementos[i].Poner(0f);
            elementos[i].transform.localScale = escElementos[i] * 0.6f;
        }

        if (retraso > 0f) yield return new WaitForSecondsRealtime(retraso);

        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / duracion);
            fPanel.Poner(EaseOut(k));
            transform.localScale = escBase * Mathf.LerpUnclamped(escalaInicial, 1f, EaseOutBack(k));
            transform.localPosition = posBase - new Vector3(0f, subida * (1f - EaseOut(k)), 0f);
            yield return null;
        }
        fPanel.Poner(1f); transform.localScale = escBase; transform.localPosition = posBase;

        for (int i = 0; i < fElementos.Count; i++)
        {
            if (fElementos[i] != null) StartCoroutine(PopElemento(i));
            yield return new WaitForSecondsRealtime(pausaEntreElementos);
        }

        if (respiracion > 0f) idle = StartCoroutine(Respirar());
        rutina = null;
    }

    IEnumerator PopElemento(int i)
    {
        var tr = elementos[i].transform; var f = fElementos[i]; Vector3 e = escElementos[i];
        for (float t = 0f; t < duracionElemento; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / duracionElemento);
            f.Poner(EaseOut(k));
            tr.localScale = e * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(k));
            yield return null;
        }
        f.Poner(1f); tr.localScale = e;
    }

    IEnumerator Respirar()
    {
        float t0 = 0f;
        while (!saliendo)
        {
            t0 += Time.unscaledDeltaTime;
            transform.localScale = escBase * (1f + respiracion * Mathf.Sin(t0 * velocidadRespiracion));
            yield return null;
        }
    }

    /// <summary>Desvanece el panel y luego lo desactiva. Puedes llamarlo desde un boton.</summary>
    public void SalirYDesactivar() { SalirYHacer(null); }

    public void SalirYHacer(Action alTerminar)
    {
        if (!isActiveAndEnabled || saliendo) { alTerminar?.Invoke(); return; }
        StartCoroutine(Salida(alTerminar));
    }

    IEnumerator Salida(Action alTerminar)
    {
        saliendo = true;
        if (rutina != null) StopCoroutine(rutina);
        if (idle != null) StopCoroutine(idle);
        Vector3 desde = transform.localScale;
        for (float t = 0f; t < duracionSalida; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / duracionSalida);
            fPanel.Poner(1f - k);
            foreach (var f in fElementos) if (f != null) f.Poner(1f - k);
            transform.localScale = desde * Mathf.Lerp(1f, 0.94f, k);
            yield return null;
        }
        alTerminar?.Invoke();
        gameObject.SetActive(false);   // OnDisable restaura el estado normal
    }
}
