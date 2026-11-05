using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Referencias")]
    public XROrigin xrOrigin;          // Tu XR Origin
    public Transform puntoInicio;      // Objeto vacío donde empieza el jugador

    [Header("Opciones")]
    public bool alinearRotacion = true;
    [Tooltip("Espera a que el tracking del visor arranque")]
    public float espera = 0.2f;
    [Tooltip("Repite el ajuste una vez más por seguridad")]
    public bool repetirUnaVez = true;

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
    }

    IEnumerator Start()
    {
        yield return null;                       // un frame
        yield return new WaitForSeconds(espera); // deja que el tracking se estabilice
        Colocar();

        if (repetirUnaVez)
        {
            yield return new WaitForSeconds(0.5f);
            Colocar();
        }
    }

    [ContextMenu("Colocar jugador ahora")]
    public void Colocar()
    {
        if (xrOrigin == null || puntoInicio == null || xrOrigin.Camera == null)
        {
            Debug.LogWarning("[PlayerSpawnPoint] Falta asignar XROrigin o PuntoInicio.");
            return;
        }

        // 1) Rotación: que la cámara mire hacia donde apunta el punto
        if (alinearRotacion)
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, puntoInicio.forward);

        // 2) Posición: la cámara queda justo sobre el punto (en horizontal)
        Vector3 delta = puntoInicio.position - xrOrigin.Camera.transform.position;
        delta.y = 0f;
        xrOrigin.transform.position += delta;

        // 3) Altura: el suelo del rig queda a la altura del punto
        Vector3 p = xrOrigin.transform.position;
        p.y = puntoInicio.position.y;
        xrOrigin.transform.position = p;

        Debug.Log($"[PlayerSpawnPoint] Jugador colocado en {puntoInicio.name}");
    }
}