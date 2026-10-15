using System.Collections;
using UnityEngine;

public class DisclaimerController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelDisclaimer;
    public GameObject panelEnfermera;

    [Header("Enfermera")]
    public EnfermeraManager enfermeraManager;

    [Header("Tiempo")]
    [Tooltip("Tiempo de espera tras el fade para iniciar la secuencia.")]
    public float delayAntesDeIniciar = 0.3f;

    public void OnEntendidoClicked()
    {
        if (panelDisclaimer == null || panelEnfermera == null)
        {
            Debug.LogError("[Disclaimer] Faltan referencias.");
            return;
        }

        PanelFader.Switch(panelDisclaimer, panelEnfermera);
        StartCoroutine(IniciarEnfermeraTrasFade());
    }

    IEnumerator IniciarEnfermeraTrasFade()
    {
        // Espera a que termine el fade del PanelFader
        yield return new WaitForSeconds(PanelFader.FadeDuration + delayAntesDeIniciar);

        if (enfermeraManager != null)
            enfermeraManager.IniciarSecuencia();
        else
            Debug.LogWarning("[Disclaimer] No asignaste el EnfermeraManager.");
    }
}