using UnityEngine;
using TMPro;

public class RankingItem : MonoBehaviour
{
    [Header("Textos de la Fila")]
    public TMP_Text textoNombre;
    public TMP_Text textoPuntaje;
    public TMP_Text textoTiempo;
    [Tooltip("Opcional: numero de puesto (1., 2., 3. ...).")]
    public TMP_Text textoPuesto;

    [Header("Fila del jugador actual")]
    [Tooltip("Texto que se agrega al nombre del jugador que acaba de jugar. Ej: 'Santiago (Tú)'.")]
    public string etiquetaJugador = "Tú";
    [Tooltip("Si esta activo, en vez de 'Santiago (Tú)' muestra solo 'Tú'.")]
    public bool soloEtiqueta = false;

    // Este metodo recibe los datos del jugador y los pone en los textos
    public void Configurar(PlayerSession sesion, int puesto = 0, bool esJugador = false)
    {
        string nombre = sesion.nombre;
        if (esJugador && !string.IsNullOrEmpty(etiquetaJugador))
            nombre = soloEtiqueta ? etiquetaJugador : $"{sesion.nombre} ({etiquetaJugador})";

        if (textoNombre != null) textoNombre.text = nombre;
        if (textoPuntaje != null) textoPuntaje.text = $"{sesion.puntajeTotal:F0} pts";
        if (textoPuesto != null) textoPuesto.text = puesto > 0 ? $"{puesto}." : "";

        // Convertimos los segundos a formato Minutos:Segundos
        int minutos = Mathf.FloorToInt(sesion.duracionSegundos / 60f);
        int segundos = Mathf.FloorToInt(sesion.duracionSegundos % 60f);
        if (textoTiempo != null) textoTiempo.text = $"{minutos:00}:{segundos:00}";
    }

    public void AplicarColor(Color color)
    {
        if (textoNombre != null) textoNombre.color = color;
        if (textoPuntaje != null) textoPuntaje.color = color;
        if (textoTiempo != null) textoTiempo.color = color;
        if (textoPuesto != null) textoPuesto.color = color;
    }
}
