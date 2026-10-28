using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class MemoramaManager : MonoBehaviour
{
    public enum RutaDificultad { Buena, Intermedia, Mala }

    [Header("Cartas (Asígnalas en orden de pareja: 0 y 1, 2 y 3, 4 y 5...)")]
    public List<CartaVoltear> todasLasCartas = new List<CartaVoltear>();

    // Esta lista guardará SOLO las cartas que se van a usar en esta partida
    private List<CartaVoltear> cartas = new List<CartaVoltear>();

    [Header("Indicadores Visuales de Ruta")]
    [Tooltip("Imagen que se activa si el jugador está en la ruta buena (8 cartas)")]
    public GameObject indicadorRutaBuena;
    [Tooltip("Imagen que se activa si el jugador está en la ruta intermedia (10 cartas)")]
    public GameObject indicadorRutaIntermedia;
    [Tooltip("Imagen que se activa si el jugador está en la ruta mala (12 cartas)")]
    public GameObject indicadorRutaMala;

    [Header("Resaltado")]
    public Color colorAcierto = new Color(0.20f, 0.85f, 0.30f);
    public Color colorError = new Color(0.90f, 0.20f, 0.20f);
    [Range(0f, 1f)] public float intensidadResalte = 0.65f;

    [Header("Imagen de Acierto")]
    [Tooltip("Imagen (objeto de la escena) que se activa cuando el jugador acierta una pareja. Déjala desactivada al inicio.")]
    public GameObject imagenAcierto;
    [Tooltip("Segundos que permanece visible la imagen de acierto.")]
    public float duracionImagenAcierto = 5f;
    private Coroutine rutinaImagenAcierto;

    [Header("Imagen de Error")]
    [Tooltip("Imagen (objeto de la escena) que se activa cuando el jugador falla una pareja. Déjala desactivada al inicio.")]
    public GameObject imagenIncorrecta;
    [Tooltip("Segundos que permanece visible la imagen de error.")]
    public float duracionImagenIncorrecta = 3f;
    private Coroutine rutinaImagenError;

    [Header("Transición de las imágenes")]
    [Tooltip("Segundos que tarda en aparecer y desaparecer (fundido de opacidad). 0 = sin transición.")]
    [Min(0f)] public float duracionFundido = 0.3f;

    [Header("Sonidos")]
    [Tooltip("AudioSource que reproduce los sonidos. Vacío = se crea uno automáticamente en este objeto.")]
    public AudioSource fuenteAudio;
    [Tooltip("Sonido al acertar una pareja.")]
    public AudioClip sonidoAcierto;
    [Tooltip("Sonido al fallar una pareja.")]
    public AudioClip sonidoError;
    [Range(0f, 1f)] public float volumenSonidos = 1f;

    [Header("Tiempos (segundos)")]
    public float esperaAntesDeResaltar = 0.15f;
    public float esperaAntesDeReiniciar = 1.8f;

    [Header("UI del Porcentaje")]
    public TMP_Text textoPorcentaje;
    public Image barraLlenadoPorcentaje;

    [Header("Temporizador")]
    public float tiempoTotal = 50f;
    private float tiempoRestante;
    private bool temporizadorActivo = false;
    public TMP_Text textoTiempo;
    public Image barraTiempo;

    [Header("Mecánica de Fallos")]
    public int fallosParaReiniciar = 2;
    private int contadorFallos = 0;

    [Header("Eventos")]
    public UnityEvent alAcertar;
    public UnityEvent alFallar;
    public UnityEvent alReiniciar;
    public UnityEvent alGanar;
    public UnityEvent alPerderPorTiempo;

    readonly List<CartaVoltear> abiertas = new List<CartaVoltear>();
    readonly HashSet<CartaVoltear> encontradas = new HashSet<CartaVoltear>();
    int seleccionadas;
    bool bloqueado;

    void Awake()
    {
        if (todasLasCartas == null || todasLasCartas.Count == 0)
        {
            todasLasCartas = new List<CartaVoltear>();
            todasLasCartas.AddRange(FindObjectsByType<CartaVoltear>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        if (fuenteAudio == null)
        {
            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.spatialBlend = 0f;   // 2D: se oye igual desde cualquier posición
        }

        ConfigurarCartasPorRuta();
    }

    void ConfigurarCartasPorRuta()
    {
        RutaDificultad ruta = DeterminarRuta();

        int cartasAUsar = 12;

        // Apagar TODOS los indicadores primero
        if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(false);
        if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(false);
        if (indicadorRutaMala != null) indicadorRutaMala.SetActive(false);

        switch (ruta)
        {
            case RutaDificultad.Buena:
                cartasAUsar = 8;
                if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(true);
                Debug.Log("<color=green>[Memorama] Ruta BUENA detectada. Se usarán 8 cartas (4 parejas).</color>");
                break;
            case RutaDificultad.Intermedia:
                cartasAUsar = 10;
                if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(true);
                Debug.Log("<color=yellow>[Memorama] Ruta INTERMEDIA detectada. Se usarán 10 cartas (5 parejas).</color>");
                break;
            case RutaDificultad.Mala:
                cartasAUsar = 12;
                if (indicadorRutaMala != null) indicadorRutaMala.SetActive(true);
                Debug.Log("<color=red>[Memorama] Ruta MALA detectada. Se usarán 12 cartas (6 parejas).</color>");
                break;
        }

        // ====================================================================
        // ACTIVACIÓN SECUENCIAL POR PAREJAS
        // ====================================================================
        cartas.Clear();

        for (int i = 0; i < todasLasCartas.Count; i++)
        {
            if (todasLasCartas[i] == null) continue;

            // Como están en orden, las primeras 'cartasAUsar' son las que van
            if (i < cartasAUsar)
            {
                todasLasCartas[i].gameObject.SetActive(true);
                cartas.Add(todasLasCartas[i]);
            }
            else
            {
                todasLasCartas[i].gameObject.SetActive(false);
            }
        }

        Debug.Log($"[Memorama] Total cartas activas para esta ruta: {cartas.Count}");
    }

    RutaDificultad DeterminarRuta()
    {
        var s = PlayerStats.Instance;
        if (s == null) return RutaDificultad.Intermedia;

        bool buena = s.energy > 70f && s.interference < 30f && s.memory > 70f;
        if (buena) return RutaDificultad.Buena;

        bool mala = s.energy < 40f || s.interference > 60f || s.memory < 40f;
        if (mala) return RutaDificultad.Mala;

        return RutaDificultad.Intermedia;
    }

    void OnEnable()
    {
        ReiniciarEstado();
        foreach (var c in cartas)
        {
            c.InicioGiro += AlIniciarGiro;
            c.Volteada += AlTerminarGiro;
        }
        ActualizarUI();
        IniciarTemporizador();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        OcultarImagenes();
        temporizadorActivo = false;
        foreach (var c in cartas)
        {
            if (c == null) continue;
            c.InicioGiro -= AlIniciarGiro;
            c.Volteada -= AlTerminarGiro;
        }
    }

    void ReiniciarEstado()
    {
        abiertas.Clear();
        encontradas.Clear();
        seleccionadas = 0;
        bloqueado = false;
        contadorFallos = 0;
        OcultarImagenes();
        ActualizarUI();
    }

    void IniciarTemporizador()
    {
        tiempoRestante = tiempoTotal;
        temporizadorActivo = true;
        if (barraTiempo != null) barraTiempo.fillAmount = 1f;
        if (textoTiempo != null) textoTiempo.text = $"{tiempoTotal:F0}s";
    }

    void Update()
    {
        if (!temporizadorActivo) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante < 0) tiempoRestante = 0;

        if (textoTiempo != null) textoTiempo.text = $"{tiempoRestante:F0}s";
        if (barraTiempo != null) barraTiempo.fillAmount = tiempoRestante / tiempoTotal;

        if (tiempoRestante <= 0)
        {
            temporizadorActivo = false;
            StartCoroutine(PerderPorTiempo());
        }
    }

    IEnumerator PerderPorTiempo()
    {
        bloqueado = true;
        BloquearTodas();
        alPerderPorTiempo?.Invoke();

        yield return new WaitForSecondsRealtime(1f);

        yield return ReiniciarJuegoCompleto();
    }

    void AlIniciarGiro(CartaVoltear carta, bool haciaFrente)
    {
        if (bloqueado || !haciaFrente) return;
        seleccionadas++;
        if (seleccionadas >= 2) BloquearTodas();
    }

    void AlTerminarGiro(CartaVoltear carta, bool deFrente)
    {
        if (bloqueado || !deFrente) return;
        abiertas.Add(carta);
        if (abiertas.Count == 2)
            StartCoroutine(Evaluar(abiertas[0], abiertas[1]));
    }

    IEnumerator Evaluar(CartaVoltear a, CartaVoltear b)
    {
        bloqueado = true;
        BloquearTodas();
        yield return new WaitForSecondsRealtime(esperaAntesDeResaltar);

        bool pareja = SonPareja(a, b);
        abiertas.Clear();
        seleccionadas = 0;

        if (pareja)
        {
            contadorFallos = 0;

            a.Resaltar(colorAcierto, intensidadResalte);
            b.Resaltar(colorAcierto, intensidadResalte);
            encontradas.Add(a);
            encontradas.Add(b);
            alAcertar?.Invoke();
            MostrarImagenAcierto();
            ReproducirSonido(sonidoAcierto);

            ActualizarUI();
            AplicarImpactoBarras();

            if (encontradas.Count >= cartas.Count)
            {
                temporizadorActivo = false;
                alGanar?.Invoke();
                yield break;
            }

            DesbloquearRestantes();
            bloqueado = false;
        }
        else
        {
            a.Resaltar(colorError, intensidadResalte);
            b.Resaltar(colorError, intensidadResalte);
            alFallar?.Invoke();
            MostrarImagenIncorrecta();
            ReproducirSonido(sonidoError);

            contadorFallos++;
            Debug.Log($"[Memorama] Fallos: {contadorFallos} / {fallosParaReiniciar}");

            yield return new WaitForSecondsRealtime(esperaAntesDeReiniciar);

            if (contadorFallos >= fallosParaReiniciar)
            {
                Debug.Log("[Memorama] Límite de fallos alcanzado. Volteando todas las cartas (el tiempo sigue).");

                foreach (var c in cartas)
                    if (c != null) c.MostrarDorso();

                yield return null;
                while (HayCartasGirando()) yield return null;

                ReiniciarEstado();
                bloqueado = false;
                DesbloquearRestantes();
                alReiniciar?.Invoke();
            }
            else
            {
                a.MostrarDorso();
                b.MostrarDorso();
                bloqueado = false;
                DesbloquearRestantes();
                alReiniciar?.Invoke();
            }
        }
    }

    IEnumerator ReiniciarJuegoCompleto()
    {
        bloqueado = true;
        BloquearTodas();

        foreach (var c in cartas)
            if (c != null) c.MostrarDorso();

        yield return null;
        while (HayCartasGirando()) yield return null;

        ReiniciarEstado();
        IniciarTemporizador();

        bloqueado = false;
        DesbloquearRestantes();

        alReiniciar?.Invoke();
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        OcultarImagenes();
        bloqueado = true;
        BloquearTodas();
        StartCoroutine(ReiniciarJuegoCompleto());
    }

    // ------------------------------------------------------------------ imágenes de acierto / error

    void MostrarImagenAcierto()
    {
        if (imagenAcierto == null)
        {
            Debug.LogWarning("[Memorama] 'Imagen Acierto' no está asignada en el Inspector del MemoramaManager.", this);
            return;
        }
        Debug.Log($"[Memorama] Mostrando imagen de acierto: {imagenAcierto.name}", imagenAcierto);
        if (rutinaImagenAcierto != null) StopCoroutine(rutinaImagenAcierto);
        rutinaImagenAcierto = StartCoroutine(ImagenAciertoRutina());
    }

    IEnumerator ImagenAciertoRutina()
    {
        yield return MostrarConFundido(imagenAcierto, duracionImagenAcierto);
        rutinaImagenAcierto = null;
    }

    void MostrarImagenIncorrecta()
    {
        if (imagenIncorrecta == null)
        {
            Debug.LogWarning("[Memorama] 'Imagen Incorrecta' no está asignada en el Inspector del MemoramaManager.", this);
            return;
        }
        Debug.Log($"[Memorama] Mostrando imagen de error: {imagenIncorrecta.name}", imagenIncorrecta);
        if (rutinaImagenError != null) StopCoroutine(rutinaImagenError);
        rutinaImagenError = StartCoroutine(ImagenIncorrectaRutina());
    }

    IEnumerator ImagenIncorrectaRutina()
    {
        yield return MostrarConFundido(imagenIncorrecta, duracionImagenIncorrecta);
        rutinaImagenError = null;
    }

    // Aparece con fundido, se queda 'visible' segundos y desaparece con fundido.
    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null || fuenteAudio == null) return;
        fuenteAudio.PlayOneShot(clip, volumenSonidos);
    }

    // Opacidad original de cada SpriteRenderer (se guarda la primera vez, para no ir perdiendo alpha)
    readonly Dictionary<SpriteRenderer, float> alfaBase = new Dictionary<SpriteRenderer, float>();

    IEnumerator MostrarConFundido(GameObject objeto, float visible)
    {
        // CanvasGroup: controla la opacidad de imágenes de UI (Image, TMP) y de todo lo que tengan dentro
        var grupo = objeto.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = objeto.AddComponent<CanvasGroup>();

        // SpriteRenderer: el CanvasGroup NO los afecta, así que se funde su color directamente
        var sprites = objeto.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprites)
            if (!alfaBase.ContainsKey(sr)) alfaBase[sr] = sr.color.a;

        Debug.Log($"[Memorama] Fundido de '{objeto.name}': {duracionFundido}s, SpriteRenderers: {sprites.Length}");

        AplicarAlfa(grupo, sprites, 0f);
        objeto.SetActive(true);

        yield return Fundir(grupo, sprites, 0f, 1f);
        yield return new WaitForSecondsRealtime(visible);
        yield return Fundir(grupo, sprites, 1f, 0f);

        objeto.SetActive(false);
    }

    void AplicarAlfa(CanvasGroup grupo, SpriteRenderer[] sprites, float a)
    {
        grupo.alpha = a;
        foreach (var sr in sprites)
        {
            if (sr == null) continue;
            var c = sr.color;
            c.a = alfaBase[sr] * a;
            sr.color = c;
        }
    }

    IEnumerator Fundir(CanvasGroup grupo, SpriteRenderer[] sprites, float desde, float hasta)
    {
        if (duracionFundido <= 0f)
        {
            AplicarAlfa(grupo, sprites, hasta);
            yield break;
        }

        float t = 0f;
        while (t < duracionFundido)
        {
            t += Time.unscaledDeltaTime;
            AplicarAlfa(grupo, sprites, Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        AplicarAlfa(grupo, sprites, hasta);
    }

    void OcultarImagenes()
    {
        if (rutinaImagenAcierto != null) { StopCoroutine(rutinaImagenAcierto); rutinaImagenAcierto = null; }
        if (rutinaImagenError != null) { StopCoroutine(rutinaImagenError); rutinaImagenError = null; }
        if (imagenAcierto != null) imagenAcierto.SetActive(false);
        if (imagenIncorrecta != null) imagenIncorrecta.SetActive(false);
    }

    // ------------------------------------------------------------------ utilidades

    bool HayCartasGirando()
    {
        foreach (var c in cartas)
            if (c != null && c.EstaAnimando) return true;
        return false;
    }

    bool SonPareja(CartaVoltear a, CartaVoltear b)
    {
        if (a.idPareja >= 0 && b.idPareja >= 0)
            return a.idPareja == b.idPareja;

        Sprite sa = ObtenerSprite(a);
        Sprite sb = ObtenerSprite(b);
        return sa != null && sa == sb;
    }

    Sprite ObtenerSprite(CartaVoltear carta)
    {
        if (carta.frente == null) return null;
        var sr = carta.frente.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) return sr.sprite;
        var img = carta.frente.GetComponentInChildren<Image>(true);
        if (img != null) return img.sprite;
        return null;
    }

    void BloquearTodas()
    {
        foreach (var c in cartas)
            if (c != null) c.SetInteractuable(false);
    }

    void DesbloquearRestantes()
    {
        foreach (var c in cartas)
            if (c != null && !encontradas.Contains(c) && !c.EstaDeFrente)
                c.SetInteractuable(true);
    }

    void ActualizarUI()
    {
        if (cartas.Count == 0) return;
        float totalParejas = cartas.Count / 2f;
        float parejasEncontradas = encontradas.Count / 2f;
        float porcentajeObjetivo = (parejasEncontradas / totalParejas) * 100f;

        if (textoPorcentaje != null)
            textoPorcentaje.text = $"Parejas: {porcentajeObjetivo:F0}%";

        if (barraLlenadoPorcentaje != null)
        {
            StopCoroutine(nameof(AnimarBarra));
            StartCoroutine(AnimarBarra(porcentajeObjetivo / 100f));
        }
    }

    private IEnumerator AnimarBarra(float objetivoNormalizado)
    {
        float valorInicial = barraLlenadoPorcentaje.fillAmount;
        float tiempoTranscurrido = 0f;
        float duracion = 0.5f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            barraLlenadoPorcentaje.fillAmount = Mathf.Lerp(valorInicial, objetivoNormalizado, tiempoTranscurrido / duracion);
            yield return null;
        }
        barraLlenadoPorcentaje.fillAmount = objetivoNormalizado;
    }

    void AplicarImpactoBarras()
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.ApplyEffects(5f, -5f, 10f);
        PlayerStats.Instance.ForzarActualizacion();
    }
}