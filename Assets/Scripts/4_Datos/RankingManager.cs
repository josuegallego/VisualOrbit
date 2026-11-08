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
    public Color colorMejor = new Color(1f, 0.84f, 0f);    // dorado
    public Color colorReciente = new Color(0.3f, 0.9f, 1f);   // celeste
    public Color colorAmbos = new Color(0.6f, 1f, 0.5f);   // mejor y más reciente

    PlayerSession sesionJugador;

    void Start()
    {
        // Guardamos la referencia antes de cerrarla
        sesionJugador = PlayerDataStore.ActiveSession;
        PlayerDataStore.CloseActiveSessionIfOpen(true);
        MostrarRanking();
    }

    public void MostrarRanking()
    {
        List<PlayerSession> ranking = PlayerDataStore.GetRanking(soloCompletadas: false);

        if (textoVacio != null) textoVacio.SetActive(ranking.Count == 0);

        if (textoPosicion != null)
        {
            int pos = sesionJugador != null ? ranking.IndexOf(sesionJugador) + 1 : 0;

            if (pos > 0)
            {
                textoPosicion.gameObject.SetActive(true);
                textoPosicion.text = pos <= filas.Count
                    ? $"¡Quedaste en el puesto {pos}!"
                    : $"Quedaste en el puesto {pos} de {ranking.Count}";
            }
            else
            {
                textoPosicion.gameObject.SetActive(false); // entró al ranking sin haber jugado
            }
        }

        // Quién terminó más recientemente (según fechaFin) -> referencia a la sesión
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

        for (int i = 0; i < filas.Count; i++)
        {
            if (i < ranking.Count)
            {
                filas[i].gameObject.SetActive(true);
                filas[i].Configurar(ranking[i]);

                bool esMejor = (i == 0); // el ranking ya viene ordenado por puntaje
                bool esReciente = (ranking[i] == sesionReciente);

                Color c = colorNormal;
                if (esMejor && esReciente) c = colorAmbos;
                else if (esMejor) c = colorMejor;
                else if (esReciente) c = colorReciente;

                filas[i].AplicarColor(c);
            }
            else
            {
                filas[i].gameObject.SetActive(false);
            }
        }

    }
}