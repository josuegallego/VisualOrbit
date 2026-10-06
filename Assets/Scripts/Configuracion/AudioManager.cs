using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioSource spatialMusic;
    public AudioMixer audioMixer;

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
        float volumeDB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
        audioMixer.SetFloat("VocesVolume", volumeDB);
    }
}