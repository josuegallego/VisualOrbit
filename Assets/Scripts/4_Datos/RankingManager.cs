using System.Collections.Generic;
using UnityEngine;

public class RankingManager : MonoBehaviour
{
    [Header("Renglones ya colocados en la escena (de arriba hacia abajo)")]
    public List<RankingItem> filas;

    [Header("Opcional")]
    public GameObject textoVacio;

    void Start()
    {
        PlayerDataStore.CloseActiveSessionIfOpen(true);
        MostrarRanking();
    }

    public void MostrarRanking()
    {
        List<PlayerSession> ranking = PlayerDataStore.GetRanking(soloCompletadas: false);

        if (textoVacio != null) textoVacio.SetActive(ranking.Count == 0);

        for (int i = 0; i < filas.Count; i++)
        {
            if (i < ranking.Count)
            {
                filas[i].gameObject.SetActive(true);
                filas[i].Configurar(ranking[i]);
            }
            else
            {
                filas[i].gameObject.SetActive(false); // sin jugador, se oculta
            }
        }
    }
}