using UnityEngine;

/// <summary>
/// Capa delgada entre el minijuego y los sistemas existentes (PlayerStats y PlayerDataStore).
/// Es el UNICO lugar del minijuego que conoce esas clases: si algo cambia alli,
/// solo se toca este archivo.
/// </summary>
public static class NutritionPerformance
{
    /// <summary>
    /// Lee el desempeno nutricional. Devuelve false si no hay PlayerStats en la escena
    /// (en ese caso performance01 = 1, es decir, condiciones favorables).
    /// performance01: 0 = muy mal, 1 = muy bien. Es 1 - PlayerStats.Difficulty01().
    /// </summary>
    public static bool TryRead(out float performance01, out float difficulty01)
    {
        var stats = PlayerStats.Instance;
        if (stats == null)
        {
            performance01 = 1f;
            difficulty01 = 0f;
            return false;
        }

        difficulty01 = Mathf.Clamp01(stats.Difficulty01());
        performance01 = 1f - difficulty01;
        return true;
    }

    /// <summary>Texto para depurar: decisiones de las tres jornadas de la sesion actual.</summary>
    public static string DescribeDecisions()
    {
        bool hayActiva = PlayerDataStore.ActiveSession != null;
        PlayerSession s = PlayerDataStore.ActiveSession ?? PlayerDataStore.LastSession;
        if (s == null) return "sin sesion guardada";

        string cruda = s.decisiones != null ? string.Join(",", s.decisiones) : "";
        string origen = hayActiva ? "sesion ACTIVA" : "SIN sesion activa (uso la ultima guardada)";

        return $"{origen} '{s.nombre}' (inicio {s.fechaInicio}): " +
               $"desayuno={Get(s, 0)}, almuerzo={Get(s, 1)}, cena={Get(s, 2)} | decisiones crudas=[{cruda}]";
    }

    /// <summary>Texto para depurar: valores actuales de los tres indicadores.</summary>
    public static string DescribeStats()
    {
        var s = PlayerStats.Instance;
        if (s == null) return "sin PlayerStats";
        return $"energia={s.energy:F0}, interferencia={s.interference:F0}, memoria={s.memory:F0}";
    }

    static int Get(PlayerSession s, int index)
    {
        return s.decisiones != null && index < s.decisiones.Count ? s.decisiones[index] : 0;
    }
}