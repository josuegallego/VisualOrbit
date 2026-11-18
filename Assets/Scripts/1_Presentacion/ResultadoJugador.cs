using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Panel "TU RESULTADO" de la escena Ranking:
///   Energía final, Memoria final, Decisiones conscientes (X de N) y una frase de cierre
///   que sale de lo que el jugador eligió de verdad.
///
/// Lee los valores de PlayerStats (barras) y de la sesión del jugador (decisiones).
/// Todos los textos son opcionales: llena solo los que arrastres.
///
/// USO: ver instrucciones al final del archivo.
/// </summary>
public class ResultadoJugador : MonoBehaviour
{
    [Header("Textos (arrastra los TMP del panel)")]
    public TMP_Text energiaText;
    public TMP_Text memoriaText;
    [Tooltip("Opcional: interferencia cognitiva final.")]
    public TMP_Text interferenciaText;
    [Tooltip("Ej: '6 de 9'.")]
    public TMP_Text decisionesText;
    [Tooltip("Frase de cierre.")]
    public TMP_Text fraseText;
    [Tooltip("Opcional: puntaje total del jugador.")]
    public TMP_Text puntajeText;

    [Header("Colores")]
    [Tooltip("Si esta activo, los porcentajes cambian de color: verde (bien), amarillo, rojo (mal).")]
    public bool colorearPorValor = true;
    public Color colorBien = new Color(0.55f, 1f, 0.2f);
    public Color colorMedio = new Color(0.97f, 0.78f, 0.2f);
    public Color colorMal = new Color(0.95f, 0.35f, 0.3f);
    [Tooltip("Colores fijos si 'Colorear Por Valor' esta apagado.")]
    public Color colorEnergiaFijo = new Color(0.55f, 1f, 0.2f);
    public Color colorMemoriaFijo = new Color(0.3f, 0.9f, 1f);

    [Header("Frases (el jugador ve una de estas)")]
    [TextArea(1, 3)] public string fraseTodasSaludables = "Priorizaste tu nutrición incluso en momentos de presión.";
    [TextArea(1, 3)] public string fraseMixta = "Priorizaste la practicidad sobre la nutrición en tu {momento}: {opcion}.";
    [TextArea(1, 3)] public string fraseNinguna = "Esta vez la practicidad ganó en todas tus comidas. La próxima, prueba cuidar tu mente.";
    [TextArea(1, 3)] public string fraseSinDatos = "";

    [Header("Animacion")]
    public bool animarNumeros = true;
    public float duracionNumeros = 1.2f;
    [Tooltip("Opcional: grupo para hacer aparecer el panel con un fundido.")]
    public CanvasGroup grupo;
    public float fundido = 0.5f;

    IEnumerator Start()
    {
        if (grupo != null) grupo.alpha = 0f;

        // Esperamos un frame: RankingManager.Start cierra la sesion y deja la del jugador en SesionJugadorActual.
        yield return null;

        var sesion = RankingManager.SesionJugadorActual ?? PlayerDataStore.LastSession;
        var stats = PlayerStats.Instance;

        // Textos fijos
        PonerDecisiones(sesion);
        if (puntajeText != null) puntajeText.text = sesion != null ? $"{sesion.puntajeTotal:F0} pts" : "";
        if (fraseText != null) fraseText.text = "\u201C" + Frase(sesion) + "\u201D";
        if (fraseText != null && string.IsNullOrEmpty(Frase(sesion))) fraseText.text = "";

        if (grupo != null)
        {
            float t = 0f;
            while (t < fundido) { t += Time.deltaTime; grupo.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fundido)); yield return null; }
            grupo.alpha = 1f;
        }

        // Porcentajes (con conteo animado)
        if (stats == null)
        {
            if (energiaText != null) energiaText.text = "—";
            if (memoriaText != null) memoriaText.text = "—";
            if (interferenciaText != null) interferenciaText.text = "—";
            yield break;
        }

        Coroutine a = StartCoroutine(Contar(energiaText, stats.energy, true, colorEnergiaFijo));
        Coroutine b = StartCoroutine(Contar(memoriaText, stats.memory, true, colorMemoriaFijo));
        Coroutine c = StartCoroutine(Contar(interferenciaText, stats.interference, false, colorMemoriaFijo));
        yield return a; yield return b; yield return c;
    }

    IEnumerator Contar(TMP_Text t, float valor, bool altoEsBueno, Color fijo)
    {
        if (t == null) yield break;
        valor = Mathf.Clamp(valor, 0f, 100f);
        Color final = colorearPorValor ? Semaforo(altoEsBueno ? valor : 100f - valor) : fijo;
        t.color = final;

        if (!animarNumeros || duracionNumeros <= 0f) { t.text = Mathf.RoundToInt(valor) + "%"; yield break; }

        float tt = 0f;
        while (tt < duracionNumeros)
        {
            tt += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(tt / duracionNumeros), 3f);   // frena al final
            t.text = Mathf.RoundToInt(valor * k) + "%";
            yield return null;
        }
        t.text = Mathf.RoundToInt(valor) + "%";
    }

    Color Semaforo(float bien)
    {
        if (bien >= 70f) return colorBien;
        if (bien >= 40f) return colorMedio;
        return colorMal;
    }

    void PonerDecisiones(PlayerSession s)
    {
        if (decisionesText == null) return;
        if (s == null || s.decisionesDetalle == null || s.decisionesDetalle.Count == 0) { decisionesText.text = "—"; return; }
        int total = s.decisionesDetalle.Count;
        int buenas = s.decisionesDetalle.Count(d => d.saludable);
        decisionesText.text = $"{buenas} de {total}";
    }

    string Frase(PlayerSession s)
    {
        if (s == null || s.decisionesDetalle == null || s.decisionesDetalle.Count == 0) return fraseSinDatos;

        var malas = s.decisionesDetalle.Where(d => !d.saludable).ToList();
        if (malas.Count == 0) return fraseTodasSaludables;
        if (malas.Count == s.decisionesDetalle.Count) return fraseNinguna;

        var m = malas[0];
        return fraseMixta.Replace("{momento}", m.momento).Replace("{opcion}", m.opcion);
    }
}

/* ======================= USO EN UNITY (escena Ranking) =======================
 1. En el Canvas, junto al panel del ranking, crea un panel "TuResultado" (mismo estilo: fondo oscuro,
    borde cian, tipografia Orbitron / Roboto Mono).
 2. Dentro pon estos TMP: titulo "TU RESULTADO", y por cada fila una etiqueta fija + un valor:
      Energía final:           [EnergiaValor]
      Memoria final:           [MemoriaValor]
      Decisiones conscientes:  [DecisionesValor]
    Y abajo un TMP con la frase [FraseCierre].
 3. Agrega ResultadoJugador al panel (o a un objeto vacio) y arrastra los TMP de valor.
    Opcional: arrastra el CanvasGroup del panel en 'Grupo' para el fundido.
 4. Si quieres numero de puesto en el ranking, agrega un TMP "Puesto" a cada RenglonNombres y
    arrastralo a 'Texto Puesto' del RankingItem.
*/
