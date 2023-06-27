using UnityEngine;

/// <summary>
/// Set volume of audio source in increments
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SetVolume : MonoBehaviour
{
    [Tooltip("The amount of change when changing the volume")]
    public float step = 0.1f;
    private AudioSource audioSource = null;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetAudioVolume(float value)
    {
        audioSource.volume = value;
    }

    public void IncreaseVolume()
    {
        float newVolume = audioSource.volume + step;
        audioSource.volume = newVolume;
    }

    public void DecreaseVolume()
    {
        float newVolume = audioSource.volume - step;
        audioSource.volume = newVolume;
    }
}
