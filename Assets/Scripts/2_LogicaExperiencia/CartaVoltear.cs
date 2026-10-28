using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Animacion de voltear una carta hecha con SPRITES (SpriteRenderer) o con UI (Image):
/// dorso -> frente con el simbolo.
///
/// ESTRUCTURA EN LA JERARQUIA:
///
///   Carta_01          <- este script (+ XR Simple Interactable). Escala (1,1,1).
///     |- Dorso        <- el "par" 1: tu Cuadro actual (SpriteRenderer boca abajo)
///     |- Frente       <- el "par" 2: el Cuadro con el simbolo (copia de Dorso con otro sprite)
///
/// Posicion y rotacion de la carta (por ejemplo la inclinacion en Y) van en Carta_01.
/// Dorso y Frente quedan con posicion y rotacion local en 0 y conservan su propia escala.
///
/// Gira la carta en Y de 0 a 180 grados; a los 90 (de canto) cambia Dorso por Frente.
/// El Frente se gira 180 en Y de forma automatica para que el simbolo no quede en espejo.
///
/// INTERACCION (el script evita que un mismo toque dispare dos veces):
///  - XR Interactable en esta carta (VR): usa selectEntered.
///  - Button en la carta (UI): usa su onClick.
///  - Si no hay ninguno: click/puntero (IPointerClickHandler), util para probar en el editor
///    (requiere EventSystem + Physics Raycaster en la camara).
///
/// El collider se crea solo a partir del sprite del Dorso si la carta no tiene uno.
/// </summary>
[DefaultExecutionOrder(-100)]   // corre antes que el XR Interactable para que vea el collider
[DisallowMultipleComponent]
public class CartaVoltear : MonoBehaviour, IPointerClickHandler
{
    [Header("Las dos caras (el par de cartas)")]
    [Tooltip("Carta boca abajo. Visible al inicio.")]
    public GameObject dorso;
    [Tooltip("Carta con el simbolo. Se oculta sola al inicio.")]
    public GameObject frente;

    [Tooltip("Objeto que gira. Vacio = este mismo objeto.")]
    public Transform pivote;

    [Header("Pareja (para el memorama)")]
    [Tooltip("Dos cartas con el mismo numero son pareja. -1 = el MemoramaManager compara " +
             "el sprite del Frente (mismo simbolo = pareja).")]
    public int idPareja = -1;

    [Header("Animacion")]
    [Min(0.05f)] public float duracion = 0.45f;
    public AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Escala que alcanza a la mitad del giro (1 = sin efecto, 1.1 = crece 10%).")]
    [Min(0.1f)] public float escalaPico = 1.08f;

    [Tooltip("Desplazamiento local que alcanza a la mitad del giro (efecto de levantarse). " +
             "(0,0,0) = sin efecto. Prueba con un valor pequeno en Z, ej. (0,0,-0.05).")]
    public Vector3 desplazamientoPico = Vector3.zero;

    [Header("Interaccion")]
    [Tooltip("Boton UI de la carta (opcional). Vacio = se busca en este objeto y sus hijos.")]
    public Button boton;
    [Tooltip("Crear un BoxCollider ajustado al sprite del Dorso si la carta no tiene collider.")]
    public bool crearColliderAutomatico = true;
    [Tooltip("Si es false, el jugador no puede voltearla (ej. pareja ya encontrada). " +
             "Voltear()/MostrarFrente()/MostrarDorso() desde codigo siempre funcionan.")]
    public bool interactuable = true;
    [Tooltip("Si es true, un nuevo toque oculta la carta cuando ya esta de frente.")]
    public bool permitirOcultarConToque = false;
    [Tooltip("Ignorar toques mientras la carta esta girando (evita el doble toque).")]
    public bool ignorarMientrasAnima = true;

    [Header("Eventos")]
    public UnityEvent alMostrarFrente;
    public UnityEvent alMostrarDorso;

    /// <summary>Se dispara cuando EMPIEZA un giro (antes de animar). Parametros: la carta y hacia donde va (true = frente).</summary>
    public event Action<CartaVoltear, bool> InicioGiro;

    /// <summary>Se dispara al terminar cada giro. Parametros: la carta y si quedo de frente.</summary>
    public event Action<CartaVoltear, bool> Volteada;

    public bool EstaDeFrente { get; private set; }
    public bool EstaAnimando { get; private set; }

    Quaternion rotBase;
    Vector3 escalaBase;
    Vector3 ultimoOffset;               // offset ya aplicado (se suma como diferencia)
    Vector3 centroLocal;                // centro visual de la carta, en el espacio local del pivote
    float anguloActual;                 // 0 = dorso, 180 = frente
    Coroutine rutina;
    XRBaseInteractable interactable;
    bool? frenteVisible;                // cache para no llamar SetActive cada frame
    readonly List<(Component comp, Color original)> coloresOriginales = new List<(Component, Color)>();

