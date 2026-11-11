using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hace que la Configuracion (Efectos, Voces, Musica) controle el audio de las escenas que NO tienen
/// un AudioManager (memorama, armar la molecula, tutorial del memorama, ranking, etc.).
///
///  - EFECTOS: todas las fuentes de audio de la escena que no estan en bucle (acierto, error, confeti, flujo...)
///             -> su volumen = volumen original x slider "EfectosSlider".
///  - MUSICA:  las fuentes en bucle (y las del Video Player) -> se silencian/activan con el interruptor "ToggleMusic".
///  - VOZ:     solo las que marques con SettingsAudioChannel (canal Voz) -> slider "VocesSlider".
///
/// NO necesita configuracion: se crea solo al iniciar el juego y vive en todas las escenas.
/// Lee los valores guardados (SettingsManager o, si la escena se abre sola en el editor, PlayerPrefs)
/// y tambien reacciona en vivo si mueves esos sliders con el panel abierto.
/// Las escenas que ya tienen AudioManager se dejan como estaban.
/// </summary>
public class SettingsAudioRouter : MonoBehaviour
{
    public static SettingsAudioRouter Instance { get; private set; }

    [Header("Nombres en la jerarquia")]
    public string efectosSliderName = "EfectosSlider";
    public string vocesSliderName = "VocesSlider";
    public string musicaToggleName = "ToggleMusic";

    [Header("Comportamiento")]
    [Tooltip("Si la escena ya tiene un AudioManager, este script no hace nada en ella.")]
    public bool saltarEscenasConAudioManager = true;
    [Tooltip("Las fuentes en bucle (y las del Video Player) cuentan como musica; el resto como efectos.")]
    public bool loopsSonMusica = true;
    public bool mostrarLogs = true;
    [Tooltip("DESMARCALO para apagar este script: devuelve los volumenes originales y deja de tocar el audio (sirve para saber si el problema es de este script).")]
    public bool activo = true;

    class Entry { public float baseVol; public AudioChannelType canal; }

    readonly Dictionary<AudioSource, Entry> entries = new Dictionary<AudioSource, Entry>();
    readonly HashSet<Object> hooked = new HashSet<Object>();

    float efectos = 1f, voz = 1f;
    bool musica = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<SettingsAudioRouter>() != null) return;
        new GameObject("SettingsAudioRouter").AddComponent<SettingsAudioRouter>();
    }

    public static void Refresh()
    {
        if (Instance != null) Instance.Rescan();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSaved();
        Rescan();
        StartCoroutine(Maintain());
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Quita fuentes de la escena anterior y vuelve a los valores guardados
        var dead = new List<AudioSource>();
        foreach (var k in entries.Keys) if (k == null) dead.Add(k);
        foreach (var k in dead) entries.Remove(k);
        hooked.RemoveWhere(x => x == null);

        LoadSaved();
        Rescan();
    }

    void LoadSaved()
    {
        var sm = SettingsManager.Instance;
        efectos = sm != null ? sm.CurrentEffectsVolume : PlayerPrefs.GetFloat("Settings_EffectsVolume", 1f);
        voz = sm != null ? sm.CurrentVoiceVolume : PlayerPrefs.GetFloat("Settings_VoiceVolume", 1f);
        musica = sm != null ? sm.CurrentSpatialMusic : PlayerPrefs.GetInt("Settings_SpatialMusic", 1) == 1;
        if (mostrarLogs)
            Debug.Log($"[SettingsAudioRouter] Valores guardados -> Efectos {efectos:F2}, Voces {voz:F2}, Musica {(musica ? "ON" : "OFF")}. " +
                      "Si Efectos es 0 o Musica es OFF, por eso no suena (menu Tools > Audio > Restablecer ajustes de audio).");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Audio/Restablecer ajustes de audio (Efectos 1, Voces 1, Musica ON)")]
    static void ResetAudioPrefs()
    {
        PlayerPrefs.SetFloat("Settings_EffectsVolume", 1f);
        PlayerPrefs.SetFloat("Settings_VoiceVolume", 1f);
        PlayerPrefs.SetInt("Settings_SpatialMusic", 1);
        PlayerPrefs.Save();
        Debug.Log("[SettingsAudioRouter] Ajustes de audio restablecidos. Detén Play y vuelve a darle Play.");
    }
#endif

    IEnumerator Maintain()
    {
        for (int i = 0; i < 3; i++) yield return null;
        var wait = new WaitForSecondsRealtime(1f);
        while (true)
        {
            if (!activo) { RestoreAll(); yield return wait; continue; }
            Rescan();
            HookControls();
            yield return wait;
        }
    }

    // ------------------------------------------------------------ Audio

    void RestoreAll()
    {
        foreach (var kv in entries)
        {
            var s = kv.Key;
            if (s == null) continue;
            if (kv.Value.canal == AudioChannelType.Musica) s.mute = false;
            else s.volume = kv.Value.baseVol;
        }
    }

    void Rescan()
    {
        if (!activo) return;
        if (saltarEscenasConAudioManager &&
            FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include) != null)
            return;

        var all = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool added = false;
        foreach (var s in all)
        {
            if (s == null || entries.ContainsKey(s)) continue;

            var mark = s.GetComponent<SettingsAudioChannel>();
            if (mark != null && mark.ignorar) continue;

            AudioChannelType c;
            if (mark != null) c = mark.canal;
            else if (loopsSonMusica && (s.loop || s.GetComponent<UnityEngine.Video.VideoPlayer>() != null)) c = AudioChannelType.Musica;
            else c = AudioChannelType.Efectos;

            entries[s] = new Entry { baseVol = s.volume, canal = c };
            added = true;
            if (mostrarLogs)
                Debug.Log($"[SettingsAudioRouter] '{s.gameObject.name}' -> {c} (volumen base {s.volume:F2})", s);
        }

        if (added) ApplyAll();
    }

    void ApplyAll()
    {
        if (!activo) return;
        foreach (var kv in entries)
        {
            var s = kv.Key;
            if (s == null) continue;
            switch (kv.Value.canal)
            {
                case AudioChannelType.Efectos: s.volume = kv.Value.baseVol * efectos; break;
                case AudioChannelType.Voz: s.volume = kv.Value.baseVol * voz; break;
                case AudioChannelType.Musica: s.mute = !musica; break;
            }
        }
    }

    // ------------------------------------------------------------ Cambios en vivo desde el panel

    void HookControls()
    {
        foreach (var sl in Resources.FindObjectsOfTypeAll<Slider>())
        {
            if (sl == null || hooked.Contains(sl)) continue;
            if (!sl.gameObject.scene.IsValid() || !sl.gameObject.scene.isLoaded) continue;

            if (sl.name == efectosSliderName) { sl.onValueChanged.AddListener(v => { efectos = v; ApplyAll(); }); hooked.Add(sl); }
            else if (sl.name == vocesSliderName) { sl.onValueChanged.AddListener(v => { voz = v; ApplyAll(); }); hooked.Add(sl); }
        }

        foreach (var t in Resources.FindObjectsOfTypeAll<Toggle>())
        {
            if (t == null || hooked.Contains(t)) continue;
            if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;

            if (t.name == musicaToggleName) { t.onValueChanged.AddListener(v => { musica = v; ApplyAll(); }); hooked.Add(t); }
        }
    }
}
