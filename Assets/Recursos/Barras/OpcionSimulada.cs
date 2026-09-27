using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpcionSimulada : MonoBehaviour
{
    [Header("A qué barra afecta esta opción")]
    public string idBarraAfectada;

    [Header("Impacto simulado (esto lo calculará el sistema de preguntas más adelante)")]
    [Tooltip("Valor positivo o negativo que se sumará al porcentaje de la barra")]
    public float impactoSimulado = 10f;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(EjecutarOpcion);
    }

    public void EjecutarOpcion()
    {
        if (GestorBarras.Instancia == null)
        {
            Debug.LogWarning("[OpcionSimulada] No hay un GestorBarras en la escena.");
            return;
        }

        GestorBarras.Instancia.AplicarCambioABarra(idBarraAfectada, impactoSimulado);
    }
}