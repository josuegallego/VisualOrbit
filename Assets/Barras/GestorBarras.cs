using System.Collections.Generic;
using UnityEngine;

public class GestorBarras : MonoBehaviour
{
    public static GestorBarras Instancia { get; private set; }

    [Tooltip("Arrastra aquí todas las barras (BarraAlimentaria) de la escena")]
    public List<BarraAlimentaria> barras = new List<BarraAlimentaria>();

    private Dictionary<string, BarraAlimentaria> mapaBarras;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        mapaBarras = new Dictionary<string, BarraAlimentaria>();
        foreach (var barra in barras)
        {
            if (barra == null) continue;

            if (!mapaBarras.ContainsKey(barra.idBarra))
                mapaBarras.Add(barra.idBarra, barra);
            else
                Debug.LogWarning($"[GestorBarras] idBarra duplicado: '{barra.idBarra}'");
        }
    }

    /// <summary>Suma/resta un valor a la barra con ese id.</summary>
    public void AplicarCambioABarra(string idBarra, float delta)
    {
        if (mapaBarras.TryGetValue(idBarra, out BarraAlimentaria barra))
        {
            barra.AplicarCambio(delta);
        }
        else
        {
            Debug.LogWarning($"[GestorBarras] No existe una barra con id '{idBarra}'");
        }
    }

    /// <summary>Fija el valor absoluto de la barra con ese id.</summary>
    public void SetPorcentajeDeBarra(string idBarra, float valor)
    {
        if (mapaBarras.TryGetValue(idBarra, out BarraAlimentaria barra))
        {
            barra.SetPorcentaje(valor);
        }
        else
        {
            Debug.LogWarning($"[GestorBarras] No existe una barra con id '{idBarra}'");
        }
    }
}