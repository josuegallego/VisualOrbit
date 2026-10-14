using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Logica del memorama: cuando se voltean dos cartas
///   - si son pareja    -> se resaltan en VERDE y quedan descubiertas (bloqueadas);
///   - si NO son pareja -> se resaltan en ROJO y, tras una espera, el JUEGO se reinicia:
///                         todas las cartas vuelven boca abajo y se puede jugar de nuevo.
///
/// Solo reinicia el memorama: NO recarga escenas ni toca las barras (PlayerStats).
///
/// USO:
///   1. Crea un objeto vacio "MemoramaManager" y agrega este script.
///   2. En "Cartas" arrastra todas las Carta_XX (o dejalo vacio: las busca solas en la escena).
///   3. Define las parejas, con UNA de estas dos formas:
///        - Dos cartas con el MISMO sprite en el Frente son pareja (no hay que configurar nada), o
///        - Ponles el mismo "Id Pareja" en CartaVoltear (0, 0, 1, 1, 2, 2...).
///
/// Requiere CartaVoltear (eventos InicioGiro / Volteada, Resaltar, SetInteractuable).
/// </summary>
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
    [Tooltip("Pausa corta antes de mostrar verde/rojo, para que se vea la segunda carta ya volteada.")]
    [Min(0f)] public float esperaAntesDeResaltar = 0.15f;
    [Tooltip("Cuanto tiempo se ve el rojo antes de reiniciar el juego.")]
    [Min(0f)] public float esperaAntesDeReiniciar = 1.8f;

    [Header("Reinicio del juego")]
    [Tooltip("Al reiniciar, cambia de lugar los simbolos entre las cartas (mientras estan boca abajo) " +
             "para que no sea siempre el mismo orden.")]
    public bool barajarAlReiniciar = false;

    [Header("Eventos (sonidos, efectos, etc.)")]
    public UnityEvent alAcertar;
    public UnityEvent alFallar;
    [Tooltip("Se dispara cuando el juego ya se reinicio y las cartas vuelven a ser jugables.")]
    public UnityEvent alReiniciar;
    [Tooltip("Se dispara cuando se encuentran todas las parejas.")]
    public UnityEvent alGanar;

    readonly List<CartaVoltear> abiertas = new List<CartaVoltear>();      // volteadas y ya terminadas, aun sin evaluar
    readonly HashSet<CartaVoltear> encontradas = new HashSet<CartaVoltear>();
    int seleccionadas;      // cartas que el jugador ya eligio en este intento (incluye las que aun giran)
    bool bloqueado;         // true mientras se evalua, se reinicia o se gano: no se acepta nada

    // ------------------------------------------------------------------ ciclo de vida

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
    }

    // ------------------------------------------------------------------ eventos de las cartas

    // Una carta EMPIEZA a voltearse. Si ya van dos, se bloquea todo de inmediato
    // (asi nadie puede tocar una tercera mientras la segunda aun esta girando).
    void AlIniciarGiro(CartaVoltear carta, bool haciaFrente)
    {
        if (bloqueado || !haciaFrente) return;

        seleccionadas++;
        if (seleccionadas >= 2) BloquearTodas();
    }

    // Una carta TERMINO de voltearse. Con dos abiertas se evalua la pareja.
    void AlTerminarGiro(CartaVoltear carta, bool deFrente)
    {
        if (bloqueado || !deFrente) return;

        abiertas.Add(carta);
        if (abiertas.Count == 2)
            StartCoroutine(Evaluar(abiertas[0], abiertas[1]));
    }

    // ------------------------------------------------------------------ logica

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

            if (encontradas.Count >= cartas.Count)
            {
                alGanar?.Invoke();              // queda bloqueado: el juego termino
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

    /// <summary>
    /// Reinicia SOLO el memorama: quita los resaltados, voltea todas las cartas boca abajo,
    /// (opcional) baraja los simbolos y vuelve a permitir jugar. Se puede llamar desde un boton.
    /// </summary>
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

        // Todas boca abajo (esto tambien les quita el verde / rojo)
        foreach (var c in cartas)
            if (c != null) c.MostrarDorso();

        yield return null;
        while (HayCartasGirando()) yield return null;

        if (barajarAlReiniciar) BarajarSimbolos();      // se hace con todas ocultas: no se ve el cambio

        ReiniciarEstado();
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

    // ------------------------------------------------------------------ parejas

    bool SonPareja(CartaVoltear a, CartaVoltear b)
    {
        // 1) Por Id Pareja, si las dos lo tienen
        if (a.idPareja >= 0 && b.idPareja >= 0)
            return a.idPareja == b.idPareja;

        // 2) Si no, por el mismo sprite en el Frente
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

    /// <summary>Mezcla los simbolos (sprite + Id Pareja) entre las cartas. Las cartas no se mueven.</summary>
    void BarajarSimbolos()
    {
        var simbolos = new List<(Sprite sprite, int id)>();
        foreach (var c in cartas)
            simbolos.Add((SimboloDe(c), c.idPareja));

        for (int i = simbolos.Count - 1; i > 0; i--)    // Fisher-Yates
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

    // ------------------------------------------------------------------ bloqueo

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
}