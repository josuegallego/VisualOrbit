using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Una sesión individual de un estudiante con todos los datos de su partida.
/// </summary>
[Serializable]
public class PlayerSession
{
    // --- Identificación ---
    public string nombre = "";
    public string fechaInicio = "";
    public string fechaFin = "";
    public float duracionSegundos = 0f;

    // --- Progreso ---
    public int ultimoPanel = 0;
    public bool experienciaCompletada = false;

    // --- Decisiones alimentarias ---
    public List<int> decisiones = new List<int>();

    // --- Minijuegos (Tiempos y si se completaron) ---
    public float tiempoMemorama = 0f;
    public bool memoramaCompletado = false;

    public float tiempoMolecula = 0f;
    public bool moleculaCompletado = false;

    // --- Puntaje final (para el ranking) ---
    public float puntajeTotal = 0f;
}

/// <summary>
/// Contenedor raíz del JSON. Guarda TODAS las sesiones en una lista.
/// </summary>
[Serializable]
public class PlayerData
{
    public List<PlayerSession> sesiones = new List<PlayerSession>();
}

/// <summary>
/// Maneja la carga y guardado de PlayerData.
/// Es estático, por lo que sobrevive a los cambios de escena mientras la app esté abierta.
/// </summary>
public static class PlayerDataStore
{
    private const string FileName = "player_data.json";

    // ------------------------------------------------------------ Configuración del puntaje
    // Ajusta estos valores a tu gusto.
    private const float PuntosBaseMinijuego = 50f;   // Por completar un minijuego
    private const float BonoMaxRapidez = 50f;        // Extra máximo por terminar rápido
    private const float SegundosParaBonoCero = 100f; // Si tarda esto o más, no hay bono de rapidez
    private const float BonoExperienciaCompleta = 50f;

    // ------------------------------------------------------------ Ruta del archivo
    private static string cachedPath;

    private static string FilePath
    {
        get
        {
            if (!string.IsNullOrEmpty(cachedPath)) return cachedPath;
            cachedPath = ResolvePath();
            return cachedPath;
        }
    }

