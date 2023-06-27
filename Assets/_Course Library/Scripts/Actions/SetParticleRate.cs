using UnityEngine;

/// <summary>
/// Changes the rate at which a particle system emits
/// </summary>
public class SetParticleRate : MonoBehaviour
{
    private ParticleSystem currentParticleSystem = null;

    private void Awake()
    {
        currentParticleSystem = GetComponent<ParticleSystem>();
    }

    public void SetRate(float value)
    {
        ParticleSystem.EmissionModule emission = currentParticleSystem.emission;
        emission.rateOverTime = value;
    }
}
