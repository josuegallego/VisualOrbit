using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public AudioSource spatialMusic;
    public AudioSource voiceAudio;
    public AudioSource[] effectsAudio;

    void Start()
    {
        StartCoroutine(InitializeNextFrame());
    }

    private IEnumerator InitializeNextFrame()
    {
        yield return null;

        // Verificamos que las referencias sean válidas
        if (spatialMusic == null)
        {
            Debug.LogWarning("[AudioManager] spatialMusic no asignado.");
        }
        if (voiceAudio == null)
        {
            Debug.LogWarning("[AudioManager] voiceAudio no asignado.");
        }

        if (SettingsManager.Instance != null)
        {
            if (voiceAudio != null)
                SetVoiceVolume(SettingsManager.Instance.CurrentVoiceVolume);
            if (effectsAudio != null && effectsAudio.Length > 0)
                SetEffectsVolume(SettingsManager.Instance.CurrentEffectsVolume);
            if (spatialMusic != null)
                SetSpatialMusic(SettingsManager.Instance.CurrentSpatialMusic);
        }
    }

    public void SetSpatialMusic(bool isOn)
    {
        if (spatialMusic == null) return;

        if (isOn) spatialMusic.Play();
        else spatialMusic.Stop();
    }

    public void SetVoiceVolume(float volume)
    {
        if (voiceAudio == null)
        {
            Debug.LogWarning("[AudioManager] voiceAudio no asignado. No se puede aplicar volumen.");
            return;
        }
        voiceAudio.volume = volume;
    }

    public void SetEffectsVolume(float volume)
    {
        if (effectsAudio == null) return;
        foreach (AudioSource effect in effectsAudio)
        {
            if (effect != null) effect.volume = volume;
        }
    }
}