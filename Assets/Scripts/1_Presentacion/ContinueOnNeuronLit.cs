using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el boton Continuar cuando la neurona se enciende (verde) y, al pulsarlo,
/// carga la siguiente escena con el SceneLoader que ya existe en el proyecto.
///
/// Ponlo en un objeto vacio FUERA de NeuralNetwork (por ejemplo "ContinueGate").
/// El boton NO debe ser hijo de NeuralNetwork ni del propio objeto con este script.
/// No configures nada en el On Click del boton: este script lo conecta solo.
/// </summary>
public class ContinueOnNeuronLit : MonoBehaviour
{
    [Header("Origen (usa UNO de los dos)")]
    [Tooltip("Preferido: el boton aparece cuando TODAS las neuronas estan encendidas.")]
    public NeuronProgress progress;
    [Tooltip("Alternativa: el boton aparece cuando esta neurona se enciende.")]
    public NeuronController neuron;
    [Tooltip("Espera tras encenderse la neurona, para que termine la transicion a verde.")]
    public float showDelay = 1.2f;
    [Tooltip("Si la neurona se apaga, vuelve a ocultar el boton.")]
    public bool hideWhenUnlit = true;

    [Header("Boton Continuar")]
    public Button continueButton;

    [Header("Siguiente escena (usa SceneLoader)")]
    public SceneLoader sceneLoader;
    [Tooltip("Vacio = siguiente escena de Build Settings (LoadNextScene). Con nombre = LoadSceneByName.")]
    public string sceneName = "";

    bool loading;
    Coroutine showRoutine;

    void Awake()
    {
        if (continueButton == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Continue Button'.", this);
            return;
        }

        continueButton.gameObject.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnEnable()
    {
        if (progress != null)
        {
            progress.ProgressChanged += OnProgressChanged;
            OnProgressChanged(progress.Progress01);   // evalua el estado actual
            return;
        }

        if (neuron == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Progress' o 'Neuron'.", this);
            return;
        }

        neuron.LitChanged += OnLitChanged;
        if (neuron.IsLit) OnLitChanged(true);
    }

    void OnDisable()
    {
        if (progress != null) progress.ProgressChanged -= OnProgressChanged;
        if (neuron != null) neuron.LitChanged -= OnLitChanged;
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
    }

    void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    void OnProgressChanged(float value)
    {
        OnLitChanged(progress != null && progress.IsComplete);
    }

    void OnLitChanged(bool lit)
    {
        if (continueButton == null) return;

        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }

        if (lit)
        {
            showRoutine = StartCoroutine(ShowAfterDelay());
        }
        else if (hideWhenUnlit && !loading)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    IEnumerator ShowAfterDelay()
    {
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = !loading;
        showRoutine = null;
        Debug.Log("[ContinueOnNeuronLit] Boton Continuar visible.", this);
    }

    void OnContinueClicked()
    {
        if (loading) return;   // evita pulsaciones repetidas

        if (sceneLoader == null)
        {
            Debug.LogError("[ContinueOnNeuronLit] Falta 'Scene Loader'.", this);
            return;
        }

        loading = true;
        continueButton.interactable = false;

        if (string.IsNullOrEmpty(sceneName)) sceneLoader.LoadNextScene();
        else sceneLoader.LoadSceneByName(sceneName);
    }
}