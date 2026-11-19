using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mantiene el ANCHO del panel fijo aunque cambies el sprite por otro de distinto tamano en pixeles.
/// El alto se calcula solo con la proporcion del sprite nuevo, asi nunca se deforma.
/// Sirve para un SpriteRenderer (objeto en el mundo, como "Cuadro") o para una Image de UI.
///
/// COMO USARLO:
///  1) Con el sprite VIEJO puesto, agrega el componente: NO cambia nada y guarda el ancho actual.
///  2) Cambia el sprite: conserva ese ancho.
///  3) Para volver a capturar el tamano actual: marca "Capturar Ahora" (o clic derecho en el componente > "Capturar tamano actual").
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class SpriteTamanoFijo : MonoBehaviour
{
    [Tooltip("Ancho guardado (SpriteRenderer: unidades del mundo; Image: unidades del Rect Transform). 0 = aun no capturado.")]
    public float ancho = 0f;

    [Tooltip("Marcalo para guardar el tamano que tiene el panel AHORA MISMO (se desmarca solo).")]
    public bool capturarAhora = false;

    [Tooltip("Si esta apagado, el script no toca el tamano (util para ajustar a mano).")]
    public bool activo = true;

    SpriteRenderer sr;
    Image img;
    Sprite ultimo;
    float ultimoAncho = -1f;

    void Reset() { ancho = 0f; }

    void OnEnable() { Refs(); if (ancho <= 0f) Capturar(); ultimo = Actual(); ultimoAncho = ancho; }

    void OnValidate()
    {
        Refs();
        if (capturarAhora) { capturarAhora = false; Capturar(); }
        // al editar "Ancho" a mano si se aplica; no se toca nada antes de capturar
        if (ancho > 0f) { ultimo = null; }
    }

    void Update() { if (activo && ancho > 0f) Aplicar(); }

    void Refs()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (img == null) img = GetComponent<Image>();
    }

    Sprite Actual() => sr != null ? sr.sprite : (img != null ? img.sprite : null);

    [ContextMenu("Capturar tamano actual")]
    public void Capturar()
    {
        Refs();
        if (sr != null && sr.sprite != null) ancho = sr.bounds.size.x;
        else if (img != null) ancho = ((RectTransform)transform).rect.width;
        ultimo = Actual(); ultimoAncho = ancho;
    }

    void Aplicar()
    {
        Refs();
        Sprite s = Actual();
        if (s == null || s.rect.width <= 0f) return;
        if (s == ultimo && Mathf.Approximately(ancho, ultimoAncho)) return;

        // Primera vez con el sprite que ya tenia: no tocar nada (solo recordarlo)
        if (ultimo == null && Mathf.Approximately(ancho, ultimoAncho)) { ultimo = s; return; }
        ultimo = s; ultimoAncho = ancho;

        if (sr != null)
        {
            float w = s.bounds.size.x;                    // ancho del sprite sin escala
            Transform p = transform.parent;
            float padre = p != null ? Mathf.Abs(p.lossyScale.x) : 1f;
            if (w <= 0f || padre <= 0f) return;
            float k = ancho / (w * padre);
            Vector3 sc = transform.localScale;
            if (!Mathf.Approximately(sc.x, k) || !Mathf.Approximately(sc.y, k))
                transform.localScale = new Vector3(k, k, sc.z);
        }
        else if (img != null)
        {
            var rt = (RectTransform)transform;
            float alto = ancho * s.rect.height / s.rect.width;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ancho);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, alto);
        }
    }
}
