using System;
using System.Collections.Generic;
using System.Linq;

// Mock semplice di Economy per il test (Economy.cs non è ancora implementato)
public class EconomyMock
{
    public int Savings { get; set; }
    public int StartingSavings { get; set; }

    public EconomyMock(int startingSavings)
    {
        Savings = startingSavings;
        StartingSavings = startingSavings;
    }
}

// Helper per creare GameState senza far partire il constructor di Economy
public static class TestHelper
{
    public static GameState CreateGameStateForTest(int seed, int startingSavings)
    {
        try
        {
            // Prova il constructor normale, se fallisce (Economy non implementato), usiamo reflection
            return new GameState(seed, startingSavings);
        }
        catch (NotImplementedException)
        {
            // Fallback: creo l'oggetto senza invocare il constructor
            var state = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(GameState)) as GameState;
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            state.GetType().GetField("Rng", flags).SetValue(state, new System.Random(seed));
            state.GetType().GetField("Villagers", flags).SetValue(state, new List<Villager>());
            state.GetType().GetField("Stock", flags).SetValue(state, new Dictionary<string, int>());
            state.GetType().GetField("PendingOrder", flags).SetValue(state, new Dictionary<string, int>());
            state.GetType().GetField("Clues", flags).SetValue(state, new HashSet<string>());
            // Lascio Money null, non è usato da Roster.Populate()
            return state;
        }
    }
}

public static class DataTests
{
    public static void Run()
    {
        Console.WriteLine("=== DataTests ===");

        // Test 1: Catalog ha almeno 20 prodotti con id univoci
        TestCatalogCount();

        // Test 2: Margini confrontati
        TestMargins();

        // Test 3: Almeno 4 Sweet
        TestSweets();

        // Test 4: Roster.Populate riempie tutto
        TestRosterPopulation();

        Console.WriteLine("=== Tutti i test passati ===");
    }

    private static void TestCatalogCount()
    {
        if (Catalog.All.Count < 20)
            throw new Exception($"Catalog ha {Catalog.All.Count} prodotti, minimo 20");

        var ids = Catalog.All.Select(p => p.Id).ToList();
        var uniqueIds = new HashSet<string>(ids);
        if (ids.Count != uniqueIds.Count)
            throw new Exception($"Catalog ha id duplicati");

        Console.WriteLine($"✓ Catalog: {Catalog.All.Count} prodotti, id univoci");
    }

    private static void TestMargins()
    {
        var naturals = Catalog.All.Where(p => p.Kind == FoodKind.Natural).ToList();
        var industrials = Catalog.All.Where(p => p.Kind == FoodKind.Industrial).ToList();

        if (naturals.Count == 0 || industrials.Count == 0)
            throw new Exception("Mancano prodotti Natural o Industrial");

        double naturalAvgMargin = naturals.Average(p => (double)(p.BasePrice - p.Cost) / p.Cost);
        double industrialAvgMargin = industrials.Average(p => (double)(p.BasePrice - p.Cost) / p.Cost);

        if (naturalAvgMargin >= industrialAvgMargin)
            throw new Exception($"Natural avg margin {naturalAvgMargin:F2} >= Industrial avg margin {industrialAvgMargin:F2}");

        Console.WriteLine($"✓ Margini: Natural {naturalAvgMargin:F2} < Industrial {industrialAvgMargin:F2}");
    }

    private static void TestSweets()
    {
        var sweets = Catalog.All.Count(p => p.Sweet);
        if (sweets < 4)
            throw new Exception($"Solo {sweets} prodotti Sweet, minimo 4");

        Console.WriteLine($"✓ Sweets: {sweets} prodotti");
    }

    private static void TestRosterPopulation()
    {
        // Creo GameState tramite helper che bypasssa Economy (non ancora implementato)
        var state = TestHelper.CreateGameStateForTest(seed: 1234, startingSavings: 100000);

        Roster.Populate(state);

        if (state.Protagonist == null)
            throw new Exception("Protagonist non inizializzato");
        if (state.Protagonist.Id != "protagonista")
            throw new Exception($"Protagonist id errato: {state.Protagonist.Id}");

        if (state.Daughter == null)
            throw new Exception("Daughter non inizializzato");
        if (state.Daughter.Id != "figlia")
            throw new Exception($"Daughter id errato: {state.Daughter.Id}");

        if (state.Villagers.Count < 12)
            throw new Exception($"Roster ha {state.Villagers.Count} paesani, minimo 12");

        // Verifica id univoci tra Protagonist, Daughter e Villagers
        var allIds = new List<string> { state.Protagonist.Id, state.Daughter.Id };
        allIds.AddRange(state.Villagers.Select(v => v.Id));

        var uniqueIds = new HashSet<string>(allIds);
        if (allIds.Count != uniqueIds.Count)
            throw new Exception("Roster ha id duplicati");

        // Verifica id fissi
        var fixedIds = new[] { "protagonista", "figlia", "parroco", "barista", "macellaio", "fioraia", "oste", "vecchia", "vittorio", "lucia" };
        var rosterIds = new HashSet<string>(allIds);
        foreach (var fixedId in fixedIds)
        {
            if (!rosterIds.Contains(fixedId))
                throw new Exception($"Id fisso mancante: {fixedId}");
        }

        // Verifica Habits per tutti
        foreach (var v in state.Everyone())
        {
            if (v.Habits == null || v.Habits.Count != 4)
                throw new Exception($"Villager {v.Id} Habits non completo");
        }

        Console.WriteLine($"✓ Roster: Protagonist, Daughter, {state.Villagers.Count} paesani, id univoci");
    }
}
