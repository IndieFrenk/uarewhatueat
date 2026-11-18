using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlla l'emissione delle finestre accese: spente di giorno, accese di sera.
/// </summary>
public class EmissionController : MonoBehaviour
{
    // serializzati: col mondo cotto nella scena arrivano già pronti
    [SerializeField] Material[] materials;
    [SerializeField] Color[] colors;

    public void Initialize(List<(Material, Color)> mats)
    {
        materials = new Material[mats.Count];
        colors = new Color[mats.Count];
        for (int i = 0; i < mats.Count; i++) (materials[i], colors[i]) = mats[i];
    }

    // GameBootstrap gira prima (DefaultExecutionOrder), quindi GameState.Current c'è già
    void Start()
    {
        GameState.Current.PhaseChanged += OnPhaseChanged;
        // Applica lo stato iniziale
        OnPhaseChanged(GameState.Current.Phase);
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        bool isEvening = phase == GamePhase.Evening || phase == GamePhase.Bar || phase == GamePhase.Night;
        for (int i = 0; i < materials.Length; i++)
            if (materials[i] != null) materials[i].SetColor("_EmissionColor", isEvening ? colors[i] : Color.black);
    }

    private void OnDestroy()
    {
        if (GameState.Current != null)
        {
            GameState.Current.PhaseChanged -= OnPhaseChanged;
        }
    }
}
