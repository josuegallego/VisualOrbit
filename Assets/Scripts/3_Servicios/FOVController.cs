using UnityEngine;
using System.Collections;

public class FOVController : MonoBehaviour
{
    [Header("Referencias")]
    public Camera targetCamera;

    [Header("Configuración")]
    public float minFOV = 40f;
    public float maxFOV = 90f;
    public float defaultFOV = 60f;

    void Start()
    {
        StartCoroutine(InitializeNextFrame());
    }

    private IEnumerator InitializeNextFrame()
    {
        yield return null;

        // Si la cámara no es válida, la buscamos
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            Debug.Log($"[FOVController] Cámara encontrada: {(targetCamera != null ? targetCamera.name : "NINGUNA")}");
        }

        if (targetCamera == null)
        {
            Debug.LogError("FOVController: No se encontró ninguna cámara.");
            yield break;
        }

        float initialValue = SettingsManager.Instance != null
            ? SettingsManager.Instance.CurrentFOV
            : defaultFOV;
        SetFOV(initialValue);
        Debug.Log($"[FOVController] Valor inicial aplicado: {initialValue}");
    }

    public void SetFOV(float value)
    {
        // Verificamos que la cámara siga siendo válida
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        float fov = Mathf.Lerp(minFOV, maxFOV, value);
        targetCamera.fieldOfView = fov;
    }
}