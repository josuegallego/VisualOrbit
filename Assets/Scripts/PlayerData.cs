using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Datos del jugador que se persisten en disco en formato JSON.
/// </summary>
[Serializable]
public class PlayerData
{
    public string nombre = "";
    public string fechaInicio = "";
    public int ultimoPanel = 0;

    // Aquí puedes ir agregando más campos sin romper el JSON existente.
}

/// <summary>
/// Maneja la carga y guardado de PlayerData en Application.persistentDataPath.
/// Es estático para poder llamarlo desde cualquier parte sin referencias.
/// </summary>
public static class PlayerDataStore
{
    private const string FileName = "player_data.json";

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    private static PlayerData cached;

    /// <summary>Devuelve los datos actuales (los carga de disco si aún no están en memoria).</summary>
    public static PlayerData Current
    {
        get
        {
            if (cached == null) cached = Load();
            return cached;
        }
    }

    /// <summary>Guarda los datos actuales en disco.</summary>
    public static void Save()
    {
        if (cached == null) return;

        try
        {
            string json = JsonUtility.ToJson(cached, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[PlayerDataStore] Guardado en: {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Error al guardar: {e.Message}");
        }
    }

    /// <summary>Carga los datos de disco (o devuelve unos nuevos si no existen).</summary>
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
                    Debug.Log($"[PlayerDataStore] Cargado desde: {FilePath}");
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

    /// <summary>Borra el archivo guardado (útil para pruebas).</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            cached = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerDataStore] Error al borrar: {e.Message}");
        }
    }

    /// <summary>Atajo: escribe el nombre y guarda.</summary>
    public static void SetNombre(string nombre)
    {
        Current.nombre = nombre;
        Save();
    }
}