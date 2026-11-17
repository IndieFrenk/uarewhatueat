using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Post-processing URP creato da codice: un Volume globale con profilo runtime.
/// Vignetta, grana, desaturazione e aberrazione crescono coi giorni, di notte e al bar,
/// con un picco breve quando qualcuno viene preso. Parametri in GlobalConfig.
/// </summary>
public class PostFx : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(PostFx)).AddComponent<PostFx>();

    GlobalConfig cfg;
    VolumeProfile profile;
    Vignette vignette;
    FilmGrain grain;
    ColorAdjustments color;
    ChromaticAberration chroma;
    Camera cam;
    float night, spike; // night: 0..1 smussato; spike: 1 -> 0
    float oldRenderScale = -1f;
    UniversalRenderPipelineAsset urpAsset;

    void Start()
    {
        cfg = GlobalConfig.Instance;
        if (cfg == null || !cfg.postFxEnabled) { enabled = false; return; }

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(true);
        vignette.color.Override(cfg.postFxVignetteColor);
        vignette.smoothness.Override(0.4f);
        grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        color = profile.Add<ColorAdjustments>(true);
        chroma = profile.Add<ChromaticAberration>(true);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(cfg.postFxBloomIntensity);
        bloom.threshold.Override(cfg.postFxBloomThreshold);
        bloom.scatter.Override(0.6f);

        var vol = gameObject.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10;
        vol.sharedProfile = profile;

        if (cfg.postFxPs1Style)
        {
            urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null) { oldRenderScale = urpAsset.renderScale; urpAsset.renderScale = cfg.postFxPs1RenderScale; }
        }

        if (GameState.Current != null) GameState.Current.VillagerTaken += OnTaken;
        night = Target();
    }

    void OnTaken(Villager v) => spike = 1f;

    // Inquietudine di base: giorni + notte/bar.
    float Target()
    {
        var gs = GameState.Current;
        if (gs == null) return 0f;
        float days = Mathf.Clamp01((gs.Day - 1f) / (GameState.LastDay - 1f));
        float n = gs.Phase == GamePhase.Night || gs.Phase == GamePhase.Bar ? 1f
                : gs.Phase == GamePhase.Evening ? 0.5f : 0f;
        return days * cfg.postFxDayWeight + n * cfg.postFxNightWeight;
    }

    void Update()
    {
        // la camera la crea WorldBuilder: la agganciamo appena c'è (e se cambia)
        if (cam == null)
        {
            cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        night = Mathf.MoveTowards(night, Target(), Time.deltaTime / Mathf.Max(0.01f, cfg.postFxSmoothTime));
        spike = Mathf.MoveTowards(spike, 0f, Time.deltaTime / Mathf.Max(0.01f, cfg.postFxSpikeDuration));
        float t = Mathf.Clamp01(night + spike * cfg.postFxSpikeAmount);

        vignette.intensity.Override(Mathf.Lerp(cfg.postFxVignette.x, cfg.postFxVignette.y, t));
        grain.intensity.Override(Mathf.Lerp(cfg.postFxGrain.x, cfg.postFxGrain.y, t));
        color.saturation.Override(Mathf.Lerp(cfg.postFxSaturation.x, cfg.postFxSaturation.y, t));
        chroma.intensity.Override(Mathf.Lerp(cfg.postFxChromatic.x, cfg.postFxChromatic.y, t));
    }

    void OnDestroy()
    {
        if (GameState.Current != null) GameState.Current.VillagerTaken -= OnTaken;
        if (urpAsset != null && oldRenderScale > 0f) urpAsset.renderScale = oldRenderScale;
        if (profile != null) Destroy(profile);
    }
}
