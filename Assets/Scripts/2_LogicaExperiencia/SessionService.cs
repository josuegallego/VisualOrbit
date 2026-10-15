// =====================================================================
// CAPA: Logica de Experiencia
// Unica puerta por la que la Presentacion (UI, botones, paneles) accede
// a la sesion del jugador. La Presentacion NO debe llamar a PlayerDataStore.
//   Presentacion -> SessionService -> PlayerDataStore (Datos)
// =====================================================================
public static class SessionService
{
    /// <summary>Crea y activa una sesion nueva para el nombre dado.</summary>
    public static PlayerSession Start(string nombre) => PlayerDataStore.AddSession(nombre);

    /// <summary>Cantidad de sesiones guardadas.</summary>
    public static int TotalSessions => PlayerDataStore.Current.sesiones.Count;

    /// <summary>Registra la opcion elegida en una jornada.</summary>
    public static void RecordDecision(int index, int value) => PlayerDataStore.SetDecision(index, value);

    /// <summary>Guarda el ultimo panel alcanzado por el jugador.</summary>
    public static void SetPanel(int panelIndex) => PlayerDataStore.SetActiveSessionPanel(panelIndex);

    /// <summary>Cierra la sesion activa (si existe) y guarda en disco.</summary>
    public static void Finish(bool completed = true)
    {
        if (PlayerDataStore.ActiveSession != null)
            PlayerDataStore.CloseActiveSession(completed);
        PlayerDataStore.Save();
    }
}
