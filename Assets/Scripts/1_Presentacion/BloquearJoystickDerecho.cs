using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Bloquea TODAS las acciones del joystick del mando DERECHO (girar, teleportar, mover, UI scroll, clic del stick).
/// El joystick izquierdo sigue funcionando para caminar.
///
/// Se instala SOLO en todas las escenas (no hay que agregarlo a ninguna). Funciona con cualquier rig
/// porque no toca los providers: anula en memoria los bindings "{RightHand}/thumbstick" de los Input Actions.
/// No modifica ningun archivo del proyecto (las anulaciones se quitan al salir de Play).
///
/// Para desactivarlo: BloquearJoystickDerecho.Activo = false;  (o apaga 'bloquear' en el objeto creado).
/// </summary>
public class BloquearJoystickDerecho : MonoBehaviour
{
    public static bool Activo = true;
    public bool bloquear = true;
    [Tooltip("Cada cuantos segundos se vuelve a comprobar (por si una escena carga otro asset de acciones).")]
    public float revisarCada = 2f;
    public bool logEvents = true;

    // Ruta que no coincide con ningun control: el binding queda "sin entrada"
    const string RutaNula = "<BloqueoJoystick>/ninguno";

    static BloquearJoystickDerecho instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("BloquearJoystickDerecho");
        DontDestroyOnLoad(go);
        go.AddComponent<BloquearJoystickDerecho>();
    }

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  { SceneManager.sceneLoaded += AlCargarEscena; }
    void OnDisable() { SceneManager.sceneLoaded -= AlCargarEscena; }

    IEnumerator Start()
    {
        Aplicar();
        var espera = new WaitForSecondsRealtime(Mathf.Max(0.5f, revisarCada));
        while (true)
        {
            yield return espera;
            Aplicar();
        }
    }

    void AlCargarEscena(Scene s, LoadSceneMode m) { Aplicar(); }

    void OnDestroy()
    {
        if (instancia == this) { Restaurar(); instancia = null; }
    }

    void OnApplicationQuit() { Restaurar(); }

    [ContextMenu("Aplicar bloqueo ahora")]
    public void Aplicar()
    {
        if (!Activo || !bloquear) { Restaurar(); return; }

        int nuevos = 0;
        foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            if (asset == null) continue;
            foreach (var accion in asset)
            {
                var bs = accion.bindings;
                for (int i = 0; i < bs.Count; i++)
                {
                    var b = bs[i];
                    if (b.isComposite || !EsJoystickDerecho(b.path)) continue;
                    if (b.overridePath == RutaNula) continue;
                    accion.ApplyBindingOverride(i, RutaNula);
                    nuevos++;
                }
            }
        }
        if (nuevos > 0 && logEvents)
            Debug.Log($"[BloquearJoystickDerecho] {nuevos} binding(s) del joystick derecho bloqueados.", this);
    }

    public void Restaurar()
    {
        foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            if (asset == null) continue;
            foreach (var accion in asset)
            {
                var bs = accion.bindings;
                for (int i = 0; i < bs.Count; i++)
                    if (bs[i].overridePath == RutaNula) accion.RemoveBindingOverride(i);
            }
        }
    }

    static bool EsJoystickDerecho(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string p = path.ToLowerInvariant();
        if (!p.Contains("{righthand}")) return false;
        return p.Contains("thumbstick") || p.Contains("primary2daxis") || p.Contains("joystick");
    }
}
