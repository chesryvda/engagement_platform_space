using UnityEngine;

// Twee schuifjes om het ruimtestof snel af te stellen, zonder in het Particle System te zoeken.
// 1.0 = de oorspronkelijke waarden. Werkt ook live in Play (wijzig de schuifjes in de Inspector).
// SpaceDustSpeedFx kan de hoeveelheid tijdens het rijden extra bijsturen via SetSpeedFactor.
[RequireComponent(typeof(ParticleSystem))]
public class SpaceDustSettings : MonoBehaviour
{
    [Tooltip("Hoeveel stof er is. 1 = oorspronkelijk (10 deeltjes per afgelegde meter, 700 bij de start).")]
    [Range(0.25f, 3f)] public float density = 1.4f;
    [Tooltip("Hoe groot de stipjes zijn. 1 = oorspronkelijk (6 tot 22 cm).")]
    [Range(0.5f, 3f)] public float size = 1f;

    const float BaseRatePerMeter = 10f;
    const int BaseBurst = 700;
    const float BaseMinSize = 0.06f;
    const float BaseMaxSize = 0.22f;
    const int BaseMaxParticles = 5000;

    // Extra factor tijdens het rijden (1 = niets extra). Wordt door SpaceDustSpeedFx gezet.
    private float speedFactor = 1f;
    private ParticleSystem ps;

    void Awake()
    {
        Apply();
    }

    void OnValidate()
    {
        if (isActiveAndEnabled) Apply();
    }

    public void SetSpeedFactor(float factor)
    {
        speedFactor = factor;
        if (ps == null) ps = GetComponent<ParticleSystem>();
        var emission = ps.emission;
        emission.rateOverDistance = BaseRatePerMeter * density * speedFactor;
    }

    private void Apply()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var emission = ps.emission;
        emission.rateOverDistance = BaseRatePerMeter * density * speedFactor;
        var bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);
        if (bursts.Length > 0)
        {
            bursts[0].count = Mathf.RoundToInt(BaseBurst * density);
            emission.SetBursts(bursts);
        }

        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(BaseMinSize * size, BaseMaxSize * size);
        // genoeg ruimte voor de deeltjes die achter het schip nog even blijven leven
        main.maxParticles = Mathf.CeilToInt(BaseMaxParticles * Mathf.Max(1f, density) * 1.6f);
    }
}
