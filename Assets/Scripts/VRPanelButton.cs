using UnityEngine;
using System.Collections;

public class VRPanelButton : MonoBehaviour
{
    [Header("Panel que se está mostrando")]
    public CanvasGroup panelActual;

    [Header("Panel al que quieres ir")]
    public CanvasGroup panelDestino;

    [Header("Duración de la transición")]
    public float duracionFade = 0.5f;

    private bool cambiando = false;

    public void CambiarPanel()
    {
        if (!cambiando)
        {
            StartCoroutine(Transicion());
        }
    }

    private IEnumerator Transicion()
    {
        cambiando = true;

        // FADE OUT DEL PANEL ACTUAL
        float tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;

            panelActual.alpha = Mathf.Lerp(
                1f,
                0f,
                tiempo / duracionFade
            );

            yield return null;
        }

        panelActual.alpha = 0f;

        // Desactivar panel actual
        panelActual.gameObject.SetActive(false);

        // Activar panel destino
        panelDestino.gameObject.SetActive(true);

        // Empezar invisible
        panelDestino.alpha = 0f;

        // FADE IN DEL PANEL DESTINO
        tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;

            panelDestino.alpha = Mathf.Lerp(
                0f,
                1f,
                tiempo / duracionFade
            );

            yield return null;
        }

        panelDestino.alpha = 1f;

        cambiando = false;
    }
}