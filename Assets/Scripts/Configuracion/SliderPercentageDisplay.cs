using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderPercentageDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Slider que queremos monitorizar")]
    public Slider targetSlider;

    [Tooltip("El texto donde mostraremos el porcentaje")]
    public TextMeshProUGUI percentageText;

    [Header("Configuración")]
    [Tooltip("Si está marcado, muestra el porcentaje (0-100%). Si no, muestra el valor raw (0-1)")]
    public bool showAsPercentage = true;

    [Tooltip("Número de decimales a mostrar (solo si showAsPercentage es false)")]
    public int decimals = 2;

    void Start()
    {
        if (targetSlider == null)
        {
            Debug.LogError("SliderPercentageDisplay: No se ha asignado el Slider.");
            return;
        }

        if (percentageText == null)
        {
            Debug.LogError("SliderPercentageDisplay: No se ha asignado el texto.");
            return;
        }

        // Nos suscribimos al evento del slider para actualizar el texto
        targetSlider.onValueChanged.AddListener(UpdateText);

        // Actualizamos el texto al inicio
        UpdateText(targetSlider.value);
    }

    void OnDestroy()
    {
        // Nos desuscribimos para evitar errores
        if (targetSlider != null)
        {
            targetSlider.onValueChanged.RemoveListener(UpdateText);
        }
    }

    private void UpdateText(float value)
    {
        if (percentageText == null) return;

        if (showAsPercentage)
        {
            // Convertimos 0-1 a 0-100%
            int percent = Mathf.RoundToInt(value * 100f);
            percentageText.text = percent + "%";
        }
        else
        {
            percentageText.text = value.ToString("F" + decimals);
        }
    }
}