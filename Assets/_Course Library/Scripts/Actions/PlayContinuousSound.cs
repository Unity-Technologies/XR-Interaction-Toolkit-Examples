using UnityEngine;

/// <summary>
/// Play a long continuous sound
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayContinuousSound : MonoBehaviour
{
    [Tooltip("The sound that is played")]
    public AudioClip sound = null;

    [Tooltip("Controls if the sound plays on start")]
    public bool playOnStart = false;

    [Tooltip("The volume of the sound")]
    public float volume = 1.0f;

    private AudioSource audioSource = null;
    private MonoBehaviour currentOwner = null;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        audioSource.clip = sound;
        audioSource.Play();
    }

    public void Pause()
    {
        audioSource.clip = null;
        audioSource.Pause();
    }

    public void PlayWithExclusivity(MonoBehaviour owner)
    {
        if (currentOwner == null)
        {
            currentOwner = owner;
            Play();
        }
    }

    public void StopWithExclusivity(MonoBehaviour owner)
    {
        if (currentOwner == owner)
        {
            currentOwner = null;
            Pause();
        }
    }

    public void TogglePlay()
    {
        bool isPlaying = !IsPlaying();
        SetPlay(isPlaying);
    }

    public void SetPlay(bool playAudio)
    {
        if (playAudio)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    public void SetClip(AudioClip audioClip)
    {
        sound = audioClip;
    }

    private void OnValidate()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }
}