    private static string ResolvePath()
    {
        // 1) Escritorio
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (!string.IsNullOrEmpty(desktop))
            {
                string path = Path.Combine(desktop, FileName);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, "test");
                File.Delete(tmp);
                return path;
            }
        }
        catch { /* pasa al siguiente */ }

        // 2) Documentos
        try
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(docs))
                return Path.Combine(docs, FileName);
        }
        catch { /* pasa al siguiente */ }

        // 3) Carpeta de Unity
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    // ------------------------------------------------------------ Estado en memoria
    private static PlayerData cached;

    /// <summary>Datos actuales (los carga de disco si aún no están en memoria).</summary>
    public static PlayerData Current
    {
        get
        {
            if (cached == null) cached = Load();
            return cached;
        }
    }

    /// <summary>Sesión ACTIVA en este momento (el jugador que está jugando ahora).</summary>
    public static PlayerSession ActiveSession { get; private set; }

    /// <summary>Última sesión del histórico (puede no ser la activa).</summary>
    public static PlayerSession LastSession
    {
        get
        {
            var list = Current.sesiones;
            return list.Count > 0 ? list[list.Count - 1] : null;
        }
    }

    // ---------------------------------------------------------------- Persistencia

    public static void Save()
    {
        if (cached == null) return;

        try
        {
            string json = JsonUtility.ToJson(cached, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[PlayerDataStore] Guardado: {cached.sesiones.Count} sesiones en {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Error al guardar: {e.Message}");
        }
    }

    public static PlayerData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<PlayerData>(json);
                if (data != null)
                {
                    if (data.sesiones == null) data.sesiones = new List<PlayerSession>();
                    Debug.Log($"[PlayerDataStore] Cargado: {data.sesiones.Count} sesiones desde {FilePath}");
                    return data;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Error al cargar: {e.Message}");
        }

        return new PlayerData();
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            cached = null;
            ActiveSession = null;
            Debug.Log("[PlayerDataStore] Archivo borrado.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Error al borrar: {e.Message}");
        }
    }

    // ---------------------------------------------------------------- Sesión activa

    /// <summary>Crea una nueva sesión con el nombre dado, la añade al histórico y la marca como activa.</summary>
    public static PlayerSession AddSession(string nombre)
    {
        var session = new PlayerSession
        {
            nombre = nombre,
            fechaInicio = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            fechaFin = "",
            duracionSegundos = 0f,
            ultimoPanel = 0,
            experienciaCompletada = false,
            decisiones = new List<int>(),
            tiempoMemorama = 0f,
            memoramaCompletado = false,
            tiempoMolecula = 0f,
            moleculaCompletado = false,
            puntajeTotal = 0f
        };

        Current.sesiones.Add(session);
        ActiveSession = session;
        Save();
        return session;
    }

    /// <summary>
    /// Cierra la sesión activa: calcula duración y puntaje total, y guarda.
    /// Llamar al terminar la experiencia (botón Continuar) antes de ir al Ranking.
    /// </summary>
    public static void CloseActiveSession(bool completada = true)
    {
        var s = ActiveSession;
        if (s == null)
        {
            Debug.LogWarning("[PlayerDataStore] CloseActiveSession: no hay sesión activa.");
            return;
        }

        if (!string.IsNullOrEmpty(s.fechaFin))
        {
            Debug.Log("[PlayerDataStore] La sesión ya estaba cerrada.");
            ActiveSession = null;
            return;
        }

        s.fechaFin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        s.experienciaCompletada = completada;

        if (DateTime.TryParse(s.fechaInicio, out var ini) &&
            DateTime.TryParse(s.fechaFin, out var fin))
        {
            s.duracionSegundos = Mathf.Max(0f, (float)(fin - ini).TotalSeconds);
        }

        // El puntaje se calcula aquí, con la sesión ya cerrada
        ComputeScore(s);

        Save();
        Debug.Log($"[PlayerDataStore] Sesión cerrada: '{s.nombre}' | duración: {s.duracionSegundos:F1}s | puntaje: {s.puntajeTotal:F0} | completada: {completada}");

        ActiveSession = null;
    }

    /// <summary>Seguro: si hay una sesión abierta, la cierra. Si no, no hace nada.</summary>
    public static void CloseActiveSessionIfOpen(bool completada = true)
    {
        if (ActiveSession != null)
            CloseActiveSession(completada);
    }

    // ---------------------------------------------------------------- Actualizar datos de la sesión activa

    public static void SetActiveSessionPanel(int panelIndex)
    {
        if (ActiveSession == null) return;
        ActiveSession.ultimoPanel = panelIndex;
        Save();
    }

    public static void SetDecision(int index, int valor)
    {
        if (ActiveSession == null) return;

        while (ActiveSession.decisiones.Count <= index)
            ActiveSession.decisiones.Add(0);

        ActiveSession.decisiones[index] = valor;
        Save();
    }

    public static void SetTiempoMemorama(float tiempo, bool completado)
    {
        if (ActiveSession == null) return;
        ActiveSession.tiempoMemorama = tiempo;
        ActiveSession.memoramaCompletado = completado;
        ComputeScore(ActiveSession); // puntaje parcial siempre actualizado
        Save();
    }

    public static void SetTiempoMolecula(float tiempo, bool completado)
    {
        if (ActiveSession == null) return;
        ActiveSession.tiempoMolecula = tiempo;
        ActiveSession.moleculaCompletado = completado;
        ComputeScore(ActiveSession); // puntaje parcial siempre actualizado
        Save();
    }

    // ---------------------------------------------------------------- Puntaje

    /// <summary>Recalcula el puntaje de la sesión activa (se mantiene por compatibilidad).</summary>
    public static void RecalculateTotalScore()
    {
        if (ActiveSession == null) return;
        ComputeScore(ActiveSession);
        Save();
    }

    /// <summary>
    /// Puntaje por minijuego completado: 50 base + hasta 50 por rapidez.
    /// Más 50 si terminó toda la experiencia. Máximo total: 250.
    /// </summary>
    private static void ComputeScore(PlayerSession s)
    {
        float score = 0f;

        if (s.memoramaCompletado)
            score += PuntosMinijuego(s.tiempoMemorama);

        if (s.moleculaCompletado)
            score += PuntosMinijuego(s.tiempoMolecula);

        if (s.experienciaCompletada)
            score += BonoExperienciaCompleta;

        s.puntajeTotal = score;
    }

    private static float PuntosMinijuego(float tiempo)
    {
        float rapidez = Mathf.Clamp01(1f - tiempo / SegundosParaBonoCero);
        return PuntosBaseMinijuego + BonoMaxRapidez * rapidez;
    }

    // ---------------------------------------------------------------- Ranking

    /// <summary>
    /// Devuelve los usuarios ordenados por puntaje descendente.
    /// En empate, gana el que tardó menos.
    /// </summary>
    public static List<PlayerSession> GetRanking(bool soloCompletadas = false)
    {
        IEnumerable<PlayerSession> lista = Current.sesiones;

        if (soloCompletadas)
            lista = lista.Where(s => s.experienciaCompletada);

        return lista.OrderByDescending(s => s.puntajeTotal)
                    .ThenBy(s => s.duracionSegundos)
                    .ToList();
    }

    /// <summary>Imprime por consola todo el histórico (debug).</summary>
    public static void PrintAllSessions()
    {
        Debug.Log($"[Histórico] {Current.sesiones.Count} sesiones:");
        foreach (var s in Current.sesiones)
        {
            string dur = s.duracionSegundos > 0f ? $"{s.duracionSegundos:F1}s" : "(en curso)";
            Debug.Log($"  - {s.nombre} | {s.fechaInicio} | dur: {dur} | completada: {s.experienciaCompletada} | puntaje: {s.puntajeTotal}");
        }
    }
}