    Transform P => pivote != null ? pivote : transform;

    // ------------------------------------------------------------------ ciclo de vida

    void Reset()
    {
        boton = GetComponentInChildren<Button>();
    }

    void Awake()
    {
        var t = P;
        rotBase = t.localRotation;
        escalaBase = t.localScale;

        if (boton == null) boton = GetComponentInChildren<Button>();

        CalcularCentro();

        // El frente se gira 180 en Y para que, al terminar el giro de 180 de la carta,
        // el simbolo quede derecho y no en espejo.
        if (frente != null)
            frente.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * frente.transform.localRotation;

        if (crearColliderAutomatico && GetComponent<Collider>() == null)
            AjustarCollider();

        Aplicar(0f);
    }

    void OnEnable()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null) interactable.selectEntered.AddListener(AlSeleccionar);

        if (boton != null) boton.onClick.AddListener(IntentarVoltearPorJugador);

        // Si el objeto se desactivo a mitad de animacion, queda en el estado objetivo.
        anguloActual = EstaDeFrente ? 180f : 0f;
        Aplicar(anguloActual);
    }

    void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(AlSeleccionar);
            interactable = null;
        }
        if (boton != null) boton.onClick.RemoveListener(IntentarVoltearPorJugador);

        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        EstaAnimando = false;
        anguloActual = EstaDeFrente ? 180f : 0f;
    }

    // ------------------------------------------------------------------ entrada

    void AlSeleccionar(SelectEnterEventArgs args) => IntentarVoltearPorJugador();

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si hay XR Interactable o Button, ellos ya se encargan (evita voltear dos veces)
        if (interactable != null || boton != null) return;
        IntentarVoltearPorJugador();
    }

    void IntentarVoltearPorJugador()
    {
        if (!interactuable) return;
        if (ignorarMientrasAnima && EstaAnimando) return;
        if (EstaDeFrente && !permitirOcultarConToque) return;
        Voltear();
    }

    // ------------------------------------------------------------------ API publica

    /// <summary>Alterna entre dorso y frente.</summary>
    public void Voltear() => IrA(!EstaDeFrente, false);

    /// <summary>Muestra el simbolo (carta de frente).</summary>
    public void MostrarFrente(bool instantaneo = false) => IrA(true, instantaneo);

    /// <summary>Vuelve a mostrar el dorso (carta boca abajo).</summary>
    public void MostrarDorso(bool instantaneo = false) => IrA(false, instantaneo);

    /// <summary>Para que un manager bloquee/desbloquee la carta (ej. pareja encontrada).</summary>
    public void SetInteractuable(bool valor)
    {
        interactuable = valor;
        if (boton != null) boton.interactable = valor;
    }

    // ------------------------------------------------------------------ resaltado

    /// <summary>
    /// Tine la cara del simbolo (Frente) del color dado. intensidad 0 = sin cambio, 1 = color puro.
    /// Funciona con SpriteRenderer y con Image/Graphic de UI.
    /// </summary>
    public void Resaltar(Color color, float intensidad = 0.65f)
    {
        QuitarResalte();
        if (frente == null)
        {
            Debug.LogWarning($"[CartaVoltear] {name}: no hay Frente asignado.", this);
            return;
        }

        int tintados = 0;

        foreach (var sr in frente.GetComponentsInChildren<SpriteRenderer>(true))
        {
            coloresOriginales.Add((sr, sr.color));
            sr.color = Mezclar(sr.color, color, intensidad);
            tintados++;
        }
        foreach (var g in frente.GetComponentsInChildren<Graphic>(true))
        {
            coloresOriginales.Add((g, g.color));
            g.color = Mezclar(g.color, color, intensidad);
            tintados++;
        }

        if (tintados == 0)
            Debug.LogWarning($"[CartaVoltear] {name}: el Frente no tiene SpriteRenderer ni Image, no hay nada que tinte.", this);
    }

    static Color Mezclar(Color original, Color tinte, float k)
    {
        var c = Color.Lerp(original, tinte, k);
        c.a = original.a;      // conserva la transparencia original
        return c;
    }

    /// <summary>Devuelve los colores originales de la cara.</summary>
    public void QuitarResalte()
    {
        foreach (var (comp, original) in coloresOriginales)
        {
            if (comp == null) continue;
            if (comp is SpriteRenderer sr) sr.color = original;
            else if (comp is Graphic g) g.color = original;
        }
        coloresOriginales.Clear();
    }

    // ------------------------------------------------------------------ animacion

    void IrA(bool deFrente, bool instantaneo)
    {
        bool cambio = EstaDeFrente != deFrente;
        EstaDeFrente = deFrente;
        float destino = deFrente ? 180f : 0f;

        if (!deFrente) QuitarResalte();
        if (cambio) InicioGiro?.Invoke(this, deFrente);

        if (rutina != null) { StopCoroutine(rutina); rutina = null; }

        if (instantaneo || !isActiveAndEnabled)
        {
            anguloActual = destino;
            Aplicar(destino);
            EstaAnimando = false;
            if (cambio) Notificar();
            return;
        }

        rutina = StartCoroutine(Animar(destino, cambio));
    }

    IEnumerator Animar(float destino, bool notificar)
    {
        EstaAnimando = true;

        float inicio = anguloActual;
        // Si se interrumpe a mitad de giro, solo recorre lo que le falta (sin saltos).
        float dur = duracion * (Mathf.Abs(destino - inicio) / 180f);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = curva.Evaluate(Mathf.Clamp01(t / dur));
            anguloActual = Mathf.LerpUnclamped(inicio, destino, k);
            Aplicar(anguloActual);
            yield return null;
        }

        anguloActual = destino;
        Aplicar(destino);
        EstaAnimando = false;
        rutina = null;

        if (notificar) Notificar();
    }

    void Aplicar(float angulo)
    {
        var t = P;
        float a = Mathf.Clamp(angulo, 0f, 180f);
        float pico = Mathf.Sin(a / 180f * Mathf.PI);     // 0 en los extremos, 1 a la mitad

        // Punto del mundo donde esta ahora el centro de la carta: no debe moverse al girar
        Vector3 centroMundo = t.TransformPoint(centroLocal);

        // rotBase conserva la inclinacion que ya tenga la carta; el giro es sobre su eje Y local
        t.localRotation = rotBase * Quaternion.Euler(0f, a, 0f);
        t.localScale = escalaBase * Mathf.Lerp(1f, escalaPico, pico);

        // Corrige la posicion para que gire EN EL MISMO SITIO (sobre su centro),
        // aunque el pivote del objeto o del sprite no este centrado
        t.position += centroMundo - t.TransformPoint(centroLocal);

        Vector3 offset = desplazamientoPico * pico;
        if (offset != ultimoOffset)
        {
            t.localPosition += offset - ultimoOffset;
            ultimoOffset = offset;
        }

        // Cambio de cara cuando la carta esta de canto
        bool mostrarFrente = a >= 90f;
        if (frenteVisible != mostrarFrente)
        {
            frenteVisible = mostrarFrente;
            if (frente != null) frente.SetActive(mostrarFrente);
            if (dorso != null) dorso.SetActive(!mostrarFrente);
        }
    }

    void Notificar()
    {
        if (EstaDeFrente) alMostrarFrente?.Invoke();
        else alMostrarDorso?.Invoke();

        Volteada?.Invoke(this, EstaDeFrente);
    }

    // ------------------------------------------------------------------ centro de giro

    /// <summary>
    /// Guarda el centro visual del sprite del Dorso en el espacio local del pivote.
    /// Sin SpriteRenderer (UI u otro), usa el origen del pivote.
    /// </summary>
    void CalcularCentro()
    {
        centroLocal = Vector3.zero;

        var sr = dorso != null ? dorso.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (sr != null && sr.sprite != null)
        {
            Vector3 centroMundo = sr.transform.TransformPoint(sr.sprite.bounds.center);
            centroLocal = P.InverseTransformPoint(centroMundo);
            return;
        }

        var rt = dorso != null ? dorso.GetComponent<RectTransform>() : null;
        if (rt != null)
        {
            Vector3 centroMundo = rt.TransformPoint(rt.rect.center);
            centroLocal = P.InverseTransformPoint(centroMundo);
        }
    }

    // ------------------------------------------------------------------ collider

    /// <summary>Crea un BoxCollider del tamano del sprite del Dorso, en el espacio de esta carta.</summary>
    void AjustarCollider()
    {
        var sr = dorso != null ? dorso.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (sr == null || sr.sprite == null)
        {
            Debug.LogWarning("[CartaVoltear] No se pudo crear el collider: el Dorso no tiene SpriteRenderer con sprite. " +
                             "Agrega un Collider manualmente.", this);
            return;
        }

        Vector3 ls = sr.transform.lossyScale;
        Vector3 ts = transform.lossyScale;
        if (Mathf.Approximately(ts.x, 0f) || Mathf.Approximately(ts.y, 0f)) return;

        Bounds b = sr.sprite.bounds;     // en unidades del sprite (sin escala del objeto)
        var box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(sr.transform.TransformPoint(b.center));
        box.size = new Vector3(
            Mathf.Abs(b.size.x * ls.x / ts.x),
            Mathf.Abs(b.size.y * ls.y / ts.y),
            0.01f);
    }
}