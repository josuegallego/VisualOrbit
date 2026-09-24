using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BarraAlimentaria : MonoBehaviour
{
    [Header("Identificación")]
    [Tooltip("Id único para referenciar esta barra desde otros scripts (ej: 'Proteina', 'Energia')")]
    public string idBarra;

    [Header("Valor inicial")]
    [Range(0f, 100f)]
    public float porcentajeInicial = 50f;

    [Header("Referencia UI (usa una sola)")]
    public Slider sliderUI;
    public Image imagenFillUI;

    [Header("Texto (opcional)")]
    [Tooltip("Arrastra aquí un TextMeshProUGUI si quieres mostrar el porcentaje como número")]
    public TMP_Text textoPorcentaje;
    [Tooltip("Formato del texto. {0} se reemplaza por el número (ej: '{0}%' -> '75%')")]
    public string formatoTexto = "{0:0}%";

    [Header("Animación")]
    public bool animarCambio = true;
    [Tooltip("Qué tan rápido se mueve la barra hacia el nuevo valor (unidades de % por segundo)")]
    public float velocidadAnimacion = 60f;

    [Header("Límites")]
    public float minimo = 0f;
    public float maximo = 100f;

    private float porcentajeActual;
    private float porcentajeObjetivo;

    void Awake()
    {
        porcentajeActual = porcentajeInicial;
        porcentajeObjetivo = porcentajeInicial;
        ActualizarUI();
    }

    void Update()
    {
        if (animarCambio && !Mathf.Approximately(porcentajeActual, porcentajeObjetivo))
        {
            porcentajeActual = Mathf.MoveTowards(porcentajeActual, porcentajeObjetivo, velocidadAnimacion * Time.deltaTime);
            ActualizarUI();
        }
    }

    /// <summary>
    /// Suma (o resta) un valor al porcentaje actual.
    /// Este es el método que, más adelante, llamará el sistema real de
    /// preguntas correctas/incorrectas con el valor que él calcule.
    /// </summary>
    public void AplicarCambio(float delta)
    {
        porcentajeObjetivo = Mathf.Clamp(porcentajeObjetivo + delta, minimo, maximo);
        if (!animarCambio)
        {
            porcentajeActual = porcentajeObjetivo;
            ActualizarUI();
        }
    }

    /// <summary>Fija el porcentaje directamente, sin sumar/restar.</summary>
    public void SetPorcentaje(float valor)
    {
        porcentajeObjetivo = Mathf.Clamp(valor, minimo, maximo);
        if (!animarCambio)
        {
            porcentajeActual = porcentajeObjetivo;
            ActualizarUI();
        }
    }

    public float ObtenerPorcentaje() => porcentajeActual;

    private void ActualizarUI()
    {
        float normalizado = porcentajeActual / 100f;
        if (sliderUI != null) sliderUI.value = normalizado;
        if (imagenFillUI != null) imagenFillUI.fillAmount = normalizado;
        if (textoPorcentaje != null) textoPorcentaje.text = string.Format(formatoTexto, porcentajeActual);
    }
}