using System.Collections;
using UnityEngine;

public class ControlVRAnimation : MonoBehaviour
{
    [Header("Renderers de los Botones (Asignar en el Inspector)")]
    public Renderer bumperRenderer;
    public Renderer triggerRenderer;
    public Renderer botonRenderer;

    [Header("Colores Independientes para cada Botón")]
    public Color colorBumper = Color.cyan;
    public Color colorTrigger = Color.magenta;
    public Color colorBoton = Color.yellow;

    [Header("Configuración Visual")]
    [Tooltip("Qué tanto crecerá el botón al latir (1.2 = 20% más grande)")]
    public float escalaMaxima = 1.2f;
    [Tooltip("Velocidad del latido")]
    public float velocidadPulso = 5f;

    [Header("Tiempos de la Secuencia")]
    public float esperaInicial = 1f;
    public float tiempoEntreBotones = 1.5f;
    public float tiempoTodosEncendidos = 2f;
    public float tiempoAntesDeReiniciar = 1.5f;

    // Variables para guardar la escala original
    private Vector3 escalaOriginalBumper;
    private Vector3 escalaOriginalTrigger;
    private Vector3 escalaOriginalBoton;

    // Variables para controlar las corrutinas de pulso independientes
    private Coroutine rutinaPulsoBumper;
    private Coroutine rutinaPulsoTrigger;
    private Coroutine rutinaPulsoBoton;

    void Start()
    {
        if (bumperRenderer != null) escalaOriginalBumper = bumperRenderer.transform.localScale;
        if (triggerRenderer != null) escalaOriginalTrigger = triggerRenderer.transform.localScale;
        if (botonRenderer != null) escalaOriginalBoton = botonRenderer.transform.localScale;

        StartCoroutine(SecuenciaAnimacion());
    }

    void OnDisable()
    {
        // Detenemos todas las corrutinas si salimos de la escena o apagamos el objeto
        StopAllCoroutines();
        ApagarBotonInmediato(bumperRenderer, escalaOriginalBumper);
        ApagarBotonInmediato(triggerRenderer, escalaOriginalTrigger);
        ApagarBotonInmediato(botonRenderer, escalaOriginalBoton);
    }

    IEnumerator SecuenciaAnimacion()
    {
        // Preparar materiales (URP)
        PrepararMaterial(bumperRenderer);
        PrepararMaterial(triggerRenderer);
        PrepararMaterial(botonRenderer);

        yield return new WaitForSeconds(esperaInicial);

        while (true)
        {
            // --- SECUENCIA DE ENCENDIDO (con pulso) ---

            EncenderYLatir(bumperRenderer, colorBumper, escalaOriginalBumper, ref rutinaPulsoBumper);
            yield return new WaitForSeconds(tiempoEntreBotones);

            EncenderYLatir(triggerRenderer, colorTrigger, escalaOriginalTrigger, ref rutinaPulsoTrigger);
            yield return new WaitForSeconds(tiempoEntreBotones);

            EncenderYLatir(botonRenderer, colorBoton, escalaOriginalBoton, ref rutinaPulsoBoton);

            // Mantener los 3 latiendo al mismo tiempo
            yield return new WaitForSeconds(tiempoTodosEncendidos);

            // --- SECUENCIA DE APAGADO ---
            ApagarBoton(bumperRenderer, escalaOriginalBumper, ref rutinaPulsoBumper);
            ApagarBoton(triggerRenderer, escalaOriginalTrigger, ref rutinaPulsoTrigger);
            ApagarBoton(botonRenderer, escalaOriginalBoton, ref rutinaPulsoBoton);

            yield return new WaitForSeconds(tiempoAntesDeReiniciar);
        }
    }

    // ====================================================================
    // LÓGICA DE PULSO
    // ====================================================================

    void EncenderYLatir(Renderer r, Color color, Vector3 escalaBase, ref Coroutine rutina)
    {
        if (r == null) return;

        // Si ya había un pulso activo, lo detenemos
        if (rutina != null) StopCoroutine(rutina);

        // Iniciamos el nuevo pulso
        rutina = StartCoroutine(EfectoLatido(r, color, escalaBase));
    }

    IEnumerator EfectoLatido(Renderer r, Color color, Vector3 escalaBase)
    {
        Material mat = r.material;
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color);

        float tiempoTranscurrido = 0f;

        while (true)
        {
            tiempoTranscurrido += Time.deltaTime * velocidadPulso;

            // Mathf.PingPong hace que el valor vaya de 0 a 1 y de vuelta a 0 suavemente
            float t = Mathf.PingPong(tiempoTranscurrido, 1f);

            // Aplicamos la escala entre la original y la máxima
            r.transform.localScale = Vector3.Lerp(escalaBase, escalaBase * escalaMaxima, t);

            yield return null;
        }
    }

    void ApagarBoton(Renderer r, Vector3 escalaBase, ref Coroutine rutina)
    {
        if (r == null) return;

        // Detenemos el latido
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        // Volvemos a su estado normal inmediatamente
        ApagarBotonInmediato(r, escalaBase);
    }

    void ApagarBotonInmediato(Renderer r, Vector3 escalaBase)
    {
        if (r == null) return;
        r.transform.localScale = escalaBase;
        if (r.material.HasProperty("_EmissionColor"))
            r.material.SetColor("_EmissionColor", Color.black);
    }

    void PrepararMaterial(Renderer r)
    {
        if (r == null) return;
        Material mat = r.material;
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
    }
}