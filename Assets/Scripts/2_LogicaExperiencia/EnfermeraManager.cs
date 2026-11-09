using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Una opción que el jugador pudo elegir, con el audio de la enfermera diciéndola.
/// El audio debe decir solo el nombre de la opción (ej: "huevo con jugo de lulo").
/// </summary>
[System.Serializable]
public class AudioOpcion
{
    public string momento;   // Desayuno / Almuerzo / Cena (sirve para desempatar opciones parecidas)
    public string opcion;    // Debe coincidir con DecisionRecord.opcion
    public AudioClip audio;  // Voz de la enfermera diciendo la opción
}

/// <summary>
/// Audios de la frase que rodea a la opción, por momento del día.
/// </summary>
[System.Serializable]
public class AudioMomento
{
    public string momento;        // Desayuno / Almuerzo / Cena
    public string textoMomento;   // Cómo se escribe: "el desayuno", "el almuerzo", "la cena"
    public AudioClip prefijo;     // "Por ejemplo, en el desayuno elegiste"
    public AudioClip conector;    // "y en el desayuno elegiste"
}

public class EnfermeraManager : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animatorEnfermera;
    public TMP_Text textoDialogo;
    public Button botonContinuar;
    public GameObject panelBarras;

    [Header("Audios (Voz de la enfermera)")]
    public AudioSource vozAudioSource;
    public AudioClip audioSaludo;
    public AudioClip audioRecomendacionBueno;
    public AudioClip audioRecomendacionRegular;
    public AudioClip audioRecomendacionMalo;
    public AudioClip audioDejandoAlgoEnClaro;
    public AudioClip audioDespedida;

    [Header("Audios del diagnóstico (opcionales, sin números)")]
    [Tooltip("Si asignas el clip, se usa el texto fijo de abajo (sin números). Si está vacío, se usa la frase con números y sin voz.")]
    public AudioClip audioDiagnosticoBueno;
    public AudioClip audioDiagnosticoRegular;
    public AudioClip audioDiagnosticoMalo;

    [Header("Audios de las decisiones")]
    public AudioClip audioBuenaEleccion;   // "una buena elección"
    public AudioClip audioMalaEleccion;    // "no es lo mejor para tu cerebro"

    [Tooltip("Prefijo y conector por momento (Desayuno, Almuerzo, Cena).")]
    public AudioMomento[] audiosMomentos = new AudioMomento[]
    {
        new AudioMomento { momento = "Desayuno", textoMomento = "el desayuno" },
        new AudioMomento { momento = "Almuerzo", textoMomento = "el almuerzo" },
        new AudioMomento { momento = "Cena",     textoMomento = "la cena" },
    };

    [Header("Audios de las opciones elegidas")]
    [Tooltip("Un clip por cada opción posible. Se busca por nombre (sin importar tildes ni mayúsculas) y se desempata por momento.")]
    public AudioOpcion[] audiosOpciones = new AudioOpcion[]
    {
        // DESAYUNO
        new AudioOpcion { momento = "Desayuno", opcion = "Energizante más galletas" },
        new AudioOpcion { momento = "Desayuno", opcion = "Café más pan queso" },
        new AudioOpcion { momento = "Desayuno", opcion = "Huevo con jugo de lulo" },
        // ALMUERZO
        new AudioOpcion { momento = "Almuerzo", opcion = "Salchipapa con aguapanela" },
        new AudioOpcion { momento = "Almuerzo", opcion = "Corrientazo" },
        new AudioOpcion { momento = "Almuerzo", opcion = "Lasaña" },
        // CENA
        new AudioOpcion { momento = "Cena", opcion = "Chocolate y pandebono" },
        new AudioOpcion { momento = "Cena", opcion = "Gaseosa más pastel de chocolate" },
        new AudioOpcion { momento = "Cena", opcion = "Energizante más papas" },
    };

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

    [Header("Textos fijos del diagnóstico (deben decir lo mismo que el audio)")]
    [TextArea(2, 5)] public string diagnosticoBueno = "Mis registros indican que elegiste opciones balanceadas, y tu energía, memoria e interferencia se mantuvieron estables.";
    [TextArea(2, 5)] public string diagnosticoRegular = "Mis registros indican que elegiste opciones variadas, y tu energía, memoria e interferencia quedaron en un punto intermedio.";
    [TextArea(2, 5)] public string diagnosticoMalo = "Mis registros indican que elegiste opciones con exceso de azúcares, y tu energía y memoria bajaron mientras tu interferencia subió.";

    [Header("Textos de las valoraciones (deben decir lo mismo que el audio)")]
    public string textoBuenaEleccion = " (una buena elección)";
    public string textoMalaEleccion = " (no es lo mejor para tu cerebro)";

    [Header("Velocidad y Tiempos")]
    public float velocidadLetra = 0.03f;
    public float velocidadTextoDecisiones = 0.035f;   // Solo se usa cuando un segmento no tiene audio
    public float velocidadDiagnostico = 0.07f;        // Solo se usa cuando no hay audio en diagnóstico/recomendación
    public float pausaEntreBloques = 1.5f;
    public float pausaAntesDeRecomendacion = 0.05f;
    public float duracionTransicionPanel = 0.5f;
    public float delayAntesDeTexto = 0.4f;
    public float multiplicadorVelocidad = 1.0f;

    [Header("Nombres de los estados y Bools en el Animator")]
    public string nombreIdle = "Idle";
    public string nombreSaludo = "Saludando";
    public string nombreDespedida = "Despidiendose";

    public string boolHablando = "EstaHablando";
    public string boolDecepcionada = "EstaDecepcionada";
    public string boolEnojada = "EstaEnojada";
    public string boolDejandoAlgoEnClaro = "EstaDejandoAlgoEnClaro";

    [Header("Debug")]
    public bool mostrarLogs = true;

    private bool yaIniciado = false;

    // ------------------------------------------------------------------ Validación en el Editor

    void OnValidate()
    {
        if (audiosOpciones == null) return;

        var vistos = new HashSet<string>();
        foreach (var a in audiosOpciones)
        {
            if (a == null || string.IsNullOrEmpty(a.opcion)) continue;

            string clave = Normalizar(a.momento) + "|" + Normalizar(a.opcion);
            if (!vistos.Add(clave))
                Debug.LogWarning($"[Enfermera] Opción duplicada en Audios Opciones: \"{a.momento} - {a.opcion}\". Solo se usará la primera.");
        }
    }

    // ------------------------------------------------------------------ Evaluación de desempeño

    enum Desempeno { Bueno, Regular, Malo }

    Desempeno EvaluarDesempeno()
    {
        var s = PlayerStats.Instance;
        if (s == null) return Desempeno.Regular;

        if (s.energy > 70f && s.interference < 30f && s.memory > 70f) return Desempeno.Bueno;
        if (s.energy < 40f || s.interference > 60f || s.memory < 40f) return Desempeno.Malo;
        return Desempeno.Regular;
    }

    // ------------------------------------------------------------------ Secuencia principal

    public void IniciarSecuencia()
    {
        if (yaIniciado) return;
        yaIniciado = true;

        if (panelBarras != null) panelBarras.SetActive(false);
        if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);

        SetAlpha(fondoNormal, 1f);
        SetAlpha(fondoVerde, 0f);
        SetAlpha(fondoNaranja, 0f);
        SetAlpha(fondoRojo, 0f);

        if (particulasSatisfaccion1 != null) { particulasSatisfaccion1.Stop(); particulasSatisfaccion1.Clear(); }
        if (particulasSatisfaccion2 != null) { particulasSatisfaccion2.Stop(); particulasSatisfaccion2.Clear(); }

        StartCoroutine(SecuenciaDialogo());
    }

    IEnumerator SecuenciaDialogo()
    {
        Log("1. Reproduciendo Saludo");
        animatorEnfermera.Play(nombreSaludo);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirSegmento(saludoBase, audioSaludo, true, velocidadLetra);
        animatorEnfermera.Play(nombreIdle);
        yield return new WaitForSeconds(pausaEntreBloques);

        Log("2. Mostrando panel de barras");
        if (panelBarras != null) panelBarras.SetActive(true);
        yield return new WaitForSeconds(pausaEntreBloques);

        AudioClip audioDiag;
        string diagnostico = ObtenerDiagnostico(out audioDiag);
        List<DecisionRecord> decisiones = ElegirDecisiones();
        string recomendacion = ElegirRecomendacion();
        string boolRec = ElegirBoolRecomendacion();
        AudioClip audioRec = ElegirAudioRecomendacion(boolRec);

        Log($"3. Activando Bool Recomendación: {boolRec}");
        ActivarBool(boolRec);
        yield return CambiarFondoSegunDesempeno(boolRec);

        if (boolRec == boolHablando)
        {
            if (particulasSatisfaccion1 != null) particulasSatisfaccion1.Play();
            if (particulasSatisfaccion2 != null) particulasSatisfaccion2.Play();
        }

        yield return new WaitForSeconds(delayAntesDeTexto);

        // 3a. Diagnóstico (barras)
        yield return EscribirSegmento(diagnostico, audioDiag, true, velocidadDiagnostico);

        // 3b. Decisiones concretas del jugador, todas con voz
        if (decisiones.Count > 0)
        {
            yield return new WaitForSeconds(pausaAntesDeRecomendacion);
            yield return EscribirDecisiones(decisiones);
        }

        // 3c. Recomendación
        yield return new WaitForSeconds(pausaAntesDeRecomendacion);
        yield return EscribirSegmento(recomendacion, audioRec, true, velocidadDiagnostico);

        DesactivarBool(boolRec);

        if (particulasSatisfaccion1 != null) particulasSatisfaccion1.Stop();
        if (particulasSatisfaccion2 != null) particulasSatisfaccion2.Stop();

        yield return new WaitForSeconds(pausaEntreBloques);

        Log("4. Activando Bool Dejando Algo En Claro");
        yield return VolverAlFondoNormal();

        ActivarBool(boolDejandoAlgoEnClaro);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirSegmento(dejandoAlgoEnClaro, audioDejandoAlgoEnClaro, true, velocidadLetra);

        DesactivarBool(boolDejandoAlgoEnClaro);
        yield return new WaitForSeconds(pausaEntreBloques);

        Log("5. Reproduciendo Despedida");
        animatorEnfermera.Play(nombreDespedida);
        yield return new WaitForSeconds(delayAntesDeTexto);
        yield return EscribirSegmento(despedida, audioDespedida, true, velocidadLetra);
        animatorEnfermera.Play(nombreIdle);

        Log("6. Mostrando botón Continuar");
        if (botonContinuar != null)
            botonContinuar.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------ Diagnóstico

    /// <summary>
    /// Si hay clip de diagnóstico para el desempeño actual, devuelve el texto fijo (sin números) y el clip.
    /// Si no, devuelve la frase con los números y sin clip.
    /// </summary>
    string ObtenerDiagnostico(out AudioClip clip)
    {
        clip = null;

        var s = PlayerStats.Instance;
        if (s == null) return "No hay datos de tu desempeño.";

        Desempeno d = EvaluarDesempeno();

        switch (d)
        {
            case Desempeno.Bueno: clip = audioDiagnosticoBueno; break;
            case Desempeno.Malo: clip = audioDiagnosticoMalo; break;
            default: clip = audioDiagnosticoRegular; break;
        }

        if (clip != null)
        {
            switch (d)
            {
                case Desempeno.Bueno: return diagnosticoBueno;
                case Desempeno.Malo: return diagnosticoMalo;
                default: return diagnosticoRegular;
            }
        }

        string decisiones = "elegiste opciones con exceso de azúcares";
        if (d == Desempeno.Bueno) decisiones = "elegiste opciones balanceadas";
        else if (d == Desempeno.Regular) decisiones = "elegiste opciones variadas";

        string barras = $"tu energía en {s.energy:F0}%, tu memoria en {s.memory:F0}% y tu interferencia en {s.interference:F0}%";

        return $"Mis registros indican que {decisiones}, lo que dejó {barras}.";
    }

    // ------------------------------------------------------------------ Decisiones del jugador

    /// <summary>
    /// Elige DOS decisiones reales del jugador.
    /// Bueno -> dos saludables | Malo -> dos no saludables | Regular -> una de cada tipo.
    /// Si no hay suficientes de un tipo, completa con las otras.
    /// </summary>
    List<DecisionRecord> ElegirDecisiones()
    {
        List<DecisionRecord> todas = PlayerDataStore.GetDecisionesDetalle()
            .Where(d => d != null && !string.IsNullOrEmpty(d.opcion))
            .ToList();

        List<DecisionRecord> elegidas = new List<DecisionRecord>();
        if (todas.Count == 0) return elegidas;

        var saludables = todas.Where(d => d.saludable).OrderBy(_ => Random.value).ToList();
        var noSaludables = todas.Where(d => !d.saludable).OrderBy(_ => Random.value).ToList();

        switch (EvaluarDesempeno())
        {
            case Desempeno.Bueno:
                elegidas.AddRange(saludables.Take(2));
                break;
            case Desempeno.Malo:
                elegidas.AddRange(noSaludables.Take(2));
                break;
            default:
                if (saludables.Count > 0) elegidas.Add(saludables[0]);
                if (noSaludables.Count > 0) elegidas.Add(noSaludables[0]);
                break;
        }

        // Completar hasta 2 con lo que haya
        if (elegidas.Count < 2)
        {
            foreach (var d in todas.OrderBy(_ => Random.value))
            {
                if (elegidas.Count >= 2) break;
                if (!elegidas.Contains(d)) elegidas.Add(d);
            }
        }

        return elegidas;
    }

    /// <summary>
    /// Escribe "Por ejemplo, en el desayuno elegiste X (...), y en la cena elegiste Y (...)."
    /// Cada parte (prefijo, opción, valoración) suena con su propio clip y el texto se sincroniza con él.
    /// </summary>
    IEnumerator EscribirDecisiones(List<DecisionRecord> decisiones)
    {
        textoDialogo.text = "";

        for (int i = 0; i < decisiones.Count; i++)
        {
            DecisionRecord d = decisiones[i];
            AudioMomento am = BuscarMomento(d.momento);
            string nombreMomento = (am != null && !string.IsNullOrEmpty(am.textoMomento)) ? am.textoMomento : d.momento;

            string textoPrefijo;
            AudioClip clipPrefijo;

            if (i == 0)
            {
                textoPrefijo = $"Por ejemplo, en {nombreMomento} elegiste ";
                clipPrefijo = am != null ? am.prefijo : null;
            }
            else
            {
                textoPrefijo = $", y en {nombreMomento} elegiste ";
                clipPrefijo = am != null ? am.conector : null;
            }

            if (clipPrefijo == null)
                Log($"AVISO: falta el audio de {(i == 0 ? "prefijo" : "conector")} para el momento \"{d.momento}\".");

            // 1) Prefijo
            yield return EscribirSegmento(textoPrefijo, clipPrefijo, false, velocidadTextoDecisiones);

            // 2) Opción elegida
            AudioClip clipOpcion = BuscarAudioOpcion(d);
            yield return EscribirSegmento(d.opcion, clipOpcion, false, velocidadTextoDecisiones);

            // 3) Valoración
            string textoVal = d.saludable ? textoBuenaEleccion : textoMalaEleccion;
            AudioClip clipVal = d.saludable ? audioBuenaEleccion : audioMalaEleccion;
            yield return EscribirSegmento(textoVal, clipVal, false, velocidadTextoDecisiones);
        }

        textoDialogo.text += ".";
        yield return new WaitForSeconds(pausaEntreBloques * 0.5f);
    }

    AudioMomento BuscarMomento(string momento)
    {
        if (audiosMomentos == null || string.IsNullOrEmpty(momento)) return null;

        string buscado = Normalizar(momento);
        foreach (var m in audiosMomentos)
        {
            if (m == null) continue;
            string mm = Normalizar(m.momento);
            if (string.IsNullOrEmpty(mm)) continue;
            if (mm == buscado || mm.Contains(buscado) || buscado.Contains(mm))
                return m;
        }
        return null;
    }

    // ------------------------------------------------------------------ Búsqueda de audio por opción

    /// <summary>
    /// Busca el clip de una decisión:
    /// 1) Coincidencia exacta del nombre (sin tildes/mayúsculas). Si hay varias, desempata por momento.
    /// 2) Si no hay exacta, coincidencia parcial filtrada por momento; solo se usa si es única.
    /// Si queda ambiguo, no reproduce nada (mejor silencio que el clip equivocado) y lo avisa en consola.
    /// </summary>
    AudioClip BuscarAudioOpcion(DecisionRecord d)
    {
        if (audiosOpciones == null || d == null) return null;

        string buscada = Normalizar(d.opcion);
        string momentoBuscado = Normalizar(d.momento);

        // 1) Exactas
        List<AudioOpcion> candidatas = audiosOpciones
            .Where(a => a != null && Normalizar(a.opcion) == buscada)
            .ToList();

        // 2) Parciales (solo si no hubo exactas)
        bool esParcial = false;
        if (candidatas.Count == 0)
        {
            candidatas = audiosOpciones
                .Where(a => a != null && !string.IsNullOrEmpty(a.opcion) &&
                            (Normalizar(a.opcion).Contains(buscada) || buscada.Contains(Normalizar(a.opcion))))
                .ToList();
            esParcial = true;
        }

        // Desempate por momento
        if (candidatas.Count > 1 && !string.IsNullOrEmpty(momentoBuscado))
        {
            var porMomento = candidatas.Where(a => MomentoCoincide(a.momento, momentoBuscado)).ToList();
            if (porMomento.Count > 0) candidatas = porMomento;
        }

        if (candidatas.Count == 0)
        {
            Log($"AVISO: ninguna entrada coincide con la opción \"{d.opcion}\" (momento \"{d.momento}\").");
            return null;
        }

        if (candidatas.Count > 1 && esParcial)
        {
            Log($"AVISO: la opción \"{d.opcion}\" (momento \"{d.momento}\") es ambigua entre varias entradas. No se reproduce audio. Revisa qué texto guarda DecisionRecord.opcion.");
            return null;
        }

        AudioOpcion elegida = candidatas[0];
        Log($"Opción del jugador: \"{d.opcion}\" ({d.momento}) -> entrada \"{elegida.opcion}\" -> clip \"{(elegida.audio != null ? elegida.audio.name : "NINGUNO")}\"");

        if (elegida.audio == null)
            Log($"AVISO: la entrada \"{elegida.opcion}\" no tiene clip asignado (la enfermera quedará en silencio en esa parte).");

        return elegida.audio;
    }

    bool MomentoCoincide(string momentoEntrada, string momentoBuscadoNormalizado)
    {
        string m = Normalizar(momentoEntrada);
        if (string.IsNullOrEmpty(m) || string.IsNullOrEmpty(momentoBuscadoNormalizado)) return false;
        return m == momentoBuscadoNormalizado || m.Contains(momentoBuscadoNormalizado) || momentoBuscadoNormalizado.Contains(m);
    }

    /// <summary>Minúsculas, sin tildes y sin espacios repetidos, para comparar nombres.</summary>
    static string Normalizar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        string descompuesto = s.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string limpio = sb.ToString().ToLowerInvariant();
        return string.Join(" ", limpio.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    // ------------------------------------------------------------------ Recomendación

    string ElegirRecomendacion()
    {
        switch (EvaluarDesempeno())
        {
            case Desempeno.Bueno: return recomendacionBueno;
            case Desempeno.Malo: return recomendacionMalo;
            default: return recomendacionRegular;
        }
    }

    string ElegirBoolRecomendacion()
    {
        if (PlayerStats.Instance == null) return boolHablando;

        switch (EvaluarDesempeno())
        {
            case Desempeno.Bueno: return boolHablando;
            case Desempeno.Malo: return boolEnojada;
            default: return boolDecepcionada;
        }
    }

    AudioClip ElegirAudioRecomendacion(string boolRec)
    {
        if (boolRec == boolHablando) return audioRecomendacionBueno;
        if (boolRec == boolEnojada) return audioRecomendacionMalo;
        return audioRecomendacionRegular;
    }

    // ------------------------------------------------------------------ Fondos

    IEnumerator CambiarFondoSegunDesempeno(string boolRec)
    {
        CanvasGroup objetivo;
        if (boolRec == boolHablando) objetivo = fondoVerde;
        else if (boolRec == boolEnojada) objetivo = fondoRojo;
        else objetivo = fondoNaranja;

        if (objetivo == null) yield break;

        yield return TransicionFondos(objetivo);
    }

    IEnumerator VolverAlFondoNormal()
    {
        if (fondoNormal == null) yield break;
        yield return TransicionFondos(fondoNormal);
    }

    /// <summary>Lleva a 1 el alpha del fondo objetivo y a 0 el de los demás.</summary>
    IEnumerator TransicionFondos(CanvasGroup objetivo)
    {
        CanvasGroup[] fondos = { fondoNormal, fondoVerde, fondoNaranja, fondoRojo };
        float[] inicio = new float[fondos.Length];
        for (int i = 0; i < fondos.Length; i++)
            inicio[i] = fondos[i] != null ? fondos[i].alpha : 0f;

        float t = 0f;
        while (t < duracionTransicionPanel)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracionTransicionPanel);

            for (int i = 0; i < fondos.Length; i++)
            {
                if (fondos[i] == null) continue;
                fondos[i].alpha = Mathf.Lerp(inicio[i], fondos[i] == objetivo ? 1f : 0f, k);
            }
            yield return null;
        }

        for (int i = 0; i < fondos.Length; i++)
        {
            if (fondos[i] != null) fondos[i].alpha = fondos[i] == objetivo ? 1f : 0f;
        }
    }

    // ------------------------------------------------------------------ Animator helpers

    void SetAlpha(CanvasGroup cg, float alpha) { if (cg != null) cg.alpha = alpha; }
    void ActivarBool(string nombre) { animatorEnfermera.SetBool(nombre, true); if (mostrarLogs) Debug.Log($"[Enfermera] SetBool({nombre}, true)"); }
    void DesactivarBool(string nombre) { animatorEnfermera.SetBool(nombre, false); if (mostrarLogs) Debug.Log($"[Enfermera] SetBool({nombre}, false)"); }

    // ------------------------------------------------------------------ Escritura de texto

    /// <summary>
    /// Escribe un texto letra por letra.
    /// - Si hay audio: lo reproduce, sincroniza la velocidad con su duración y espera a que termine.
    /// - Si no hay audio: usa velocidadFija.
    /// </summary>
    IEnumerator EscribirSegmento(string texto, AudioClip audio, bool limpiarAntes, float velocidadFija, bool esperarAudio = true)
    {
        if (limpiarAntes) textoDialogo.text = "";
        if (string.IsNullOrEmpty(texto)) yield break;

        float velocidad = velocidadFija;

        if (vozAudioSource != null && audio != null)
        {
            vozAudioSource.Stop();
            vozAudioSource.clip = audio;
            vozAudioSource.Play();

            velocidad = (audio.length / texto.Length) / Mathf.Max(0.01f, multiplicadorVelocidad);
            if (velocidad < 0.01f) velocidad = 0.01f;
        }

        foreach (char c in texto)
        {
            textoDialogo.text += c;
            yield return new WaitForSeconds(velocidad);
        }

        if (esperarAudio && vozAudioSource != null && vozAudioSource.isPlaying)
        {
            yield return new WaitWhile(() => vozAudioSource.isPlaying);
        }
    }

    void Log(string mensaje) { if (mostrarLogs) Debug.Log($"[Enfermera] {mensaje}"); }
}