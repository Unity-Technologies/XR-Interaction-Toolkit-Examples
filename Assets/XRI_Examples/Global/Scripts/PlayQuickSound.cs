namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Play a simple sound using <c>PlayOneShot</c> with volume and randomized pitch.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayQuickSound : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The sound that is played.")]
        AudioClip m_Sound;

        [SerializeField]
        [Tooltip("The volume of the sound.")]
        float m_Volume = 1f;

        [SerializeField]
        [Tooltip("The range of pitch the sound is played at (-pitch, pitch).")]
        [Range(0, 1)]
        float m_RandomPitchVariance;

        AudioSource m_AudioSource;

        const float k_DefaultPitch = 1f;

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
        }

        public void Play()
        {
            var randomVariance = Random.Range(-m_RandomPitchVariance, m_RandomPitchVariance);
            randomVariance += k_DefaultPitch;

            m_AudioSource.pitch = randomVariance;
            m_AudioSource.PlayOneShot(m_Sound, m_Volume);
            m_AudioSource.pitch = k_DefaultPitch;
        }
    }
}
