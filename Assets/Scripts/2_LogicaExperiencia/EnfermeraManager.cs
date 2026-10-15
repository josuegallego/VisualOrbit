using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnfermeraManager : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animatorEnfermera;
    public TMP_Text textoDialogo;
    public Button botonContinuar;
    public GameObject panelBarras;

    [Header("Fondos de color (con CanvasGroup, hijos del PanelDialogo)")]
    public CanvasGroup fondoNormal;
    public CanvasGroup fondoVerde;
    public CanvasGroup fondoNaranja;
    public CanvasGroup fondoRojo;

    [Header("Partículas de satisfacción")]
    public ParticleSystem particulasSatisfaccion1;
    public ParticleSystem particulasSatisfaccion2;

    [Header("Textos")]
    [TextArea(2, 5)] public string saludoBase = "Hola, soy Marcela Ramírez, enfermera de la UAO. He revisado tus decisiones y tu desempeño durante la jornada.";
    [TextArea(3, 8)] public string recomendacionBueno = "Tus elecciones fueron equilibradas. Tu energía, memoria e interferencia cognitiva se mantuvieron estables. Sigue así, tu cerebro te lo agradece.";
    [TextArea(3, 8)] public string recomendacionRegular = "Tus elecciones afectaron parcialmente tu rendimiento. Te recomiendo incluir en tu desayuno huevos, un carbohidrato complejo como papa o plátano cocido, agua y una fruta entera. En almuerzo y cena debes tener obligatoriamente 4 elementos.";
    [TextArea(3, 8)] public string recomendacionMalo = "Tus decisiones afectaron seriamente tu rendimiento cognitivo. Tu memoria y energía bajaron, y tu interferencia cognitiva subió. Necesitas incluir proteínas, carbohidratos complejos, vegetales y grasas saludables en cada comida. Tu cerebro no puede funcionar óptimamente sin estos componentes.";
    [TextArea(2, 5)] public string dejandoAlgoEnClaro = "Recuerda que esta experiencia es educativa y no reemplaza un diagnóstico médico real.";
    [TextArea(2, 5)] public string despedida = "Cuídate y nos vemos en la próxima jornada.";

    [Header("Velocidad")]
    public float velocidadLetra = 0.03f;
    public float pausaEntreBloques = 1.5f;
    public float duracionTransicionPanel = 0.5f;
    public float delayAntesDeTexto = 0.4f;

    [Header("Nombres de los Bool en el Animator")]
    public string boolSaludando = "EstaSaludando";
    public string boolHablando = "EstaHablando";
    public string boolDecepcionada = "EstaDecepcionada";
    public string boolEnojada = "EstaEnojada";
    public string boolDejandoAlgoEnClaro = "EstaDejandoAlgoEnClaro";
    public string boolDespidiendose = "EstaDespidiendose";

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool yaIniciado = false;

    // ---------------------------------------------------------------- API pública

    public void IniciarSecuencia()
    {
        if (yaIniciado)
        {
            Debug.LogWarning("[EnfermeraManager] La secuencia ya se inició.");
            return;
        }
        yaIniciado = true;

        if (panelBarras != null) panelBarras.SetActive(false);
        if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);

        SetAlpha(fondoNormal, 1f);
        SetAlpha(fondoVerde, 0f);
        SetAlpha(fondoNaranja, 0f);
        SetAlpha(fondoRojo, 0f);

        if (particulasSatisfaccion1 != null)
        {
            particulasSatisfaccion1.Stop();
            particulasSatisfaccion1.Clear();
        }

        if (particulasSatisfaccion2 != null)
        {
            particulasSatisfaccion2.Stop();
            particulasSatisfaccion2.Clear();
        }

        StartCoroutine(SecuenciaDialogo());
    }

    // ---------------------------------------------------------------- Secuencia

    IEnumerator SecuenciaDialogo()
    {
        // 1) Saludo
        Log("1. Activando Bool Saludando");
        ActivarBool(boolSaludando);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirTexto(saludoBase);
        DesactivarBool(boolSaludando);
        yield return new WaitForSeconds(pausaEntreBloques);

        // 2) Barras
        Log("2. Mostrando panel de barras");
        if (panelBarras != null) panelBarras.SetActive(true);
        yield return new WaitForSeconds(pausaEntreBloques);

        // 3) Recomendación según desempeño
        string recomendacion = ElegirRecomendacion();
        string boolRec = ElegirBoolRecomendacion();
        Log($"3. Activando Bool Recomendación: {boolRec}");
        ActivarBool(boolRec);

        // Cambiar el fondo con transición según el desempeño
        yield return CambiarFondoSegunDesempeno(boolRec);

        // Activar partículas si el desempeño es bueno
        if (boolRec == boolHablando)
        {
            if (particulasSatisfaccion1 != null) particulasSatisfaccion1.Play();
            if (particulasSatisfaccion2 != null) particulasSatisfaccion2.Play();
        }

        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirTexto(recomendacion);
        DesactivarBool(boolRec);

        // Detener las partículas al terminar el bloque
        if (particulasSatisfaccion1 != null) particulasSatisfaccion1.Stop();
        if (particulasSatisfaccion2 != null) particulasSatisfaccion2.Stop();

        yield return new WaitForSeconds(pausaEntreBloques);

        // 4) Dejando algo en claro (volver al fondo normal)
        Log($"4. Volviendo al fondo normal");
        yield return VolverAlFondoNormal();

        ActivarBool(boolDejandoAlgoEnClaro);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirTexto(dejandoAlgoEnClaro);
        DesactivarBool(boolDejandoAlgoEnClaro);
        yield return new WaitForSeconds(pausaEntreBloques);

        // 5) Despedida
        Log($"5. Activando Bool Despidiendose: {boolDespidiendose}");
        ActivarBool(boolDespidiendose);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirTexto(despedida);
        yield return new WaitForSeconds(delayAntesDeTexto);
        DesactivarBool(boolDespidiendose);

        // 6) Botón Continuar
        Log("6. Mostrando botón Continuar");
        if (botonContinuar != null)
            botonContinuar.gameObject.SetActive(true);
    }

    // ---------------------------------------------------------------- Cambio de fondo

    IEnumerator CambiarFondoSegunDesempeno(string boolRec)
    {
        CanvasGroup objetivo = fondoNormal;

        if (boolRec == boolHablando) objetivo = fondoVerde;
        else if (boolRec == boolEnojada) objetivo = fondoRojo;
        else objetivo = fondoNaranja;

        if (objetivo == null) yield break;

        float t = 0f;
        float aNormal = fondoNormal != null ? fondoNormal.alpha : 0f;
        float aVerde = fondoVerde != null ? fondoVerde.alpha : 0f;
        float aNaranja = fondoNaranja != null ? fondoNaranja.alpha : 0f;
        float aRojo = fondoRojo != null ? fondoRojo.alpha : 0f;

        while (t < duracionTransicionPanel)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duracionTransicionPanel;

            if (fondoNormal != null)
                fondoNormal.alpha = Mathf.Lerp(aNormal, objetivo == fondoNormal ? 1f : 0f, k);
            if (fondoVerde != null)
                fondoVerde.alpha = Mathf.Lerp(aVerde, objetivo == fondoVerde ? 1f : 0f, k);
            if (fondoNaranja != null)
                fondoNaranja.alpha = Mathf.Lerp(aNaranja, objetivo == fondoNaranja ? 1f : 0f, k);
            if (fondoRojo != null)
                fondoRojo.alpha = Mathf.Lerp(aRojo, objetivo == fondoRojo ? 1f : 0f, k);

            yield return null;
        }

        if (fondoNormal != null) fondoNormal.alpha = objetivo == fondoNormal ? 1f : 0f;
        if (fondoVerde != null) fondoVerde.alpha = objetivo == fondoVerde ? 1f : 0f;
        if (fondoNaranja != null) fondoNaranja.alpha = objetivo == fondoNaranja ? 1f : 0f;
        if (fondoRojo != null) fondoRojo.alpha = objetivo == fondoRojo ? 1f : 0f;
    }

    IEnumerator VolverAlFondoNormal()
    {
        if (fondoNormal == null) yield break;

        float t = 0f;
        float aNormal = fondoNormal.alpha;
        float aVerde = fondoVerde != null ? fondoVerde.alpha : 0f;
        float aNaranja = fondoNaranja != null ? fondoNaranja.alpha : 0f;
        float aRojo = fondoRojo != null ? fondoRojo.alpha : 0f;

        while (t < duracionTransicionPanel)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duracionTransicionPanel;

            if (fondoNormal != null)
                fondoNormal.alpha = Mathf.Lerp(aNormal, 1f, k);
            if (fondoVerde != null)
                fondoVerde.alpha = Mathf.Lerp(aVerde, 0f, k);
            if (fondoNaranja != null)
                fondoNaranja.alpha = Mathf.Lerp(aNaranja, 0f, k);
            if (fondoRojo != null)
                fondoRojo.alpha = Mathf.Lerp(aRojo, 0f, k);

            yield return null;
        }

        if (fondoNormal != null) fondoNormal.alpha = 1f;
        if (fondoVerde != null) fondoVerde.alpha = 0f;
        if (fondoNaranja != null) fondoNaranja.alpha = 0f;
        if (fondoRojo != null) fondoRojo.alpha = 0f;
    }

    void SetAlpha(CanvasGroup cg, float alpha)
    {
        if (cg != null) cg.alpha = alpha;
    }

    // ---------------------------------------------------------------- Control de Bool

    void ActivarBool(string nombre)
    {
        animatorEnfermera.SetBool(nombre, true);
        if (mostrarLogs)
            Debug.Log($"[Enfermera] SetBool({nombre}, true) → valor actual: {animatorEnfermera.GetBool(nombre)}");
    }

    void DesactivarBool(string nombre)
    {
        animatorEnfermera.SetBool(nombre, false);
        if (mostrarLogs)
            Debug.Log($"[Enfermera] SetBool({nombre}, false)");
    }

    // ---------------------------------------------------------------- Lógica de decisión

    string ElegirRecomendacion()
    {
        var s = PlayerStats.Instance;
        if (s == null) return recomendacionRegular;

        bool bueno = s.energy > 70f && s.interference < 30f && s.memory > 70f;
        bool malo = s.energy < 40f || s.interference > 60f || s.memory < 40f;

        if (bueno) return recomendacionBueno;
        if (malo) return recomendacionMalo;
        return recomendacionRegular;
    }

    string ElegirBoolRecomendacion()
    {
        var s = PlayerStats.Instance;
        if (s == null) return boolHablando;

        bool bueno = s.energy > 70f && s.interference < 30f && s.memory > 70f;
        bool malo = s.energy < 40f || s.interference > 60f || s.memory < 40f;

        if (bueno) return boolHablando;
        if (malo) return boolEnojada;
        return boolDecepcionada;
    }

    // ---------------------------------------------------------------- Escritura letra por letra

    IEnumerator EscribirTexto(string texto)
    {
        textoDialogo.text = "";
        foreach (char c in texto)
        {
            textoDialogo.text += c;
            yield return new WaitForSeconds(velocidadLetra);
        }
    }

    // ---------------------------------------------------------------- Debug

    void Log(string mensaje)
    {
        if (mostrarLogs)
            Debug.Log($"[Enfermera] {mensaje}");
    }
}