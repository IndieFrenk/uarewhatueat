using System;
using System.Collections.Generic;

/// <summary>
/// Regola nascosta del marchio. Logica pura (testabile con tools/LogicTests).
/// Industriale, alcol e fumo lo tolgono; naturale lo mantiene o lo fa comparire.
/// La presa è casuale: il cibo cambia la probabilità, non dà certezze.
/// </summary>
public static class MarkRules
{
    public const float VisibleThreshold = 0.35f;

    /// <summary>Aggiorna il marchio con quanto mangiato oggi (più fumo/alcol abituali) e svuota EatenToday.</summary>
    public static void ApplyDay(Villager v) => throw new NotImplementedException();

    /// <summary>Probabilità (0..1) di essere preso stanotte. Cresce coi giorni (atto 2-3) e col marchio basso.</summary>
    public static float TakeProbability(Villager v, int day) => throw new NotImplementedException();

    /// <summary>Notte: per ogni non preso applica TakeProbability. Ritorna i presi stanotte.</summary>
    public static List<Villager> ResolveNight(IEnumerable<Villager> villagers, int day, Random rng) => throw new NotImplementedException();
}
