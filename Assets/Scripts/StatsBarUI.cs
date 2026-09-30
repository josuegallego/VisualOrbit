using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en cada barra (Energia, Interferencia, Memoria). Cuando el panel aparece,
/// la barra arranca desde el ultimo valor que se mostro y se anima hasta el valor actual.
/// Asi, en el panel de retroalimentacion se ve "bajar" o "subir" la barra.
/// </summary>
public class StatsBarUI : MonoBehaviour
{
    public enum StatType { Energy, Interference, Memory }

    public StatType stat;

    [Tooltip("Imagen de relleno. Si es Image Type = Filled usa fillAmount; si no, escala en X")]
    public Image fillImage;
    [Tooltip("Opcional: si tu barra es un Slider, arrastralo aqui")]
    public Slider slider;
    [Tooltip("Opcional: texto tipo 100%")]
    public TMP_Text valueText;

    [Tooltip("Velocidad de la animacion (1 = tarda 1 segundo en recorrer toda la barra)")]
    public float animSpeed = 0.6f;

    // Ultimo valor mostrado por cada barra (compartido entre todos los paneles). -1 = ninguno.
    static readonly float[] lastShown = { -1f, -1f, -1f };

    float shown;

    public static void ForgetLastValues() { lastShown[0] = lastShown[1] = lastShown[2] = -1f; }

    float Target()
    {
        var s = PlayerStats.Instance;
        if (s == null) return 0f;
        switch (stat)
        {
            case StatType.Energy: return s.energy / 100f;
            case StatType.Interference: return s.interference / 100f;
            default: return s.memory / 100f;
        }
    }

    void OnEnable()
    {
        float last = lastShown[(int)stat];
        shown = last < 0f ? Target() : last;
        Apply(shown);
    }

    void Update()
    {
        float t = Target();
        if (Mathf.Abs(shown - t) > 0.0005f)
        {
            shown = Mathf.MoveTowards(shown, t, animSpeed * Time.unscaledDeltaTime);
            Apply(shown);
        }
    }

    void Apply(float v)
    {
        lastShown[(int)stat] = v;

        if (slider != null) slider.value = v;

        if (fillImage != null)
        {
            if (fillImage.type == Image.Type.Filled) fillImage.fillAmount = v;
            else
            {
                // Para que crezca desde la izquierda pon Pivot X = 0 en el RectTransform
                var sc = fillImage.rectTransform.localScale;
                sc.x = v;
                fillImage.rectTransform.localScale = sc;
            }
        }

        if (valueText != null) valueText.text = Mathf.RoundToInt(v * 100f) + "%";
    }
}
