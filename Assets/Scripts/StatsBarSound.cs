using UnityEngine;

/// <summary>
/// Ponlo en el panel de retroalimentacion.
/// Cuando el panel aparece suenan DOS audios a la vez (capa A y capa B).
/// Cuando el panel se oculta, se detienen.
///
/// Usa un Audio Source por cada capa. Si solo agregas uno (o ninguno), los que falten se crean solos.
/// </summary>
public class StatsBarSound : MonoBehaviour
{
    [Header("Capa A")]
    public AudioClip clipA;
    [Range(0f, 1f)] public float volumeA = 0.5f;

    [Header("Capa B (suena a la vez que la A)")]
    public AudioClip clipB;
    [Range(0f, 1f)] public float volumeB = 0.4f;

    [Header("Opciones")]
    [Tooltip("Activalo si el audio es corto y quieres que se repita mientras el panel este visible")]
    public bool loop = false;

    AudioSource a, b;

    void Awake()
    {
        var sources = GetComponents<AudioSource>();
        a = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        b = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        Play(a, clipA, volumeA);
        Play(b, clipB, volumeB);
    }

    void OnDisable()
    {
        if (a != null) a.Stop();
        if (b != null) b.Stop();
    }

    void Play(AudioSource s, AudioClip clip, float vol)
    {
        if (s == null) return;
        if (clip == null) return;

        s.Stop();
        s.clip = clip;
        s.volume = vol;
        s.pitch = 1f;
        s.loop = loop;
        s.spatialBlend = 0f;      // sonido 2D: se escucha igual en cualquier lugar
        s.mute = false;
        s.Play();
    }
}
