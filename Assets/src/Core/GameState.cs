using System;
using System.Collections.Generic;

public enum GamePhase { Morning, ShopMorning, Lunch, ShopAfternoon, Evening, Bar, Night, Ending }

public enum Ending { None, Loss, SavedAlone, SharedSalvation, Bitter }

/// <summary>
/// Stato della partita, logica pura. Unico punto di verità condiviso dai sistemi.
/// Creato da GameBootstrap (Unity) o dai test.
/// </summary>
public class GameState
{
    public static GameState Current;

    public const int LastDay = 7;
    public int Day = 1;
    public GamePhase Phase = GamePhase.Morning;
    public Economy Money;
    public Random Rng;

    public Villager Protagonist;
    public Villager Daughter;
    public List<Villager> Villagers = new List<Villager>(); // paesani, esclusi protagonista e figlia

    /// <summary>Prodotti in vendita stasera nel negozio (id → pezzi). Aggiornato dal negozio.</summary>
    public Dictionary<string, int> Stock = new Dictionary<string, int>();
    /// <summary>Ordini al camion, consegnati la mattina dopo (id → pezzi).</summary>
    public Dictionary<string, int> PendingOrder = new Dictionary<string, int>();

    public HashSet<string> Clues = new HashSet<string>();   // indizi raccolti ("tv_day2", "consp_truck", ...)
    public int SymbolFragments;                              // pezzi della scritta (servono per decifrare)
    public const int FragmentsToDecode = 5;
    public bool MetConspiracist;
    public int IndustrialSoldTotal, NaturalSoldTotal;
    public int NightsDaughterAlone;                          // sere al bar lasciandola sola

    public event Action<GamePhase> PhaseChanged;
    public event Action<Villager> VillagerTaken;
    public event Action<string> Message;                     // notifica a schermo

    public GameState(int seed, int startingSavings)
    {
        Rng = new Random(seed);
        Money = new Economy(startingSavings);
    }

    public void SetPhase(GamePhase p) { Phase = p; PhaseChanged?.Invoke(p); }
    public void RaiseTaken(Villager v) => VillagerTaken?.Invoke(v);
    public void Say(string msg) => Message?.Invoke(msg);

    public Villager Find(string id) => Villagers.Find(v => v.Id == id);
    public IEnumerable<Villager> Everyone()
    {
        yield return Protagonist;
        yield return Daughter;
        foreach (var v in Villagers) yield return v;
    }
}
