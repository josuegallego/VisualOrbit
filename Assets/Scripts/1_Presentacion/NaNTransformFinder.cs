using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Herramienta de depuracion: avisa en consola QUE objeto tiene posicion, rotacion o escala
/// invalida (NaN / infinito / enorme). Borrala cuando encuentres el problema.
/// </summary>
public class NaNTransformFinder : MonoBehaviour
{
    public float maxValue = 100000f;
    [Tooltip("Desactiva el objeto culpable para que dejen de salir los errores.")]
    public bool disableOffender = false;

    readonly HashSet<Transform> reported = new HashSet<Transform>();

    void LateUpdate()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (reported.Contains(t)) continue;

            if (Bad(t.position) || Bad(t.localPosition) || Bad(t.localScale) || Bad(t.rotation))
            {
                reported.Add(t);
                Debug.LogError($"[NaNFinder] Transform invalido: {Path(t)} | pos={t.localPosition} | " +
                               $"rot={t.localRotation} | escala={t.localScale}", t);
                if (disableOffender) t.gameObject.SetActive(false);
            }
        }
    }

    bool Bad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        Mathf.Abs(v.x) > maxValue || Mathf.Abs(v.y) > maxValue || Mathf.Abs(v.z) > maxValue;

    bool Bad(Quaternion q) =>
        float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w);

    string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}