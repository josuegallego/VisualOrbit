using UnityEngine;

public class LevitacionIndicador : MonoBehaviour
{
    [Header("Configuración de Levitación")]
    public float amplitud = 0.05f;   // Qué tanto sube y baja (en metros)
    public float velocidad = 1f;     // Qué tan rápido se mueve

    private Vector3 posicionInicial;
    private float tiempoAleatorio;

    void Start()
    {
        // Guardamos la posición exacta donde lo pusiste en la escena
        posicionInicial = transform.localPosition;

        // Un pequeño offset aleatorio para que si hay varios objetos, no se muevan todos a la vez
        tiempoAleatorio = Random.Range(0f, 10f);
    }

    void Update()
    {
        // Usamos Mathf.Sin para un movimiento suave de arriba a abajo
        float offsetY = Mathf.Sin((Time.time + tiempoAleatorio) * velocidad) * amplitud;

        // Aplicamos el movimiento
        transform.localPosition = posicionInicial + new Vector3(0f, offsetY, 0f);
    }
}