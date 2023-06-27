using UnityEngine;
using System.Collections;

namespace VRBuilder.Core.ProcessUtils
{
    /// <summary>
    /// Manager of all listed particle systems using confetti as particles.
    /// Manages activation, deactivation, and some particle system configurations.
    /// </summary>
    public class ConfettiMachine : MonoBehaviour, IParticleMachine
    {
        [SerializeField]
        [Tooltip("List of all particle systems with individual confetti particle materials.")]
        private ParticleSystem[] confettiSystems = null;

        [SerializeField]
        [Tooltip("Duration in seconds after the machine finishes to emit confetti.")]
        private float emissionDuration;

        [SerializeField]
        [Tooltip("If this flag is set to true, the machine starts emitting confetti automatically after spawning.")]
        private bool activateOnStart = false;

        [SerializeField]
        [Tooltip("Audio source playing audio during confetti emission.")]
        private AudioSource source = null;

        [SerializeField]
        [Tooltip("Audio clip being played during confetti emission.")]
        private AudioClip clip = null;

        private float[] defaultEmissionRateMultipliers;

        /// <summary>
        /// Duration in seconds after the machine finishes to emit confetti.
        /// </summary>
        public float EmissionDuration
        {
            get
            {
                return emissionDuration;
            }
        }

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <inheritdoc />
        public void Activate()
        {
            if (IsActive || confettiSystems.Length == 0)
            {
                return;
            }

            foreach (ParticleSystem confettiSystem in confettiSystems)
            {
                confettiSystem.gameObject.SetActive(true);
            }

            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }

            StartCoroutine(DeactivateAfter(emissionDuration));
            IsActive = true;
        }

        /// <inheritdoc />
        public void Activate(float newRadius, float newDuration)
        {
            if (IsActive)
            {
                return;
            }

            ChangeAreaRadius(newRadius);
            ChangeEmissionDuration(newDuration);
            Activate();
        }

        /// <inheritdoc />
        public void Deactivate()
        {
            if (IsActive == false)
            {
                return;
            }

            StopAllCoroutines();

            foreach (ParticleSystem confettiSystem in confettiSystems)
            {
                confettiSystem.gameObject.SetActive(false);
            }

            if (source != null && source.isPlaying && source.clip == clip)
            {
                source.Stop();
            }

            IsActive = false;
        }

        /// <inheritdoc />
        public void ChangeAreaRadius(float newRadius)
        {
            // Handle invalid radius values
            if (newRadius < 0.01f)
            {
                newRadius = 0.01f;
                Debug.LogWarning("You provided a too small or negative area radius. The area radius can not be smaller than 0.01.");
            }

            foreach (ParticleSystem confettiSystem in confettiSystems)
            {
                ParticleSystem.ShapeModule shapeModule = confettiSystem.shape;

                shapeModule.shapeType = ParticleSystemShapeType.Cone;

                shapeModule.angle = 0f;
                shapeModule.radius = newRadius;
            }

            ChangeEmissionRate(newRadius * newRadius);
        }

        /// <inheritdoc />
        public void ChangeEmissionDuration(float newDuration)
        {
            // Handle invalid duration values
            if (newDuration < 0f)
            {
                newDuration = 0f;
                Debug.LogWarning("You provided a negative duration. The duration has to be positive or 0.");
            }

            foreach (ParticleSystem confettiSystem in confettiSystems)
            {
                ParticleSystem.MainModule mainModule = confettiSystem.main;
                mainModule.duration = newDuration;
            }

            emissionDuration = newDuration;
        }

        /// <summary>
        /// Multiplies the particle emission rate over time by the given value.
        /// </summary>
        /// <param name="multiplier">Value being multiplied by the default emission rate value at radius 1.</param>
        private void ChangeEmissionRate(float multiplier)
        {
            for (int i = 0; i < confettiSystems.Length; i++)
            {
                ParticleSystem.EmissionModule emissionModule = confettiSystems[i].emission;
                emissionModule.rateOverTimeMultiplier = multiplier * defaultEmissionRateMultipliers[i];
            }
        }

        /// <summary>
        /// Deactivates the confetti machine after the given time.
        /// </summary>
        /// <param name="time">Seconds after which the confetti machine should stop.</param>
        private IEnumerator DeactivateAfter(float time)
        {
            yield return new WaitForSeconds(time);
            Deactivate();
        }

        private void Awake()
        {
            if (confettiSystems.Length == 0)
            {
                Debug.LogWarning("There are no particle systems added to the array \"Confetti Systems\". Please add them in the inspector.");
                return;
            }

            defaultEmissionRateMultipliers = new float[confettiSystems.Length];

            for (int i = 0; i < defaultEmissionRateMultipliers.Length; i++)
            {
                defaultEmissionRateMultipliers[i] = confettiSystems[i].emission.rateOverTimeMultiplier;
                confettiSystems[i].gameObject.SetActive(false);
            }

            ChangeEmissionDuration(EmissionDuration);
        }

        private void Start()
        {
            if (activateOnStart)
            {
                Activate();
            }
        }
    }
}
