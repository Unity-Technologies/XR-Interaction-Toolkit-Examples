using UnityEngine;

/// <summary>
/// Play a simple sounds using Play one shot with volume, and pitch
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayQuickSound : MonoBehaviour
{
    [Tooltip("The sound that is played")]
    public AudioClip sound = null;

    [Tooltip("The volume of the sound")]
    public float volume = 1.0f;

    [Tooltip("The range of pitch the sound is played at (-pitch, pitch)")]
    [Range(0, 1)] public float randomPitchVariance = 0.0f;

    private AudioSource audioSource = null;

    private float defaultPitch = 1.0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Play()
    {
        float randomVariance = Random.Range(-randomPitchVariance, randomPitchVariance);
        randomVariance += defaultPitch;

        audioSource.pitch = randomVariance;
        audioSource.PlayOneShot(sound, volume);
        audioSource.pitch = defaultPitch;
    }

    private void OnValidate()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
}
