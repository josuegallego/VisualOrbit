using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource spatialMusic;

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
}