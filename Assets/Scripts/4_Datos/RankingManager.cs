using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class RankingManager : MonoBehaviour
{
    [Header("Renglones ya colocados en la escena (de arriba hacia abajo)")]
    public List<RankingItem> filas;

    [Header("Opcional")]
    public GameObject textoVacio;

    [Header("Mensaje de posición")]
    public TMPro.TMP_Text textoPosicion; // un texto aparte, fuera de las filas

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorMejor = new Color(1f, 0.84f, 0f);       // dorado
    public Color colorReciente = new Color(0.3f, 0.9f, 1f);   // celeste: el jugador actual ("Tú")
    public Color colorAmbos = new Color(0.6f, 1f, 0.5f);      // mejor y jugador actual

    [Header("Jugador fuera del top")]
    [Tooltip("Si el jugador quedó más abajo de las filas disponibles, la última fila muestra su puesto real.")]
    public bool mostrarJugadorSiQuedoFuera = true;

    /// <summary>Sesión del jugador que acaba de terminar (la usa ResultadoJugador para el panel 'Tu resultado').</summary>
    public static PlayerSession SesionJugadorActual { get; private set; }

    PlayerSession sesionJugador;

    void Start()
    {
        // Guardamos la referencia antes de cerrarla
        sesionJugador = PlayerDataStore.ActiveSession;
        SesionJugadorActual = sesionJugador;
        PlayerDataStore.CloseActiveSessionIfOpen(true);
        MostrarRanking();
    }

    public void MostrarRanking()
    {
        List<PlayerSession> ranking = PlayerDataStore.GetRanking(soloCompletadas: false);

        if (textoVacio != null) textoVacio.SetActive(ranking.Count == 0);

        int posJugador = sesionJugador != null ? ranking.IndexOf(sesionJugador) + 1 : 0;

        if (textoPosicion != null)
        {
            if (posJugador > 0)
            {
                textoPosicion.gameObject.SetActive(true);
                textoPosicion.text = posJugador <= filas.Count
                    ? $"¡Quedaste en el puesto {posJugador}!"
                    : $"Quedaste en el puesto {posJugador} de {ranking.Count}";
            }
            else
            {
                textoPosicion.gameObject.SetActive(false); // entró al ranking sin haber jugado
            }
        }

        // Quién terminó más recientemente (solo se usa si no hay jugador actual)
        PlayerSession sesionReciente = null;
        DateTime masReciente = DateTime.MinValue;

        foreach (var s in ranking)
        {
            if (!s.experienciaCompletada || string.IsNullOrEmpty(s.fechaFin)) continue;

            if (DateTime.TryParseExact(s.fechaFin, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fin)
                && fin >= masReciente)
            {
                masReciente = fin;
                sesionReciente = s;
            }
        }

        PlayerSession resaltada = sesionJugador != null ? sesionJugador : sesionReciente;

        // ¿El jugador quedó fuera de las filas? Entonces la última fila es para él.
        bool jugadorFuera = mostrarJugadorSiQuedoFuera && posJugador > filas.Count && filas.Count > 0;

        for (int i = 0; i < filas.Count; i++)
        {
            int idx = i;
            if (jugadorFuera && i == filas.Count - 1) idx = posJugador - 1;

            if (idx < ranking.Count)
            {
                var s = ranking[idx];
                bool esJugador = sesionJugador != null && s == sesionJugador;

                filas[i].gameObject.SetActive(true);
                filas[i].Configurar(s, idx + 1, esJugador);

                bool esMejor = (idx == 0); // el ranking ya viene ordenado por puntaje
                bool esResaltada = (s == resaltada);

                Color c = colorNormal;
                if (esMejor && esResaltada) c = colorAmbos;
                else if (esMejor) c = colorMejor;
                else if (esResaltada) c = colorReciente;

                filas[i].AplicarColor(c);
            }
            else
            {
                filas[i].gameObject.SetActive(false);
            }
        }
    }
}
