using System;
using UnityEngine;

/// <summary>
/// Avvia una partita: crea GameState, i paesani e il mondo, poi lancia Ready.
/// Ogni sistema si aggancia così (niente riferimenti incrociati tra sistemi):
///   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
///   static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(MioSistema)).AddComponent&lt;MioSistema&gt;();
/// </summary>
[DefaultExecutionOrder(-1000)] // Start prima dei componenti del mondo cotto nella scena (EmissionController usa GameState)
public class GameBootstrap : MonoBehaviour
{
    public static event Action Ready;

    public int seed = 0; // 0 = casuale

    void Start()
    {
        if (GlobalConfig.Instance == null) new GameObject("GlobalConfig").AddComponent<GlobalConfig>();
        var cfg = GlobalConfig.Instance;
        GameState.Current = new GameState(seed != 0 ? seed : Environment.TickCount, cfg.startingSavings);
        Roster.Populate(GameState.Current);
        UIKit.Ensure();
        if (FindAnyObjectByType<WorldAnchorsData>() == null) WorldBuilder.Build(); // altrimenti il mondo è già cotto nella scena
        Ready?.Invoke();
    }

    // Gira a ogni ingresso in Play prima degli Hook: con il domain reload disattivato evita iscritti doppi.
    // Non si azzera in OnDestroy, così "Ricomincia" (ricarica della scena) riaggancia tutti i sistemi.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHooks() => Ready = null;
}
