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

    // Modificatori del marchio per il cibo
    public const float NaturalGain = 0.08f;
    public const float IndustrialLoss = -0.07f;
    public const float AlcoholLoss = -0.09f;
    public const float TobaccoLoss = -0.09f;

    // Modificatori abituali
    public const float DrinkerDaily = -0.04f;
    public const float SmokerDaily = -0.04f;

    /// <summary>Aggiorna il marchio con quanto mangiato oggi (più fumo/alcol abituali) e svuota EatenToday.</summary>
    public static void ApplyDay(Villager v)
    {
        float delta = 0f;

        // Applica l'effetto del cibo mangiato
        foreach (var food in v.EatenToday)
        {
            switch (food)
            {
                case FoodKind.Natural:
                    delta += NaturalGain;
                    break;
                case FoodKind.Industrial:
                    delta += IndustrialLoss;
                    break;
                case FoodKind.Alcohol:
                    delta += AlcoholLoss;
                    break;
                case FoodKind.Tobacco:
                    delta += TobaccoLoss;
                    break;
            }
        }

        // Applica gli effetti abituali
        if (v.Drinker) delta += DrinkerDaily;
        if (v.Smoker) delta += SmokerDaily;

        // Aggiorna il marchio e clamp a 0..1
        v.Mark = Math.Max(0f, Math.Min(1f, v.Mark + delta));

        // Svuota l'elenco di quanto mangiato
        v.EatenToday.Clear();
    }

    /// <summary>Probabilità (0..1) di essere preso stanotte. Cresce coi giorni (atto 2-3) e col marchio basso.</summary>
    public static float TakeProbability(Villager v, int day)
    {
        if (v.Taken || day < v.FirstTakeDay) return 0f;

        // Base: (1 - Mark)^2
        float base_prob = (1f - v.Mark) * (1f - v.Mark);

        // Moltiplicatore per giorno
        float day_mult = day switch
        {
            1 => 0.05f,
            2 => 0.15f,
            3 => 0.2f,
            4 => 0.25f,
            5 => 0.3f,
            6 => 0.35f,
            7 => 0.4f,
            _ => 0f
        };

        float prob = base_prob * day_mult;

        // Se il marchio è visibile, max 0.02
        if (v.Mark >= VisibleThreshold)
        {
            prob = Math.Min(prob, 0.02f);
        }

        return Math.Max(0f, Math.Min(1f, prob));
    }

    /// <summary>Notte: per ogni non preso applica TakeProbability. Ritorna i presi stanotte.</summary>
    public static List<Villager> ResolveNight(IEnumerable<Villager> villagers, int day, Random rng)
    {
        var taken = new List<Villager>();

        foreach (var v in villagers)
        {
            if (!v.Taken)
            {
                float prob = TakeProbability(v, day);
                if (rng.NextDouble() < prob)
                {
                    taken.Add(v);
                }
            }
        }

        return taken;
    }
}
