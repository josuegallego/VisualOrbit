using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

/// <summary>
/// Bloquea TODAS las acciones del joystick del mando DERECHO (girar, teleportar, mover, UI scroll, clic del stick).
/// El joystick izquierdo sigue funcionando para caminar, salvo en las escenas de "escenasSinMovimiento"
/// (por defecto TutorialControl), donde tambien se bloquea para que el raycast no se vuelva loco.
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
    const string RutaNulaIzq = "<BloqueoJoystick>/ninguno-izq";

    [Header("Escenas donde TAMBIEN se bloquea el joystick izquierdo (no mover al jugador)")]
    [Tooltip("Nombres de escena. En el tutorial de controles el joystick solo se detecta, no mueve al jugador.")]
    public string[] escenasSinMovimiento = { "TutorialControl" };

    [Tooltip("Segundos tras entrar a la escena antes de fijar la posicion del jugador (deja terminar el XROriginStartPose).")]
    public float esperaAncla = 2f;

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
        if (instancia == this) { ReactivarLocomocion(); Restaurar(); instancia = null; }
    }

    void OnApplicationQuit() { ReactivarLocomocion(); Restaurar(); }

    [ContextMenu("Aplicar bloqueo ahora")]
    public void Aplicar()
    {
        if (!Activo || !bloquear) { Restaurar(); return; }

        bool bloquearIzq = EscenaSinMovimiento();
        if (bloquearIzq) { DiagnosticarLocomocion(); ApagarLocomocion(); } else ReactivarLocomocion();
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
                    if (b.isComposite) continue;

                    // Joystick izquierdo: solo bloqueado en las escenas indicadas; fuera de ellas se libera
                    if (EsJoystickIzquierdo(b.path))
                    {
                        if (bloquearIzq && b.overridePath != RutaNulaIzq) { accion.ApplyBindingOverride(i, RutaNulaIzq); nuevos++; }
                        else if (!bloquearIzq && b.overridePath == RutaNulaIzq) accion.RemoveBindingOverride(i);
                        continue;
                    }

                    if (!EsJoystickDerecho(b.path)) continue;
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
                    if ((bs[i].overridePath == RutaNula || bs[i].overridePath == RutaNulaIzq)) accion.RemoveBindingOverride(i);
            }
        }
    }

    // El rig puede traer sus acciones incrustadas en el componente (no en un asset), asi que
    // ademas de anular bindings se apagan los providers de movimiento mientras dure la escena.
    readonly List<Behaviour> apagados = new List<Behaviour>();

    void ApagarLocomocion()
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null || !mb.enabled) continue;
            if (!EsProviderDeMovimiento(mb.GetType())) continue;
            mb.enabled = false;
            apagados.Add(mb);
            if (logEvents) Debug.Log("[BloquearJoystickDerecho] Movimiento apagado: " + mb.GetType().Name + " en " + mb.gameObject.name, mb);
        }
    }

    void ReactivarLocomocion()
    {
        if (apagados.Count == 0) return;
        foreach (var mb in apagados) if (mb != null) mb.enabled = true;
        apagados.Clear();
    }

    static bool EsProviderDeMovimiento(System.Type t)
    {
        for (; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            string n = t.Name;
            // Cualquier "...MoveProvider..." (Continuous, Dynamic, GrabMove, TwoHandedGrabMove, ActionBased...)
            if (n.Contains("MoveProvider")) return true;
        }
        return false;
    }

    // Seguro final: en las escenas sin movimiento el XR Origin se queda donde esta, venga el movimiento de donde venga.
    XROrigin origen;
    Vector3 ancla;
    bool anclaLista;
    float entradaEscena;
    string escenaAncla;

    void LateUpdate()
    {
        if (!Activo || !bloquear || !EscenaSinMovimiento()) { anclaLista = false; escenaAncla = null; return; }

        string esc = SceneManager.GetActiveScene().name;
        if (escenaAncla != esc) { escenaAncla = esc; anclaLista = false; entradaEscena = Time.unscaledTime; origen = null; }

        if (origen == null) { origen = FindFirstObjectByType<XROrigin>(); if (origen == null) return; }
        Transform tr = origen.Origin != null ? origen.Origin.transform : origen.transform;

        if (!anclaLista)
        {
            if (Time.unscaledTime - entradaEscena < esperaAncla) return;
            ancla = tr.position; anclaLista = true;
            if (logEvents) Debug.Log("[BloquearJoystickDerecho] Posicion del jugador fijada en " + esc, this);
            return;
        }

        if ((tr.position - ancla).sqrMagnitude > 1e-6f)
        {
            var cc = tr.GetComponent<CharacterController>();
            if (cc != null && cc.enabled) { cc.enabled = false; tr.position = ancla; cc.enabled = true; }
            else tr.position = ancla;
        }
    }

    string ultimaEscenaDiag;
    void DiagnosticarLocomocion()
    {
        string esc = SceneManager.GetActiveScene().name;
        if (esc == ultimaEscenaDiag) return;
        ultimaEscenaDiag = esc;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string n = mb.GetType().Name;
            if (n.Contains("Provider") || n.Contains("Locomotion") || n.Contains("Teleport") || n.Contains("CharacterController"))
                Debug.Log("[BloquearJoystickDerecho] (diag) " + n + " en '" + mb.gameObject.name + "' enabled=" + mb.enabled, mb);
        }
    }

    bool EscenaSinMovimiento()
    {
        string actual = SceneManager.GetActiveScene().name;
        if (escenasSinMovimiento == null) return false;
        foreach (var n in escenasSinMovimiento)
            if (!string.IsNullOrEmpty(n) && actual == n) return true;
        return false;
    }

    static bool EsJoystickIzquierdo(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string p = path.ToLowerInvariant();
        if (!p.Contains("{lefthand}")) return false;
        return p.Contains("thumbstick") || p.Contains("primary2daxis") || p.Contains("joystick");
    }

    static bool EsJoystickDerecho(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string p = path.ToLowerInvariant();
        if (!p.Contains("{righthand}")) return false;
        return p.Contains("thumbstick") || p.Contains("primary2daxis") || p.Contains("joystick");
    }
}
