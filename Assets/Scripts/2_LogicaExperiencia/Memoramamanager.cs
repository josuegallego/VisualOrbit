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

    private List<CartaVoltear> cartas = new List<CartaVoltear>();

    [Header("Indicadores Visuales de Ruta")]
    public GameObject indicadorRutaBuena;
    public GameObject indicadorRutaIntermedia;
    public GameObject indicadorRutaMala;

    [Header("Resaltado")]
    public Color colorAcierto = new Color(0.20f, 0.85f, 0.30f);
    public Color colorError = new Color(0.90f, 0.20f, 0.20f);
    [Range(0f, 1f)] public float intensidadResalte = 0.65f;

    [Header("Imagen de Acierto")]
    public GameObject imagenAcierto;
    public float duracionImagenAcierto = 5f;
    private Coroutine rutinaImagenAcierto;

    [Header("Imagen de Error")]
    public GameObject imagenIncorrecta;
    public float duracionImagenIncorrecta = 3f;
    private Coroutine rutinaImagenError;

    [Header("Transición de las imágenes")]
    [Min(0f)] public float duracionFundido = 0.3f;

    [Header("Sonidos")]
    public AudioSource fuenteAudio;
    public AudioClip sonidoAcierto;
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
            fuenteAudio.spatialBlend = 0f;
        }

        ConfigurarCartasPorRuta();
    }

    void ConfigurarCartasPorRuta()
    {
        RutaDificultad ruta = DeterminarRuta();
        int cartasAUsar = 12;

        if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(false);
        if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(false);
        if (indicadorRutaMala != null) indicadorRutaMala.SetActive(false);

        switch (ruta)
        {
            case RutaDificultad.Buena:
                cartasAUsar = 8;
                if (indicadorRutaBuena != null) indicadorRutaBuena.SetActive(true);
                break;
            case RutaDificultad.Intermedia:
                cartasAUsar = 10;
                if (indicadorRutaIntermedia != null) indicadorRutaIntermedia.SetActive(true);
                break;
            case RutaDificultad.Mala:
                cartasAUsar = 12;
                if (indicadorRutaMala != null) indicadorRutaMala.SetActive(true);
                break;
        }

        cartas.Clear();
        for (int i = 0; i < todasLasCartas.Count; i++)
        {
            if (todasLasCartas[i] == null) continue;

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

    [Header("Tiempo segun estado cognitivo")]
    public float umbralOptimo = 70f;
    public float umbralDeficiente = 39f;
    public float tiempoOptimo = 120f;
    public float tiempoMedio = 90f;
    public float tiempoDeficiente = 60f;
    public Color colorTiempo = Color.white;
    [Range(0f, 1f)] public float brilloTiempo = 0.9f;
    public bool forzarTimerBlanco = true;

    void QuitarEfectosDeColorAlTimer()
    {
        var pulse = textoTiempo.GetComponent<TextGlowPulse>();
        if (pulse != null) pulse.enabled = false;

        Material m = textoTiempo.fontMaterial;
        m.DisableKeyword("GLOW_ON");
        if (m.HasProperty("_FaceColor")) m.SetColor("_FaceColor", Color.white);
    }

    float EstadoCognitivoPorcentaje()
    {
        var s = PlayerStats.Instance;
        if (s == null) return 55f;
        return (s.energy + s.memory + (100f - s.interference)) / 3f;
    }

    float CalcularTiempoPorEstado()
    {
        float estado = EstadoCognitivoPorcentaje();
        float t;
        if (estado >= umbralOptimo) t = tiempoOptimo;
        else if (estado <= umbralDeficiente) t = tiempoDeficiente;
        else t = tiempoMedio;
        return t;
    }

    void IniciarTemporizador()
    {
        tiempoTotal = CalcularTiempoPorEstado();
        tiempoRestante = tiempoTotal;
        temporizadorActivo = true;

        if (barraTiempo != null) barraTiempo.fillAmount = 1f;
        if (textoTiempo != null)
        {
            if (forzarTimerBlanco) QuitarEfectosDeColorAlTimer();
            textoTiempo.color = new Color(colorTiempo.r * brilloTiempo, colorTiempo.g * brilloTiempo, colorTiempo.b * brilloTiempo, 1f);
            textoTiempo.text = $"{tiempoTotal:F0}s";
        }
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

        // GUARDAR TIEMPO Y ESTADO (FALLIDO) EN EL JSON
        if (PlayerDataStore.ActiveSession != null)
            PlayerDataStore.SetTiempoMemorama(tiempoTotal, false);

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

                // GUARDAR TIEMPO Y ESTADO (GANADO) EN EL JSON
                if (PlayerDataStore.ActiveSession != null)
                {
                    float tiempoJugado = tiempoTotal - tiempoRestante;
                    PlayerDataStore.SetTiempoMemorama(tiempoJugado, true);
                }

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

            yield return new WaitForSecondsRealtime(esperaAntesDeReiniciar);

            if (contadorFallos >= fallosParaReiniciar)
            {
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

    void MostrarImagenAcierto()
    {
        if (imagenAcierto == null) return;
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
        if (imagenIncorrecta == null) return;
        if (rutinaImagenError != null) StopCoroutine(rutinaImagenError);
        rutinaImagenError = StartCoroutine(ImagenIncorrectaRutina());
    }

    IEnumerator ImagenIncorrectaRutina()
    {
        yield return MostrarConFundido(imagenIncorrecta, duracionImagenIncorrecta);
        rutinaImagenError = null;
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null || fuenteAudio == null) return;
        fuenteAudio.PlayOneShot(clip, volumenSonidos);
    }

    readonly Dictionary<SpriteRenderer, float> alfaBase = new Dictionary<SpriteRenderer, float>();

    IEnumerator MostrarConFundido(GameObject objeto, float visible)
    {
        var grupo = objeto.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = objeto.AddComponent<CanvasGroup>();

        var sprites = objeto.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprites)
            if (!alfaBase.ContainsKey(sr)) alfaBase[sr] = sr.color.a;

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

    [Header("Impacto en las barras (HU memorama)")]
    [Tooltip("Cambio TOTAL que recibe cada barra si el jugador completa todas las parejas. " +
             "Se reparte entre las parejas y NUNCA se pasa de este total, aunque el juego se reinicie.")]
    public float impactoTotalEnergia = 5f;
    public float impactoTotalInterferencia = -5f;
    public float impactoTotalMemoria = 10f;

    float fraccionImpactoAplicada = 0f;

    void AplicarImpactoBarras()
    {
        if (PlayerStats.Instance == null) return;

        // Antes se sumaban +5/-5/+10 POR CADA pareja (con 9 parejas: +45/-45/+90) y se pisaban
        // los resultados de las decisiones. Ahora el total es fijo y se reparte entre las parejas.
        float totalParejas = Mathf.Max(1f, cartas.Count / 2f);
        float fraccion = Mathf.Min(1f / totalParejas, 1f - fraccionImpactoAplicada);
        if (fraccion <= 0.0001f) return;
        fraccionImpactoAplicada += fraccion;

        PlayerStats.Instance.ApplyEffects(
            impactoTotalEnergia * fraccion,
            impactoTotalInterferencia * fraccion,
            impactoTotalMemoria * fraccion);
        PlayerStats.Instance.ForzarActualizacion();
    }
}