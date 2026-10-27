using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class MemoramaManager : MonoBehaviour
{
    [Header("Cartas")]
    public List<CartaVoltear> cartas = new List<CartaVoltear>();

    [Header("Resaltado")]
    public Color colorAcierto = new Color(0.20f, 0.85f, 0.30f);
    public Color colorError = new Color(0.90f, 0.20f, 0.20f);
    [Range(0f, 1f)] public float intensidadResalte = 0.65f;

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
        cartas.RemoveAll(c => c == null);
        if (cartas.Count == 0)
            cartas.AddRange(FindObjectsByType<CartaVoltear>(FindObjectsSortMode.None));
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
        // EL TIEMPO YA NO SE PAUSA CON BLOQUEADO
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

        // Reiniciar todo: voltear cartas, tiempo a 50s, porcentaje a 0%
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
            contadorFallos = 0; // Si aciertan, resetea los fallos

            a.Resaltar(colorAcierto, intensidadResalte);
            b.Resaltar(colorAcierto, intensidadResalte);
            encontradas.Add(a);
            encontradas.Add(b);
            alAcertar?.Invoke();

            ActualizarUI();
            AplicarImpactoBarras();

            if (encontradas.Count >= cartas.Count)
            {
                temporizadorActivo = false; // SE DETIENE AL GANAR
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

            contadorFallos++;
            Debug.Log($"[Memorama] Fallos: {contadorFallos} / {fallosParaReiniciar}");

            yield return new WaitForSecondsRealtime(esperaAntesDeReiniciar);

            // Si llegamos al límite de fallos, volteamos las cartas PERO NO RESETEAMOS EL TIEMPO
            if (contadorFallos >= fallosParaReiniciar)
            {
                Debug.Log("[Memorama] Límite de fallos alcanzado. Volteando todas las cartas (el tiempo sigue).");

                foreach (var c in cartas)
                    if (c != null) c.MostrarDorso();

                yield return null;
                while (HayCartasGirando()) yield return null;

                ReiniciarEstado(); // Resetea fallos, porcentaje y bloqueos
                bloqueado = false;
                DesbloquearRestantes();
                alReiniciar?.Invoke();
            }
            else
            {
                // Solo volteamos las dos cartas que fallamos
                a.MostrarDorso();
                b.MostrarDorso();
                bloqueado = false;
                DesbloquearRestantes();
                alReiniciar?.Invoke();
            }
        }
    }

    // Este método SOLO se llama al perder por tiempo. Reinicia el tiempo.
    IEnumerator ReiniciarJuegoCompleto()
    {
        bloqueado = true;
        BloquearTodas();

        foreach (var c in cartas)
            if (c != null) c.MostrarDorso();

        yield return null;
        while (HayCartasGirando()) yield return null;

        ReiniciarEstado();
        IniciarTemporizador(); // Aquí SÍ se resetea el tiempo

        bloqueado = false;
        DesbloquearRestantes();

        alReiniciar?.Invoke();
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        bloqueado = true;
        BloquearTodas();
        StartCoroutine(ReiniciarJuegoCompleto());
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

    void AplicarImpactoBarras()
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.ApplyEffects(5f, -5f, 10f);
        PlayerStats.Instance.ForzarActualizacion();
    }
}