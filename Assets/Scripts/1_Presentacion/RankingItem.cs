using UnityEngine;
using TMPro;

public class RankingItem : MonoBehaviour
{
    [Header("Textos de la Fila")]
    public TMP_Text textoNombre;
    public TMP_Text textoPuntaje;
    public TMP_Text textoTiempo;

    // Este método recibirá los datos del jugador y los pondrá en los textos
    public void Configurar(PlayerSession sesion)
    {
        textoNombre.text = sesion.nombre;
        textoPuntaje.text = $"{sesion.puntajeTotal:F0} pts";

        // Convertimos los segundos a formato Minutos:Segundos
        int minutos = Mathf.FloorToInt(sesion.duracionSegundos / 60f);
        int segundos = Mathf.FloorToInt(sesion.duracionSegundos % 60f);
        textoTiempo.text = $"{minutos:00}:{segundos:00}";
    }
    public void AplicarColor(Color color)
    {
        if (textoNombre != null) textoNombre.color = color;
        if (textoPuntaje != null) textoPuntaje.color = color;
        if (textoTiempo != null) textoTiempo.color = color;
    }
}  