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

    [Header("Sincronizacion con los paneles")]
    [Tooltip("Cada pareja de cartas solo hace su demostracion mientras su panel esta visible, y empieza desde cero cada vez que aparece.")]
    public bool soloMientrasVisible = true;

    void Start()
    {
        // Una rutina por pareja: arrancan cuando el panel de esas cartas se activa.
        StartCoroutine(RutinaPareja(carta1, carta2, Color.green));
        StartCoroutine(RutinaPareja(carta3, carta4, Color.red));
        StartCoroutine(RutinaTimer());
        StartCoroutine(RutinaControlVR());
    }

    bool Visible(CartaVoltear c) => c != null && (!soloMientrasVisible || c.gameObject.activeInHierarchy);

    // Espera (sin consumir nada) hasta que las dos cartas esten visibles.
    IEnumerator EsperarVisibles(CartaVoltear a, CartaVoltear b)
    {
        while (!(Visible(a) && Visible(b))) yield return null;
    }

    // Espera "t" segundos; devuelve false si las cartas se ocultaron antes (el panel cambio).
    IEnumerator Esperar(float t, CartaVoltear a, CartaVoltear b, System.Action<bool> ok)
    {
        float r = 0f;
        while (r < t)
        {
            if (!(Visible(a) && Visible(b))) { ok(false); yield break; }
            r += Time.deltaTime;
            yield return null;
        }
        ok(true);
    }

    IEnumerator RutinaPareja(CartaVoltear a, CartaVoltear b, Color resalte)
    {
        if (a == null || b == null) yield break;
        while (true)
        {
            yield return EsperarVisibles(a, b);

            // Siempre arranca con las dos cartas boca abajo y sin resalte.
            a.QuitarResalte(); b.QuitarResalte();
            a.MostrarDorso(true); b.MostrarDorso(true);

            bool ok = true;
            yield return Esperar(1.0f, a, b, v => ok = v); if (!ok) continue;
            a.MostrarFrente();
            yield return Esperar(2.0f, a, b, v => ok = v); if (!ok) continue;
            b.MostrarFrente();
            yield return Esperar(1.0f, a, b, v => ok = v); if (!ok) continue;
            a.Resaltar(resalte, 0.8f); b.Resaltar(resalte, 0.8f);
            yield return Esperar(3.0f, a, b, v => ok = v); if (!ok) continue;
            a.QuitarResalte(); b.QuitarResalte();
            a.MostrarDorso(); b.MostrarDorso();
            yield return Esperar(2.0f, a, b, v => ok = v);
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