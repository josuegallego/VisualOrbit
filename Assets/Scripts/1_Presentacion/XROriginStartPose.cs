using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Coloca la CABEZA (la vista) del jugador en una posicion y giro exactos del mundo al empezar.
/// Mueve el XR Origin lo necesario para que la camara quede ahi, asi que funciona aunque el
/// Camera Offset o la camara tengan valores raros.
///
/// COMO FIJAR EL PUNTO PERFECTO:
///   1. Dale Play y ponte donde quieres que empiece el jugador (simulador o gafas).
///   2. Menu del componente (tres puntos) -> "Capturar vista actual".
///   3. Menu del componente -> Copy Component.
///   4. Detén Play, menu del componente -> Paste Component Values. Guarda la escena.
///
/// Tambien se pueden escribir los valores a mano o arrastrar un objeto a 'Target'.
/// </summary>
[DefaultExecutionOrder(-100)]
public class XROriginStartPose : MonoBehaviour
{
    [Header("Donde queda la cabeza del jugador")]
    [Tooltip("Opcional. Si lo asignas, se usa su posicion y su giro (eje Z azul = hacia donde mira).")]
    public Transform target;
    public Vector3 headPosition;
    [Tooltip("Se activa solo al usar 'Capturar vista actual'. Si esta apagado y no hay Target, el script no mueve nada.")]
    public bool configured = false;
    [Tooltip("Giro horizontal (Y) hacia donde mira el jugador.")]
    public float headYaw;
    [Tooltip("Si esta activo usa tambien la altura Y de la cabeza. Si no, conserva la altura real del visor.")]
    public bool useHeadHeight = false;

    [Header("Momento")]
    [Tooltip("Espera antes de colocar, para que el visor ya reporte su posicion.")]
    public float delaySeconds = 0f;
    [Tooltip("Segundos durante los que se reaplica cada fotograma.")]
    public float holdSeconds = 1.5f;

    [Header("Depuracion")]
    public bool logEvents = true;

    XROrigin origin;
    CharacterController cc;

    Vector3 TargetPos => target != null ? target.position : headPosition;
    float TargetYaw => target != null ? target.eulerAngles.y : headYaw;

    void Awake()
    {
        if (target == null && !configured) return;
        origin = FindFirstObjectByType<XROrigin>();
        if (origin != null && origin.Origin != null) cc = origin.Origin.GetComponent<CharacterController>();
        Apply();
    }

    IEnumerator Start()
    {
        if (target == null && !configured)
        {
            Debug.Log("[XROriginStartPose] Sin Target ni vista capturada: no muevo al jugador.", this);
            yield break;
        }

        origin = FindFirstObjectByType<XROrigin>();
        if (origin == null || origin.Camera == null)
        {
            Debug.LogWarning("[XROriginStartPose] No encuentro el XR Origin o su camara.", this);
            yield break;
        }
        cc = origin.Origin != null ? origin.Origin.GetComponent<CharacterController>() : null;

        // Se coloca ya en el primer fotograma y se mantiene: asi no se ve el salto
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        float end = Time.time + holdSeconds;
        do
        {
            Apply();
            yield return new WaitForEndOfFrame();
        } while (Time.time < end);

        if (logEvents)
            Debug.Log($"[XROriginStartPose] Cabeza en {origin.Camera.transform.position} " +
                      $"(giro {origin.Camera.transform.eulerAngles.y:F1}); destino {TargetPos} / {TargetYaw:F1}.", this);
    }

    [ContextMenu("Colocar ahora")]
    public void Apply()
    {
        if (origin == null) origin = FindFirstObjectByType<XROrigin>();
        if (origin == null || origin.Camera == null) return;

        bool ccWasOn = cc != null && cc.enabled;
        if (ccWasOn) cc.enabled = false;

        // 1) girar para que la camara mire al giro pedido
        Vector3 fwd = Quaternion.Euler(0f, TargetYaw, 0f) * Vector3.forward;
        origin.MatchOriginUpCameraForward(Vector3.up, fwd);

        // 2) mover el origen para que la cabeza quede en el punto
        Vector3 p = TargetPos;
        if (!useHeadHeight) p.y = origin.Camera.transform.position.y;
        origin.MoveCameraToWorldLocation(p);

        if (ccWasOn)
        {
            Physics.SyncTransforms();
            cc.enabled = true;
        }
    }

    [ContextMenu("Capturar vista actual")]
    void CaptureCurrent()
    {
        var o = FindFirstObjectByType<XROrigin>();
        if (o == null || o.Camera == null)
        {
            Debug.LogWarning("[XROriginStartPose] No encuentro el XR Origin o su camara.", this);
            return;
        }

        headPosition = o.Camera.transform.position;
        headYaw = o.Camera.transform.eulerAngles.y;
        useHeadHeight = true;
        configured = true;
        Debug.Log($"[XROriginStartPose] Capturado: cabeza {headPosition}, giro {headYaw:F1}. " +
                  "Ahora Copy Component, deten Play y Paste Component Values.", this);
    }
}
