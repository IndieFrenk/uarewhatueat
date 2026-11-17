using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>La notte: dieta dei paesani senza acquisti, marchio, prese. Logica pura.</summary>
public static class Night
{
    /// <summary>Pesca un tipo di cibo dalle abitudini (pesi relativi).</summary>
    public static FoodKind Pick(Villager v, Random rng)
    {
        float tot = v.Habits.Values.Sum();
        if (tot <= 0) return FoodKind.Natural;
        float r = (float)rng.NextDouble() * tot;
        FoodKind last = FoodKind.Natural;
        foreach (var kv in v.Habits)
        {
            last = kv.Key;
            r -= kv.Value;
            if (r <= 0) return kv.Key;
        }
        return last;
    }

    /// <summary>Chi oggi non ha comprato nulla mangia secondo le sue abitudini (2-3 voci).</summary>
    public static void FillDiet(Villager v, Random rng)
    {
        if (v.EatenToday.Count > 0) return;
        int n = rng.Next(2, 4);
        for (int i = 0; i < n; i++) v.EatenToday.Add(Pick(v, rng)); // fumo e alcol abituali li conta già MarkRules.ApplyDay
    }

    /// <summary>Notte intera: ritorna i paesani presi stanotte (già marcati Taken e notificati).</summary>
    public static List<Villager> Resolve(GameState s)
    {
        s.RaiseDayEnding();
        foreach (var v in s.Villagers.Where(v => !v.Taken)) FillDiet(v, s.Rng);
        foreach (var v in s.Everyone().Where(v => !v.Taken)) MarkRules.ApplyDay(v);
        var taken = MarkRules.ResolveNight(s.Everyone().Where(v => !v.Taken).ToList(), s.Day, s.Rng);
        foreach (var v in taken) { v.Taken = true; v.TakenOnDay = s.Day; s.RaiseTaken(v); }
        s.Money.EndDay();
        return taken;
    }
}
