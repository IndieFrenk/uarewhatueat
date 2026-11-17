using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlla l'emissione delle finestre accese: spente di giorno, accese di sera.
/// </summary>
public class EmissionController : MonoBehaviour
{
    private List<(Material, Color)> emissiveMats;

    public void Initialize(List<(Material, Color)> mats)
    {
        emissiveMats = new List<(Material, Color)>(mats);
        GameState.Current.PhaseChanged += OnPhaseChanged;
        // Applica lo stato iniziale
        OnPhaseChanged(GameState.Current.Phase);
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        bool isEvening = phase == GamePhase.Evening || phase == GamePhase.Bar || phase == GamePhase.Night;
        foreach (var (material, originalColor) in emissiveMats)
        {
            if (material != null)
            {
                material.SetColor("_EmissionColor", isEvening ? originalColor : Color.black);
            }
        }
    }

    private void OnDestroy()
    {
        if (GameState.Current != null)
        {
            GameState.Current.PhaseChanged -= OnPhaseChanged;
        }
    }
}
