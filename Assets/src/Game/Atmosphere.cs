using UnityEngine;
using System.Collections;

/// <summary>
/// Gestisce l'atmosfera del gioco: nebbia progressiva, tremolio luci, luce nel cielo per le prese.
/// </summary>
public class Atmosphere : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(Atmosphere)).AddComponent<Atmosphere>();

    private GlobalConfig cfg;
    private float lastFlickerTime;
    private float nextFlickerInterval;
    private AudioSource audioSource;
    private Color dayFogColor;

    void Start()
    {
        cfg = GlobalConfig.Instance;
        RenderSettings.fog = true;
        dayFogColor = RenderSettings.fogColor;

        // Iscriviti agli eventi
        if (GameState.Current != null)
            GameState.Current.VillagerTaken += OnVillagerTaken;

        // Setup audio source per le notti di presa
        if (cfg.humClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = cfg.humClip;
            audioSource.volume = 0.3f;
            audioSource.playOnAwake = false;
        }

        ComputeNextFlickerInterval();
    }

    void Update()
    {
        UpdateFog();
        UpdateShopLights();
    }

    void UpdateFog()
    {
        if (cfg == null) return;

        float hour = cfg.currentHour;
        float dayMultiplier = (GameState.Current.Day - 1) * cfg.fogDensityDayMultiplier;

        // Nebbia principalmente di sera/notte (18:00 - 06:00)
        float fogIntensity = 0f;

        if (hour >= 18f || hour < 6f)
        {
            // Curva: 18→00 sale, 00→06 rimane alta, 06→18 scende
            if (hour >= 18f)
                fogIntensity = Mathf.InverseLerp(18f, 24f, hour); // 18-24: 0 → 1
            else
                fogIntensity = Mathf.InverseLerp(6f, 0f, hour); // 06-00: 1 → 0

            fogIntensity = Mathf.Clamp01(fogIntensity);
        }

        // notte profonda: nebbia più fitta, scura, e luce ambientale che cala (cresce con l'ora, quindi col bar)
        float deep = fogIntensity * fogIntensity;
        float max = cfg.fogDensityMax + cfg.nightFogExtra * deep + dayMultiplier;
        float density = Mathf.Lerp(cfg.fogDensityMin, cfg.fogDensityMax, fogIntensity) + cfg.nightFogExtra * deep + dayMultiplier;
        RenderSettings.fogDensity = Mathf.Clamp(density, cfg.fogDensityMin, max);
        RenderSettings.fogColor = Color.Lerp(dayFogColor, cfg.nightFogColor, fogIntensity);
        RenderSettings.ambientIntensity = Mathf.Lerp(1f, cfg.nightAmbientIntensity, fogIntensity);
    }

    void UpdateShopLights()
    {
        if (WorldAnchors.ShopLights == null || WorldAnchors.ShopLights.Length == 0) return;

        float now = Time.time;

        // Tempo trascorso da ultimo tremolio
        if (now - lastFlickerTime >= nextFlickerInterval)
        {
            lastFlickerTime = now;

            // Probabilità di tremolio
            float chance = cfg.shopLightFlickerChance;

            // Dal giorno 3+ aumenta la frequenza
            if (GameState.Current.Day >= 3)
                chance *= cfg.flickerFrequencyMultiplierDay3Plus;

            // Se qualcuno è stato preso la notte prima, aumenta
            if (GameState.Current.Day > 1)
                chance *= 1.3f;

            if (Random.value < chance)
                StartCoroutine(FlickerLights());

            ComputeNextFlickerInterval();
        }
    }

    void ComputeNextFlickerInterval()
    {
        // Intervallo casuale tra tremolii (minimo 3 secondi)
        nextFlickerInterval = Random.Range(3f, 8f);
    }

    IEnumerator FlickerLights()
    {
        // Salva intensità originali
        float[] originalIntensities = new float[WorldAnchors.ShopLights.Length];
        for (int i = 0; i < WorldAnchors.ShopLights.Length; i++)
        {
            if (WorldAnchors.ShopLights[i] != null)
                originalIntensities[i] = WorldAnchors.ShopLights[i].intensity;
        }

        // Tremolio: abbassa per la durata
        float elapsed = 0f;
        while (elapsed < cfg.shopLightFlickerDuration)
        {
            elapsed += Time.deltaTime;
            float factor = 0.3f + Random.Range(0f, 0.2f); // 0.3-0.5 della luminosità originale

            for (int i = 0; i < WorldAnchors.ShopLights.Length; i++)
            {
                if (WorldAnchors.ShopLights[i] != null)
                    WorldAnchors.ShopLights[i].intensity = originalIntensities[i] * factor;
            }

            yield return null;
        }

        // Ripristina
        for (int i = 0; i < WorldAnchors.ShopLights.Length; i++)
        {
            if (WorldAnchors.ShopLights[i] != null)
                WorldAnchors.ShopLights[i].intensity = originalIntensities[i];
        }
    }

    void OnVillagerTaken(Villager v)
    {
        // Luce nel cielo quando qualcuno viene preso
        StartCoroutine(SkyLight());

        // Riproduci audio se disponibile
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.PlayOneShot(cfg.humClip);
    }

    IEnumerator SkyLight()
    {
        // Crea GameObject con Light nel cielo
        GameObject skyLightObj = new GameObject("SkyLight_Taking");
        Light skyLight = skyLightObj.AddComponent<Light>();

        // Posiziona in alto sopra la valle
        Vector3 playerPos = WorldAnchors.Player != null ? WorldAnchors.Player.position : Vector3.zero;
        skyLightObj.transform.position = playerPos + Vector3.up * 200f + Random.insideUnitSphere * 50f;

        // Setup light
        skyLight.type = LightType.Point;
        skyLight.range = 300f;
        skyLight.intensity = 0f;
        skyLight.color = new Color(0.8f, 0.8f, 1f); // Colore pallido bluastro

        // Accendi lentamente
        float elapsed = 0f;
        float riseTime = cfg.skyLightDuration * 0.3f;

        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            skyLight.intensity = Mathf.Lerp(0f, cfg.skyLightIntensity, elapsed / riseTime);
            yield return null;
        }

        skyLight.intensity = cfg.skyLightIntensity;

        // Mantieni acceso
        yield return new WaitForSeconds(cfg.skyLightDuration * 0.4f);

        // Svanisci
        elapsed = 0f;
        float fadeTime = cfg.skyLightDuration * 0.3f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            skyLight.intensity = Mathf.Lerp(cfg.skyLightIntensity, 0f, elapsed / fadeTime);
            yield return null;
        }

        Destroy(skyLightObj);
    }

    void OnDestroy()
    {
        if (GameState.Current != null)
            GameState.Current.VillagerTaken -= OnVillagerTaken;
    }
}
