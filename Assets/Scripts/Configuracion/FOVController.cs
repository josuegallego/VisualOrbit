using UnityEngine;

public class FOVController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí la Main Camera (dentro de XR Origin > Camera Offset)")]
    public Camera targetCamera;

    [Header("Configuración")]
    [Tooltip("Valor mínimo del Field of View")]
    public float minFOV = 40f;
    [Tooltip("Valor máximo del Field of View")]
    public float maxFOV = 90f;
    [Tooltip("Valor inicial del Field of View")]
    public float defaultFOV = 60f;

    void Start()
    {
        if (targetCamera == null)
        {
            Debug.LogError("FOVController: No se ha asignado la cámara objetivo.");
            return;
        }

        // Aplicamos el valor inicial
        targetCamera.fieldOfView = defaultFOV;
    }

    /// <summary>
    /// Método que se conecta al On Value Changed (Single) del Slider.
    /// Recibe un valor entre 0 y 1 y lo convierte en FOV.
    /// </summary>
    public void SetFOV(float value)
    {
        if (targetCamera == null) return;

        // Mapeamos 0-1 al rango de FOV
        float fov = Mathf.Lerp(minFOV, maxFOV, value);
        targetCamera.fieldOfView = fov;
    }
}