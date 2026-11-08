
using UnityEngine;
using System.Collections;

public class PanelFadeIn : MonoBehaviour
{
    [Header("Configuración")]
    public float duracion = 1f;

    private SpriteRenderer panel;
    private SpriteRenderer texto;

    private void Awake()
    {
        panel = GetComponent<SpriteRenderer>();

        // Busca el SpriteRenderer del hijo (texto)
        texto = GetComponentInChildren<SpriteRenderer>();

        // Inicia ambos elementos transparentes
        CambiarOpacidad(panel, 0f);
        CambiarOpacidad(texto, 0f);
    }

    private void Start()
    {
        StartCoroutine(Aparecer());
    }

    IEnumerator Aparecer()
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float alpha = Mathf.Clamp01(tiempo / duracion);

            CambiarOpacidad(panel, alpha);
            CambiarOpacidad(texto, alpha);

            yield return null;
        }

        CambiarOpacidad(panel, 1f);
        CambiarOpacidad(texto, 1f);
    }

    void CambiarOpacidad(SpriteRenderer elemento, float alpha)
    {
        if (elemento == null) return;

        Color color = elemento.color;
        color.a = alpha;
        elemento.color = color;
    }
}
