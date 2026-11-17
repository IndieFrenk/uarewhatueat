using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

// Test dei finali e della dieta notturna. GameState a mano, senza Roster/MarkRules.
static class EndingsTests
{
    static void Check(bool ok, string what)
    {
        if (!ok) { Console.WriteLine("FALLITO: " + what); Environment.Exit(1); }
    }

    // Se Economy è ancora lo stub (costruttore che lancia) costruisce lo stato aggirando i costruttori.
    static GameState Make(int savings, int start)
    {
        GameState s;
        try { s = new GameState(1, start); }
        catch (NotImplementedException)
        {
            s = (GameState)RuntimeHelpers.GetUninitializedObject(typeof(GameState));
            s.Rng = new Random(1);
            s.Villagers = new List<Villager>();
            s.Money = (Economy)RuntimeHelpers.GetUninitializedObject(typeof(Economy));
            Set(s.Money, "StartingSavings", start);
        }
        Set(s.Money, "Savings", savings);
        s.Protagonist = new Villager { Id = "p" };
        s.Daughter = new Villager { Id = "d" };
        for (int i = 0; i < 10; i++) s.Villagers.Add(new Villager { Id = "v" + i });
        return s;
    }

    static void Set(object o, string prop, int value)
    {
        var f = o.GetType().GetField("<" + prop + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (f != null) f.SetValue(o, value);
    }

    static void Take(GameState s, int n) { for (int i = 0; i < n; i++) s.Villagers[i].Taken = true; }

    static void Derided(GameState s, int n) { for (int i = 0; i < n; i++) s.Villagers[i].Derided = true; }

    public static void Run()
    {
        // Perdita: figlia presa, qualunque cosa sia successa
        var s = Make(500000, 500000);
        s.Daughter.Taken = true; s.MetConspiracist = true;
        Check(Endings.Evaluate(s) == Ending.Loss, "figlia presa = Loss");

        // Salvo da soli: figlia salva, 7/10 presi, nessuna collaborazione
        s = Make(500000, 500000);
        Take(s, 7);
        s.IndustrialSoldTotal = 50; s.NaturalSoldTotal = 10;
        Check(Endings.Evaluate(s) == Ending.SavedAlone, "maggior parte presa = SavedAlone");

        // Salvezza condivisa: pochi presi, ha incontrato il cospirazionista, poco industriale
        s = Make(500000, 500000);
        Take(s, 2); s.MetConspiracist = true;
        s.IndustrialSoldTotal = 5; s.NaturalSoldTotal = 60;
        Check(Endings.Evaluate(s) == Ending.SharedSalvation, "paese salvo + collaborazione = SharedSalvation");

        // Senza cospirazionista o con tanto industriale non è condivisa
        s.MetConspiracist = false;
        Check(Endings.Evaluate(s) == Ending.SavedAlone, "senza cospirazionista = SavedAlone");
        s.MetConspiracist = true; s.IndustrialSoldTotal = 80;
        Check(Endings.Evaluate(s) == Ending.SavedAlone, "troppo industriale = SavedAlone");

        // Amaro: verità completa, risparmi a pezzi, il paese ha smesso di comprare
        s = Make(50000, 500000);
        s.SymbolFragments = GameState.FragmentsToDecode;
        Derided(s, 6);
        Check(Endings.Evaluate(s) == Ending.Bitter, "verità + fallimento + boicottaggio = Bitter");
        s.SymbolFragments = GameState.FragmentsToDecode - 1;
        Check(Endings.Evaluate(s) != Ending.Bitter, "senza verità completa non è Bitter");
        s = Make(400000, 500000);
        s.SymbolFragments = GameState.FragmentsToDecode;
        Derided(s, 6);
        Check(Endings.Evaluate(s) != Ending.Bitter, "risparmi alti non è Bitter");

        // Testi: ogni finale ha titolo e corpo; la scritta decifrata compare solo se decifrata
        s.SymbolFragments = 0;
        foreach (var e in new[] { Ending.Loss, Ending.SavedAlone, Ending.SharedSalvation, Ending.Bitter })
        {
            Check(Endings.Title(e) != "" && Endings.Body(e, s) != "", "testo " + e);
            Check(!Endings.Body(e, s).Contains("non commestibile"), "scritta non decifrata " + e);
        }
        s.SymbolFragments = GameState.FragmentsToDecode;
        Check(Endings.Body(Ending.Bitter, s).Contains("non commestibile"), "scritta decifrata");

        // Dieta notturna: 2-3 voci dalle abitudini (fumo e alcol li conta MarkRules), chi ha già mangiato non cambia
        var v = new Villager { Smoker = true, Drinker = true };
        v.Habits[FoodKind.Natural] = 1f;
        Night.FillDiet(v, new Random(3));
        Check(v.EatenToday.Count == 2 || v.EatenToday.Count == 3, "dieta 2-3 voci");
        Check(v.EatenToday.FindAll(k => k == FoodKind.Natural).Count >= 2, "pesca solo dalle abitudini");
        var w = new Villager(); w.Habits[FoodKind.Industrial] = 1f; w.EatenToday.Add(FoodKind.Natural);
        Night.FillDiet(w, new Random(3));
        Check(w.EatenToday.Count == 1, "chi ha già mangiato non viene toccato");
        var z = new Villager(); z.Habits[FoodKind.Industrial] = 9f; z.Habits[FoodKind.Natural] = 0f;
        for (int i = 0; i < 20; i++) Check(Night.Pick(z, new Random(i)) == FoodKind.Industrial, "pesi rispettati");
    }
}
