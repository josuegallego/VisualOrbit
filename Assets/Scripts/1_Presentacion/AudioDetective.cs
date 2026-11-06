using System.Text;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Herramienta de DIAGNOSTICO. Ponla en cualquier objeto de la escena, dale Play y mira la Consola.
/// Lista todo lo que puede estar produciendo sonido: Audio Sources (tambien apagados),
/// Video Players (el audio directo suena SIN Audio Source) y la ubicacion de cada uno.
/// Cuando encuentres al culpable, quitale el componente y borra este script.
/// </summary>
public class AudioDetective : MonoBehaviour
{
    public float repeatEvery = 3f;
    float next;

    void Start()
    {
        Report("INICIO");
        next = Time.time + repeatEvery;
    }

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + repeatEvery;
        Report("CADA " + repeatEvery + "s");
    }

    [ContextMenu("Reportar ahora")]
    void ReportNow() => Report("MANUAL");

    void Report(string when)
    {
        var sb = new StringBuilder($"[AudioDetective] ({when})\n");

        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"AudioSources en escena: {sources.Length}");
        foreach (var a in sources)
            sb.AppendLine($"  - {Path(a.transform)} | clip={(a.clip ? a.clip.name : "NINGUNO")} | " +
                          $"sonando={a.isPlaying} | habilitado={a.enabled} | activo={a.gameObject.activeInHierarchy} | volumen={a.volume}");

        var videos = FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"VideoPlayers en escena: {videos.Length}");
        foreach (var v in videos)
            sb.AppendLine($"  - {Path(v.transform)} | clip={(v.clip ? v.clip.name : "NINGUNO")} | url={v.url} | " +
                          $"audio={v.audioOutputMode} | reproduciendo={v.isPlaying} | activo={v.gameObject.activeInHierarchy}");

        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"AudioListeners: {listeners.Length}");

        Debug.Log(sb.ToString(), this);
    }

    static string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
