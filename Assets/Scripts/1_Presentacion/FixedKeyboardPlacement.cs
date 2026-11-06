using UnityEngine;

/// <summary>
/// Coloca el teclado VR (el de ejemplo de XRI) SIEMPRE a la misma distancia, altura e inclinacion
/// respecto a la CAMARA del jugador, cada vez que se abre. Asi aparece bien aunque el jugador este en otro
/// sitio o mire en otra direccion.
///
/// Se mueve el objeto "Keyboard Canvas Root" directamente en el MUNDO (no depende de los valores locales
/// de sus padres), por eso siempre queda igual.
///
/// COMO AJUSTARLO:
///   1. Dale Play, abre el teclado y mueve "Keyboard Canvas Root" hasta que quede perfecto para ti.
///   2. Menu del componente (tres puntos) -> "Capturar posicion actual". Eso calcula distancia, altura,
///      lado e inclinacion respecto a tu camara.
///   3. Copy Component, detén Play, Paste Component Values. Guarda la escena.
/// </summary>
[DefaultExecutionOrder(1000)]
public class FixedKeyboardPlacement : MonoBehaviour
{
    [Header("Posicion respecto a la camara")]
    [Tooltip("Metros hacia delante, en horizontal, desde la cabeza.")]
    public float distance = 0.6f;
    [Tooltip("Metros hacia abajo (-) o arriba (+) respecto a los ojos.")]
    public float heightOffset = -0.35f;
    [Tooltip("Metros a la derecha (+) o izquierda (-).")]
    public float sideOffset = 0f;
    [Tooltip("Inclinacion del teclado en grados (positivo = la parte de arriba se aleja, como un atril).")]
    public float tilt = 11.65f;
    [Tooltip("Giro horizontal respecto a mirar de frente.")]
    public float yawOffset = 0f;

    [Header("Objeto que se mueve")]
    public string canvasRootName = "Keyboard Canvas Root";
    [Tooltip("ENCENDIDO (recomendado): el teclado se queda fijo en esa posicion todo el rato que este abierto, " +
             "aunque otro script intente moverlo. Para moverlo y capturar usa 'Soltar teclado' en el menu del componente.")]
    public bool keepFixed = true;
    [Tooltip("Fotogramas tras abrir el teclado en los que se fuerza la posicion.")]
    public int forceFramesOnOpen = 5;

    [Header("Ocultar partes del teclado al abrirlo")]
    public string[] hideChildNames = { "Interaction Affordance", "Right Scale Handle", "Left Scale Handle" };

    [Header("Depuracion")]
    public bool logEvents = true;

    Transform keyboardTransform;
    Transform canvasRoot;
    bool warned, wasOpen;
    int framesLeft;
    bool released;   // solo en memoria: si es true no se fuerza la pose hasta la proxima apertura
    Vector3 cachedPos;
    Quaternion cachedRot;

    // ------------------------------------------------------------ busqueda

    static Transform FindDeep(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    static Transform PlayerCamera()
    {
        var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null && origin.Camera != null) return origin.Camera.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }

    bool Resolve()
    {
        if (keyboardTransform == null)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "XRKeyboard") { keyboardTransform = mb.transform; break; }
            }
        }

        if (keyboardTransform == null) return false;

        if (canvasRoot == null) canvasRoot = FindDeep(keyboardTransform, canvasRootName);

        if (canvasRoot == null)
        {
            if (logEvents && !warned)
            {
                warned = true;
                Debug.LogWarning($"[FixedKeyboard] El teclado no tiene un objeto llamado '{canvasRootName}'.", this);
            }
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------ calculo de la pose

    static Vector3 FlatForward(Transform cam)
    {
        Vector3 f = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
        if (f.sqrMagnitude < 0.0001f) f = Vector3.ProjectOnPlane(cam.up, Vector3.up);   // mirando recto arriba o abajo
        return f.normalized;
    }

    void ComputeTarget(Transform cam)
    {
        Vector3 fwd = FlatForward(cam);
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        cachedPos = cam.position + fwd * distance + right * sideOffset + Vector3.up * heightOffset;

        Vector3 facing = Quaternion.Euler(0f, yawOffset, 0f) * fwd;
        cachedRot = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(tilt, 0f, 0f);
    }

    // ------------------------------------------------------------ bucle

    void LateUpdate()
    {
        if (!Resolve()) return;

        bool open = keyboardTransform.gameObject.activeInHierarchy;

        if (open && !wasOpen)
        {
            framesLeft = Mathf.Max(1, forceFramesOnOpen);
            released = false;
            var cam = PlayerCamera();
            if (cam != null)
            {
                ComputeTarget(cam);   // se calcula una sola vez, con la cabeza tal como estaba al abrir
                if (logEvents) Debug.Log($"[FixedKeyboard] Teclado abierto: lo coloco a {distance:F2} m de la camara.", this);
            }
        }
        wasOpen = open;
        if (!open) return;

        if (!released && (framesLeft > 0 || keepFixed))
        {
            canvasRoot.SetPositionAndRotation(cachedPos, cachedRot);
            if (framesLeft > 0) framesLeft--;
        }

        HideParts();
    }

    void HideParts()
    {
        if (hideChildNames == null) return;

        foreach (string childName in hideChildNames)
        {
            if (string.IsNullOrEmpty(childName)) continue;

            Transform child = FindDeep(keyboardTransform, childName);
            if (child != null && child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                if (logEvents) Debug.Log($"[FixedKeyboard] Oculto '{childName}'.", this);
            }
        }
    }

    // ------------------------------------------------------------ captura

    [ContextMenu("Soltar teclado (para moverlo y capturar)")]
    void ReleaseKeyboard()
    {
        released = true;
        Debug.Log("[FixedKeyboard] Teclado suelto: muevelo y luego usa 'Capturar posicion actual'.", this);
    }

    [ContextMenu("Capturar posicion actual")]
    void CaptureCurrent()
    {
        var cam = PlayerCamera();

        if (!Resolve() || cam == null)
        {
            Debug.LogWarning("[FixedKeyboard] No encuentro el teclado o la camara. Dale Play y abre el teclado antes de capturar.", this);
            return;
        }

        Vector3 fwd = FlatForward(cam);
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 toCanvas = canvasRoot.position - cam.position;

        distance = Vector3.Dot(toCanvas, fwd);
        sideOffset = Vector3.Dot(toCanvas, right);
        heightOffset = toCanvas.y;

        Vector3 canvasFwd = canvasRoot.forward;
        tilt = Mathf.Asin(Mathf.Clamp(-canvasFwd.y, -1f, 1f)) * Mathf.Rad2Deg;

        Vector3 canvasFlat = Vector3.ProjectOnPlane(canvasFwd, Vector3.up);
        yawOffset = canvasFlat.sqrMagnitude > 0.0001f ? Vector3.SignedAngle(fwd, canvasFlat.normalized, Vector3.up) : 0f;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[FixedKeyboard] Capturado: distancia {distance:F2} m, altura {heightOffset:F2} m, lado {sideOffset:F2} m, " +
                  $"inclinacion {tilt:F1}, giro {yawOffset:F1}. Ahora Copy Component, detén Play y Paste Component Values.", this);
    }
}
