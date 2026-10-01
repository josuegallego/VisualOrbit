using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Va en el panel de retroalimentacion (uno solo, compartido por todas las comidas).
/// - Las tarjetas le pasan titulo y texto.
/// - El decide a donde va el boton Continuar, segun la ronda (desayuno, almuerzo, cena).
/// - Guarda cada decision en el JSON de la sesion.
/// No usa VRPanelNavigator.
/// </summary>
public class FeedbackPanel : MonoBehaviour
{
    [Serializable]
    public class Round
    {
        public string nombre;
        [Tooltip("Panel que se muestra al darle Continuar (ej. el panel de las 2 PM)")]
        public GameObject nextPanel;
        [Tooltip("Opcional: si escribes una escena, Continuar la carga en vez de ir a un panel")]
        public string continueToScene = "";
    }

    [Header("Textos del panel")]
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Tooltip("Pausa corta tras elegir una opcion, antes de abrir este panel")]
    public float openDelay = 0.6f;

    [Header("Despues de Continuar (Element 0 = desayuno, 1 = almuerzo, 2 = cena)")]
    public Round[] rounds;

    int round = -1;   // ronda actual (0 = desayuno)

    // La tarjeta llama a esto al elegir
    public void Prepare(string title, string body, int optionNumber)
    {
        round++;

        if (titleText == null) Debug.LogWarning("[FeedbackPanel] 'Title Text' esta vacio en el Inspector.", this);
        if (bodyText == null) Debug.LogWarning("[FeedbackPanel] 'Body Text' esta vacio en el Inspector.", this);

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        PlayerDataStore.SetDecision(round, optionNumber);
    }

    // La tarjeta llama a esto despues de la pausa: oculta el panel de decisiones y muestra este
    public void Open(GameObject decisionPanel)
    {
        PanelFader.Switch(decisionPanel, gameObject);
    }

    // Conectar en el On Click del boton Continuar (o dejar que lo haga solo, ver abajo)
    public void Continue()
    {
        Round r = (rounds != null && round >= 0 && round < rounds.Length) ? rounds[round] : null;

        if (r == null)
        {
            Debug.LogWarning("[FeedbackPanel] No hay datos en 'Rounds' para la ronda " + round);
            return;
        }

        if (!string.IsNullOrEmpty(r.continueToScene))
        {
            SceneManager.LoadScene(r.continueToScene);
            return;
        }

        PanelFader.Switch(gameObject, r.nextPanel);
    }

    // Opcional: llamar al empezar una partida nueva
    public void ResetRounds() { round = -1; }
}
