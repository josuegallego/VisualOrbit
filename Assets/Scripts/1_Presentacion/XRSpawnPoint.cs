using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Coloca al jugador (su cabeza) en un punto de la escena al empezar, sin importar donde este
/// el XR Origin ni donde se encuentre el jugador en su espacio real.
///
/// USO:
///   1. Crea un objeto vacio en la escena (por ejemplo "SpawnPoint") y ponlo donde quieres que
///      este la cabeza del jugador. Su flecha azul (eje Z) marca hacia donde mirara.
///   2. Pon este script en cualquier objeto de la escena (por ejemplo en el mismo SpawnPoint).
///   3. Arrastra el SpawnPoint a 'Spawn Point'.
///
/// Espera un momento antes de colocar al jugador porque el visor tarda unos fotogramas en
/// empezar a enviar su posicion real.
/// </summary>
public class XRSpawnPoint : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Donde quedara la cabeza del jugador. Su eje Z (azul) = hacia donde mira.")]
    public Transform spawnPoint;
    [Tooltip("Si esta activo, tambien se gira al jugador para que mire hacia el eje Z del punto.")]
    public bool alignHeading = true;
    [Tooltip("Si esta activo, usa tambien la altura (Y) del punto. Si no, mantiene la altura actual de la cabeza.")]
    public bool useSpawnHeight = false;

    [Header("Momento")]
    [Tooltip("Segundos de espera tras empezar la escena, para que el visor ya este reportando su posicion.")]
    public float delaySeconds = 0.5f;

    [Header("Depuracion")]
    public bool logEvents = true;

    IEnumerator Start()
    {
        yield return null;
        if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);
        Align();
    }

    /// <summary>Tambien se puede llamar a mano desde el menu del componente (tres puntos).</summary>
    [ContextMenu("Colocar al jugador ahora")]
    public void Align()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("[XRSpawnPoint] Falta 'Spawn Point': no se mueve al jugador.", this);
            return;
        }

        var origin = FindFirstObjectByType<XROrigin>();
        if (origin == null || origin.Camera == null)
        {
            Debug.LogWarning("[XRSpawnPoint] No encuentro el XR Origin o su camara.", this);
            return;
        }

        // 1) Girar primero, para que la posicion se calcule ya con el giro aplicado
        if (alignHeading)
        {
            Vector3 forward = Vector3.ProjectOnPlane(spawnPoint.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
                origin.MatchOriginUpCameraForward(Vector3.up, forward.normalized);
        }

        // 2) Mover el origen para que la cabeza quede en el punto
        Vector3 target = spawnPoint.position;
        if (!useSpawnHeight) target.y = origin.Camera.transform.position.y;
        origin.MoveCameraToWorldLocation(target);

        if (logEvents)
            Debug.Log($"[XRSpawnPoint] Jugador colocado en {spawnPoint.name} " +
                      $"(cabeza en {origin.Camera.transform.position}).", this);
    }
}
