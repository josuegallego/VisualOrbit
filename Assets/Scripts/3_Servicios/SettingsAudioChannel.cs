using UnityEngine;

public enum AudioChannelType { Efectos, Musica, Voz }

/// <summary>
/// OPCIONAL. Ponlo en un objeto con AudioSource para decir a que slider de Configuracion obedece.
///   Efectos = slider "Efectos"   |   Musica = interruptor de musica (silencia/activa)   |   Voz = slider "Voces".
/// Si una fuente NO lo tiene, SettingsAudioRouter la clasifica solo (sonido en bucle = musica, el resto = efectos).
/// </summary>
[DisallowMultipleComponent]
public class SettingsAudioChannel : MonoBehaviour
{
    public AudioChannelType canal = AudioChannelType.Efectos;

    [Tooltip("Marcado: la configuracion NO toca esta fuente de audio.")]
    public bool ignorar = false;

    void OnEnable()
    {
        SettingsAudioRouter.Refresh();
    }
}
