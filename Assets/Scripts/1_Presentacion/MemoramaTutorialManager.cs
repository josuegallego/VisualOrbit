using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MemoramaTutorialManager : MonoBehaviour
{
    [Header("Cartas de Ejemplo (Usa 4 cartas)")]
    public CartaVoltear carta1;
    public CartaVoltear carta2;
    public CartaVoltear carta3;
    public CartaVoltear carta4;

    [Header("Control VR 3D")]
    [Tooltip("El Renderer del botón del control (Trigger o A) que va a brillar")]
    public Renderer botonControlRenderer;
    public Color colorBrilloBoton = Color.cyan;
    public float velocidadParpadeo = 0.5f;

    [Header("Efecto de Escala del Control")]
    public float escalaMaxima = 1.2f;
    public float velocidadEscala = 5f;

    [Header("Temporizador UI")]
    public Image barraTiempo;
    public float tiempoTotalTimer = 50f;

    void Start()
    {
        StartCoroutine(RutinaPrincipal());
        StartCoroutine(RutinaTimer());
        StartCoroutine(RutinaControlVR());
    }

    IEnumerator RutinaPrincipal()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // --- FASE 1: MOSTRAR UN ACIERTO ---
            carta1.MostrarFrente();
            yield return new WaitForSeconds(3f);

            carta2.MostrarFrente();
            yield return new WaitForSeconds(1f);

            carta1.Resaltar(Color.green, 0.8f);
            carta2.Resaltar(Color.green, 0.8f);
            yield return new WaitForSeconds(3f);

            carta1.MostrarDorso();
            carta2.MostrarDorso();
            yield return new WaitForSeconds(2f);

            // --- FASE 2: MOSTRAR UN ERROR ---
            carta3.MostrarFrente();
            yield return new WaitForSeconds(3f);

            carta4.MostrarFrente();
            yield return new WaitForSeconds(1f);

            carta3.Resaltar(Color.red, 0.8f);
            carta4.Resaltar(Color.red, 0.8f);
            yield return new WaitForSeconds(3f);

            carta3.MostrarDorso();
            carta4.MostrarDorso();

            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator RutinaControlVR()
    {
        if (botonControlRenderer == null) yield break;

        // NUEVO: En URP, hay que habilitar la propiedad de emisión en el material instanciado
        Material mat = botonControlRenderer.material;
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        // En URP, también aseguramos que la propiedad de emisión esté activa en el shader
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);

        Color colorApagado = Color.black;
        Vector3 escalaOriginal = botonControlRenderer.transform.localScale;

        while (true)
        {
            // --- FASE DE ENCENDIDO (Color + Crecimiento) ---
            mat.SetColor("_EmissionColor", colorBrilloBoton);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadEscala;
                botonControlRenderer.transform.localScale = Vector3.Lerp(escalaOriginal, escalaOriginal * escalaMaxima, t);
                yield return null;
            }

            yield return new WaitForSeconds(velocidadParpadeo * 0.5f);

            // --- FASE DE APAGADO (Color + Regreso a la escala) ---
            mat.SetColor("_EmissionColor", colorApagado);

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadEscala;
                botonControlRenderer.transform.localScale = Vector3.Lerp(escalaOriginal * escalaMaxima, escalaOriginal, t);
                yield return null;
            }

            yield return new WaitForSeconds(velocidadParpadeo * 0.5f);
        }
    }

    IEnumerator RutinaTimer()
    {
        if (barraTiempo == null) yield break;

        while (true)
        {
            float tiempoRestante = tiempoTotalTimer;

            while (tiempoRestante > 0)
            {
                tiempoRestante -= Time.deltaTime;
                barraTiempo.fillAmount = tiempoRestante / tiempoTotalTimer;
                yield return null;
            }

            barraTiempo.fillAmount = 1f;
            yield return new WaitForSeconds(2f);
        }
    }
}