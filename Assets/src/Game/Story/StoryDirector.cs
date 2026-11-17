using UnityEngine;

/// <summary>Aggancia la storia al gioco: TV, bar della sera, tasto J per il quaderno.</summary>
public class StoryDirector : MonoBehaviour
{
    GameState state;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(StoryDirector)).AddComponent<StoryDirector>();

    void Start()
    {
        state = GameState.Current;
        BarScene.Reset();
        BuildTv();
        state.PhaseChanged += OnPhase;
        if (state.Phase == GamePhase.Bar) OnPhase(GamePhase.Bar);
    }

    void OnPhase(GamePhase p)
    {
        if (p == GamePhase.Bar) BarScene.Spawn(state);
        else BarScene.Clear();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) Story.ToggleNotebook();
    }

    void OnDestroy()
    {
        if (state != null) state.PhaseChanged -= OnPhase;
        BarScene.Reset();
    }

    /// <summary>Usa l'oggetto "TV" del mondo se c'è, altrimenti ne crea uno segnaposto vicino a HomeSpawn.</summary>
    static void BuildTv()
    {
        var tv = GameObject.Find("TV");
        if (tv == null)
        {
            var home = WorldAnchors.HomeSpawn;
            if (home == null) return;
            tv = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tv.name = "TV";
            tv.transform.position = home.position + home.forward * 2f + Vector3.up * 0.9f;
            tv.transform.rotation = Quaternion.LookRotation(-home.forward);
            tv.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
            tv.GetComponent<Renderer>().material.color = new Color(0.12f, 0.12f, 0.14f);
        }
        if (tv.GetComponentInChildren<Collider>() == null) tv.AddComponent<BoxCollider>();
        if (tv.GetComponent<TvSet>() == null) tv.AddComponent<TvSet>();
    }
}
