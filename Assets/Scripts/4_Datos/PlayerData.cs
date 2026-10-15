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
    // Ejemplo: 0 = no respondido, 1..5 = opción elegida.
    public List<int> decisiones = new List<int>();

    // --- Minijuegos ---
    public float puntajeMinijuego1 = 0f;
    public float puntajeMinijuego2 = 0f;

    // --- Puntaje final (para el ranking) ---
    public float puntajeTotal = 0f;

    // Si necesitas más campos, agrégalos aquí y los nuevos JSON
    // seguirán siendo compatibles (JsonUtility los rellena por defecto).
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
/// Maneja la carga y guardado de PlayerData en Application.persistentDataPath.
/// Es estático, por lo que sobrevive a los cambios de escena mientras la app esté abierta.
/// </summary>
public static class PlayerDataStore
{
    private const string FileName = "player_data.json";

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

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
            string json = JsonUtility.ToJson(cached, prettyPrint: true);
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
            puntajeMinijuego1 = 0f,
            puntajeMinijuego2 = 0f,
            puntajeTotal = 0f
        };

        Current.sesiones.Add(session);
        ActiveSession = session;
        Save();
        return session;
    }

    /// <summary>Cierra la sesión activa, calcula duración y guarda.</summary>
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
            s.duracionSegundos = (float)(fin - ini).TotalSeconds;
        }

        Save();
        Debug.Log($"[PlayerDataStore] Sesión cerrada: '{s.nombre}' | duración: {s.duracionSegundos:F1}s | completada: {completada}");

        ActiveSession = null;
    }

    // ---------------------------------------------------------------- Actualizar datos de la sesión activa

    /// <summary>Actualiza el último panel alcanzado por la sesión activa.</summary>
    public static void SetActiveSessionPanel(int panelIndex)
    {
        if (ActiveSession == null) return;
        ActiveSession.ultimoPanel = panelIndex;
        Save();
    }

    /// <summary>Guarda una decisión alimentaria (agrega o reemplaza en el índice dado).</summary>
    public static void SetDecision(int index, int valor)
    {
        if (ActiveSession == null) return;

        while (ActiveSession.decisiones.Count <= index)
            ActiveSession.decisiones.Add(0);

        ActiveSession.decisiones[index] = valor;
        Save();
    }

    /// <summary>Actualiza el puntaje de un minijuego (1 o 2).</summary>
    public static void SetMinigameScore(int minigame, float score)
    {
        if (ActiveSession == null) return;

        if (minigame == 1) ActiveSession.puntajeMinijuego1 = score;
        else if (minigame == 2) ActiveSession.puntajeMinijuego2 = score;
        else Debug.LogWarning($"[PlayerDataStore] Minijuego inválido: {minigame}");

        Save();
    }

    /// <summary>Recalcula el puntaje total (o pon el que quieras).</summary>
    public static void RecalculateTotalScore()
    {
        if (ActiveSession == null) return;
        ActiveSession.puntajeTotal = ActiveSession.puntajeMinijuego1 + ActiveSession.puntajeMinijuego2;
        Save();
    }

    // ---------------------------------------------------------------- Ranking

    /// <summary>Devuelve el histórico ordenado por puntaje descendente (para el ranking).</summary>
    public static List<PlayerSession> GetRanking(bool soloCompletadas = true)
    {
        var lista = Current.sesiones;

        if (soloCompletadas)
            lista = lista.Where(s => s.experienciaCompletada).ToList();

        return lista.OrderByDescending(s => s.puntajeTotal).ToList();
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