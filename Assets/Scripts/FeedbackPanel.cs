using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Va en el panel de retroalimentacion (uno solo, compartido por todas las comidas).
/// - Las tarjetas le pasan titulo y texto.
/// - El decide a donde va el boton Continuar, segun la ronda (desayuno, almuerzo, cena).
/// - Guarda cada decision en el JSON de la sesion.
/// </summary>
public class FeedbackPanel : MonoBehaviour
{
    [Serializable]
    public class Round
    {
        public string nombre;
        [Tooltip("Panel al que va Continuar (indice en la lista del navegador). -1 = siguiente de la lista")]
        public int continueToPanelIndex = -1;
        [Tooltip("Opcional: si escribes una escena, Continuar la carga en vez de ir a un panel")]
        public string continueToScene = "";
    }

    [Header("Textos del panel")]
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Navegacion")]
    public VRPanelNavigator navigator;
    [Tooltip("Indice de ESTE panel en la lista Panels del navegador")]
    public int thisPanelIndex = -1;
    [Tooltip("Pausa corta tras elegir, antes de abrir este panel")]
    public float openDelay = 0.6f;

    [Header("A donde va Continuar, por ronda (Element 0 = desayuno, 1 = almuerzo, 2 = cena)")]
    public Round[] rounds;

    int round = -1;   // ronda actual (0 = desayuno)

    // El navegador deja los paneles ocultos con alpha 0. Si este panel se activa
    // por otro camino (un evento On Click, por ejemplo), evitamos que quede invisible.
    void OnEnable()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null && cg.alpha < 1f) cg.alpha = 1f;
    }

    // La tarjeta llama a esto al elegir
    public void Prepare(string title, string body, int optionNumber)
    {
        round++;

        if (titleText == null) Debug.LogWarning("[FeedbackPanel] 'Title Text' esta vacio en el Inspector.", this);
        if (bodyText == null) Debug.LogWarning("[FeedbackPanel] 'Body Text' esta vacio en el Inspector.", this);
        Debug.Log($"[FeedbackPanel] Ronda {round} | titulo='{title}' | cuerpo({(body == null ? 0 : body.Length)} letras) | panel='{name}'", this);

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        PlayerDataStore.SetDecision(round, optionNumber);
    }

    public void Open()
    {
        if (navigator == null) { Debug.LogWarning("[FeedbackPanel] Falta asignar el VRPanelNavigator."); return; }
        if (thisPanelIndex < 0) { Debug.LogWarning("[FeedbackPanel] Falta poner 'This Panel Index'."); return; }
        navigator.GoTo(thisPanelIndex);
    }

    // Conectar en el On Click del boton Continuar
    public void Continue()
    {
        if (navigator == null) { Debug.LogWarning("[FeedbackPanel] Falta asignar el VRPanelNavigator."); return; }

        Round r = (rounds != null && round >= 0 && round < rounds.Length) ? rounds[round] : null;

        if (r != null && !string.IsNullOrEmpty(r.continueToScene))
        {
            SceneManager.LoadScene(r.continueToScene);
            return;
        }

        if (r != null && r.continueToPanelIndex >= 0) navigator.GoTo(r.continueToPanelIndex);
        else navigator.Next();
    }

    // Opcional: llamar al empezar una partida nueva
    public void ResetRounds() { round = -1; }
}
