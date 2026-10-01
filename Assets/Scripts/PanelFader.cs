using System.Collections;
using UnityEngine;

/// <summary>
/// Cambia de un panel a otro con fade. No necesita listas: cada boton le dice cual es el siguiente.
/// Se crea solo (un objeto "PanelFader" invisible) la primera vez que se usa.
/// </summary>
public class PanelFader : MonoBehaviour
{
    public static float FadeDuration = 0.25f;

    static PanelFader instance;
    static PanelFader Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("PanelFader");
                instance = go.AddComponent<PanelFader>();
            }
            return instance;
        }
    }

    bool busy;

    /// <summary>Encuentra el panel "raiz" al que pertenece un objeto: el hijo directo del Canvas.</summary>
    public static GameObject RootPanelOf(Transform t)
    {
        while (t.parent != null && t.parent.GetComponent<Canvas>() == null)
            t = t.parent;
        return t.gameObject;
    }

    /// <summary>Oculta 'from' (con fade) y muestra 'to' (con fade). Cualquiera puede ser null.</summary>
    public static void Switch(GameObject from, GameObject to)
    {
        var f = Instance;
        if (f.busy) return;                 // evita dobles clics
        f.StartCoroutine(f.Run(from, to));
    }

    IEnumerator Run(GameObject from, GameObject to)
    {
        busy = true;

        if (from != null && from.activeInHierarchy)
        {
            var cg = GroupOf(from);
            cg.blocksRaycasts = false;
            yield return Fade(cg, cg.alpha, 0f);
            from.SetActive(false);
            cg.alpha = 1f;                  // lo dejamos listo para cuando se vuelva a mostrar
            cg.blocksRaycasts = true;
        }

        if (to != null)
        {
            var cg = GroupOf(to);
            cg.alpha = 0f;
            to.SetActive(true);
            yield return Fade(cg, 0f, 1f);
            cg.alpha = 1f;
        }

        busy = false;
    }

    static CanvasGroup GroupOf(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    static IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        if (FadeDuration <= 0f) { cg.alpha = to; yield break; }

        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / FadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }
}
