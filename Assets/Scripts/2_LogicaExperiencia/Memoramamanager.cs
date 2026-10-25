using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro; // NUEVO: Necesario para usar TextMeshPro

public class MemoramaManager : MonoBehaviour
{
    [Header("Cartas")]
    [Tooltip("Vacio = se buscan todas las CartaVoltear de la escena al iniciar.")]
    public List<CartaVoltear> cartas = new List<CartaVoltear>();

    [Header("Resaltado")]
    public Color colorAcierto = new Color(0.20f, 0.85f, 0.30f);
    public Color colorError = new Color(0.90f, 0.20f, 0.20f);
    [Range(0f, 1f)] public float intensidadResalte = 0.65f;

    [Header("Tiempos (segundos)")]
    [Min(0f)] public float esperaAntesDeResaltar = 0.15f;
    [Min(0f)] public float esperaAntesDeReiniciar = 1.8f;

    [Header("Reinicio del juego")]
    public bool barajarAlReiniciar = false;

    [Header("UI del Porcentaje (NUEVO)")]
    [Tooltip("Arrastra aquí el TextMeshPro donde se mostrará el porcentaje")]
    public TMP_Text textoPorcentaje;
    [Tooltip("Arrastra aquí la barra de llenado (Image tipo Filled) si quieres una barra visual")]
    public Image barraLlenadoPorcentaje;

    [Header("Eventos (sonidos, efectos, etc.)")]
    public UnityEvent alAcertar;
    public UnityEvent alFallar;
    public UnityEvent alReiniciar;
    public UnityEvent alGanar;

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
        ActualizarUI(); // NUEVO: Inicializar el texto en 0%
    }

    void OnDisable()
    {
        StopAllCoroutines();
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

        // NUEVO: Si se reinicia (porque perdiste o se ganó), actualizamos la UI
        ActualizarUI();
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
            a.Resaltar(colorAcierto, intensidadResalte);
            b.Resaltar(colorAcierto, intensidadResalte);
            encontradas.Add(a);
            encontradas.Add(b);
            alAcertar?.Invoke();

            // NUEVO: Actualizar UI y enviar el impacto a las barras
            ActualizarUI();
            AplicarImpactoBarras();

            if (encontradas.Count >= cartas.Count)
            {
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

            yield return new WaitForSecondsRealtime(esperaAntesDeReiniciar);
            yield return ReiniciarJuego();
        }
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        bloqueado = true;
        BloquearTodas();
        StartCoroutine(ReiniciarJuego());
    }

    IEnumerator ReiniciarJuego()
    {
        bloqueado = true;
        BloquearTodas();

        foreach (var c in cartas)
            if (c != null) c.MostrarDorso();

        yield return null;
        while (HayCartasGirando()) yield return null;

        if (barajarAlReiniciar) BarajarSimbolos();

        ReiniciarEstado(); // Aquí se actualiza la UI a 0% si perdiste
        bloqueado = false;
        foreach (var c in cartas)
            if (c != null) c.SetInteractuable(true);

        alReiniciar?.Invoke();
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
        Sprite sa = SimboloDe(a);
        Sprite sb = SimboloDe(b);
        return sa != null && sa == sb;
    }

    static Sprite SimboloDe(CartaVoltear carta)
    {
        if (carta.frente == null) return null;
        var sr = carta.frente.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) return sr.sprite;
        var img = carta.frente.GetComponentInChildren<Image>(true);
        if (img != null) return img.sprite;
        return null;
    }

    static void PonerSimbolo(CartaVoltear carta, Sprite sprite)
    {
        if (carta.frente == null) return;
        var sr = carta.frente.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) { sr.sprite = sprite; return; }
        var img = carta.frente.GetComponentInChildren<Image>(true);
        if (img != null) img.sprite = sprite;
    }

    void BarajarSimbolos()
    {
        var simbolos = new List<(Sprite sprite, int id)>();
        foreach (var c in cartas)
            simbolos.Add((SimboloDe(c), c.idPareja));

        for (int i = simbolos.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (simbolos[i], simbolos[j]) = (simbolos[j], simbolos[i]);
        }

        for (int i = 0; i < cartas.Count; i++)
        {
            PonerSimbolo(cartas[i], simbolos[i].sprite);
            cartas[i].idPareja = simbolos[i].id;
        }
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

    // ====================================================================
    // NUEVOS MÉTODOS PARA LA HU
    // ====================================================================

    /// <summary>
    /// Calcula el porcentaje de parejas encontradas y actualiza la UI.
    /// </summary>
        // Reemplaza tu ActualizarUI() actual por este:
    void ActualizarUI()
    {
        if (cartas.Count == 0) return;

        // Cada pareja son 2 cartas. Ejemplo: 8 cartas = 4 parejas.
        float totalParejas = cartas.Count / 2f;
        float parejasEncontradas = encontradas.Count / 2f;

        // Calculamos el porcentaje objetivo (de 0 a 100)
        float porcentajeObjetivo = (parejasEncontradas / totalParejas) * 100f;

        // Si el texto existe, lo actualizamos directo (el texto no necesita animación)
        if (textoPorcentaje != null)
        {
            textoPorcentaje.text = $"Parejas: {porcentajeObjetivo:F0}%";
        }

        // Si la barra visual existe, lanzamos la animación suave
        if (barraLlenadoPorcentaje != null)
        {
            // Detenemos cualquier animación anterior para evitar conflictos si se voltean cartas rápido
            StopCoroutine(nameof(AnimarBarra));
            StartCoroutine(AnimarBarra(porcentajeObjetivo / 100f));
        }
    }

    // NUEVO: Corrutina para llenar la barra suavemente
    private System.Collections.IEnumerator AnimarBarra(float objetivoNormalizado)
    {
        float valorInicial = barraLlenadoPorcentaje.fillAmount;
        float tiempoTranscurrido = 0f;
        float duracion = 0.5f; // Puedes cambiar esto si la quieres más rápida o lenta

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            // Mathf.Lerp interpola suavemente entre el valor inicial y el objetivo
            barraLlenadoPorcentaje.fillAmount = Mathf.Lerp(valorInicial, objetivoNormalizado, tiempoTranscurrido / duracion);
            yield return null;
        }

        // Nos aseguramos de que quede exacto en el objetivo al terminar
        barraLlenadoPorcentaje.fillAmount = objetivoNormalizado;
    }

    /// <summary>
    /// Envía una recompensa al sistema de barras (PlayerStats) cada vez que se encuentra una pareja.
    /// Esto cumple el requisito de conectar el minijuego con las decisiones alimenticias.
    /// </summary>
    void AplicarImpactoBarras()
    {
        if (PlayerStats.Instance == null) return;

        // Calculamos cuánto sube la energía y la memoria por cada pareja encontrada.
        // Ajusta estos valores según el balance que quieras en tu juego.
        float subidaEnergia = 5f;
        float bajadaInterferencia = 5f; // Bajar la interferencia es bueno
        float subidaMemoria = 10f;

        // Aplicamos los cambios a PlayerStats
        PlayerStats.Instance.ApplyEffects(subidaEnergia, -bajadaInterferencia, subidaMemoria);

        // Forzamos a que las barras visuales en la pantalla (StatsBarUI) se actualicen
        PlayerStats.Instance.ForzarActualizacion();
    }
}