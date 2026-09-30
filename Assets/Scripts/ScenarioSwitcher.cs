using System;
using UnityEngine;

/// <summary>
/// Cambia el escenario 3D (cuarto, salon, pasillo...) segun el panel que muestra
/// el VRPanelNavigator. Conectalo en el evento On Panel Changed (Int32) del navegador.
///
/// Cada entrada dice: "desde el panel N en adelante, muestra este escenario".
/// Se queda activo el de mayor firstPanelIndex que sea &lt;= al panel actual.
/// Tambien guarda el ultimo panel alcanzado en el JSON de la sesion.
/// </summary>
public class ScenarioSwitcher : MonoBehaviour
{
    [Serializable]
    public class Scenario
    {
        public string nombre;
        [Tooltip("Indice del primer panel (en la lista del navegador) donde aparece este escenario")]
        public int firstPanelIndex;
        public GameObject root;
    }

    public Scenario[] scenarios;
    public bool saveProgress = true;

    // Conectar en On Panel Changed (Int32) -> ScenarioSwitcher.OnPanelChanged (dynamic int)
    public void OnPanelChanged(int panelIndex)
    {
        Scenario best = null;
        foreach (var s in scenarios)
            if (s.firstPanelIndex <= panelIndex && (best == null || s.firstPanelIndex > best.firstPanelIndex))
                best = s;

        foreach (var s in scenarios)
            if (s.root != null) s.root.SetActive(s == best);

        if (saveProgress) PlayerDataStore.SetActiveSessionPanel(panelIndex);
    }
}
