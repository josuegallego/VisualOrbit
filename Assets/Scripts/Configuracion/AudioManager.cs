using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource spatialMusic;
    public AudioSource voiceAudio;
    public AudioSource[] effectsAudio;

    public void SetSpatialMusic(bool isOn)
    {
        if (isOn)
        {
            spatialMusic.Play();
        }
        else
        {
            spatialMusic.Stop();
        }
    }

    public void SetVoiceVolume(float volume)
    {
        voiceAudio.volume = volume;
    }

    public void SetEffectsVolume(float volume)
    {
        foreach (AudioSource effect in effectsAudio)
        {
            effect.volume = volume;
        }
    }
}