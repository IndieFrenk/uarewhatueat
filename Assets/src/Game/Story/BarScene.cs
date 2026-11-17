using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Il bar la sera: spawna i paesani (capsule segnaposto) sugli sgabelli e gestisce i dialoghi.
/// BarSeats si assume come punti sul pavimento davanti agli sgabelli.
/// </summary>
public static class BarScene
{
    const string Consp = "vittorio";
    const float HeedChance = 0.15f; // dar retta all'avvertimento (fioraia, maestra), cresce con i presi

    static readonly List<GameObject> spawned = new List<GameObject>();
    static readonly Dictionary<string, int> counters = new Dictionary<string, int>(); // "chat_id_giorno", "drink_id_giorno"

    static int Bump(string key) { counters.TryGetValue(key, out int n); counters[key] = n + 1; return n; }

    public static void Reset() { Clear(); counters.Clear(); }

    public static void Clear()
    {
        foreach (var g in spawned) if (g != null) UnityEngine.Object.Destroy(g);
        spawned.Clear();
    }

    // ---------------------------------------------------------------- spawn

    public static void Spawn(GameState s)
    {
        Clear();
        var seats = WorldAnchors.BarSeats;
        if (seats == null || seats.Length == 0) { Debug.LogWarning("BarScene: WorldAnchors.BarSeats vuoto, nessun paesano al bar."); return; }

        var vitt = s.Day >= Roster.ConspAppearDay ? s.Find(Consp) : null;
        if (vitt != null && vitt.Taken) vitt = null;
        int free = seats.Length - (vitt != null ? 1 : 0);

        var rng = new System.Random(s.Day * 977 + 13); // stessa sera = stessi clienti, senza toccare s.Rng
        var pool = s.Villagers.Where(v => !v.Taken && v.Id != Consp).OrderBy(_ => rng.Next()).ToList();
        var bar = pool.Find(v => v.Id == "barista"); // chi tiene aperto il bar c'è sempre, se non è stato preso
        if (bar != null) { pool.Remove(bar); pool.Insert(0, bar); }

        int n = Mathf.Min(4, Mathf.Min(free, pool.Count));
        for (int i = 0; i < n; i++) Make(pool[i], seats[i].position);
        // Vittorio sta in disparte, oltre l'ultimo sgabello
        if (vitt != null) Make(vitt, seats[seats.Length - 1].position + seats[seats.Length - 1].right * 1.2f);
    }

    static void Make(Villager v, Vector3 floorPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Paesano_" + v.Id;
        go.transform.position = floorPos + Vector3.up * 0.9f;
        go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        if (WorldAnchors.Player != null)
        {
            var d = WorldAnchors.Player.position - go.transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(d);
        }
        go.GetComponent<Renderer>().material.color = Color.HSVToRGB(Mathf.Abs(v.Id.GetHashCode() % 100) / 100f, 0.25f, 0.45f);

        if (v.HasVisibleMark) // il segno sulla fronte, un ovale scuro
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(m.GetComponent<Collider>());
            m.transform.SetParent(go.transform, false);
            m.transform.localPosition = new Vector3(0, 0.62f, 0.42f);
            m.transform.localScale = new Vector3(0.35f, 0.22f, 0.2f);
            m.GetComponent<Renderer>().material.color = new Color(0.35f, 0.05f, 0.05f);
        }
        go.AddComponent<BarPatron>().V = v;
        spawned.Add(go);
    }

    // ---------------------------------------------------------------- dialogo

    public static void Talk(Villager v)
    {
        var s = GameState.Current;
        if (s == null) return;
        int act = Story.Act(s.Day);
        string body;
        if (v.Id == Consp)
        {
            if (!s.MetConspiracist) { s.MetConspiracist = true; s.Clues.Add("consp_met"); body = StoryTexts.ConspIntro; }
            else body = StoryTexts.ConspBody;
        }
        else
        {
            body = StoryTexts.Opener(act, v.Name) + "\n\n" + StoryTexts.MarkNarration(v.Name, v.HasVisibleMark);
            var last = LastTaken(s);
            if (last != null) body += "\n\n" + StoryTexts.EmptyStool(last.Name);
        }
        Menu(v, body);
    }

    static void Menu(Villager v, string body)
    {
        var s = GameState.Current;
        var opts = new List<(string label, Func<string> act)>();
        if (v.Id == Consp)
        {
            opts.Add(("Ascolta le sue teorie", () => ConspTheory(s)));
            opts.Add(("Chiedigli della scritta", () => ConspFragment(s)));
        }
        else
        {
            opts.Add(("Chiacchierare", () => Chat(s, v)));
            if (v.HasVisibleMark && !s.Clues.Contains("frag_mark_" + v.Id)) opts.Add(("Guarda il segno sulla fronte", () => LookMark(v)));
            opts.Add(("Avvertirlo: «stanno mangiando la gente»", () => Warn(s, v)));
            opts.Add(($"Offri da bere a {v.Name} ({GlobalConfig.Instance.barRoundCost} lire)", () => Drink(s, v)));
        }
        opts.Add(("Ordina da bere per te (alcol)", () => OwnDrink(s)));
        opts.Add(("Prendi un caffè", () => StoryTexts.Coffee));
        opts.Add(("Torna a casa", () => { s.RequestAdvance(); return null; }));
        opts.Add(("Lascia stare", () => null));

        UIKit.Choices(v.Name, body, opts.Select(o => o.label).ToArray(), i =>
        {
            var reply = opts[i].act();
            if (reply != null) Menu(v, reply); // risposta + stesso menu
        });
    }

    static Villager LastTaken(GameState s) =>
        s.Villagers.Where(x => x.Taken).OrderByDescending(x => x.TakenOnDay).FirstOrDefault();

    static string Chat(GameState s, Villager v)
    {
        int act = Story.Act(s.Day);
        int n = Bump($"chat_{v.Id}_{s.Day}");
        if (n > 0 || !StoryTexts.HasChat(v.Id)) return StoryTexts.Idle(n);

        var last = LastTaken(s);
        string miss = last == null ? null : StoryTexts.MissingLine(v.Id, last.Name, s.Villagers.Count(x => x.Taken), act);
        // la cosa nuova dell'atto si dice una volta sola; i giorni dopo resta il commento su chi manca
        if (!s.Clues.Add($"bar_{v.Id}_a{act}")) return miss ?? StoryTexts.Idle(n);
        return (miss != null ? miss + "\n\n" : "") + StoryTexts.Chat(v.Id, act);
    }

    static string LookMark(Villager v) =>
        Story.AddFragment("mark_" + v.Id) ? StoryTexts.LookMark(v.Name) : "Il segno è lo stesso di prima. Non scopri niente di nuovo.";

    static string Warn(GameState s, Villager v)
    {
        if (v.Warned) return StoryTexts.AlreadyWarned(v.Name);
        v.Warned = true;
        float chance = HeedChance + 0.05f * s.Villagers.Count(x => x.Taken);
        if ((v.Id == "fioraia" || v.Id == "lucia") && s.Rng.NextDouble() < chance)
        {
            // gli dà retta: da qui in poi mangia più naturale
            v.Habits.TryGetValue(FoodKind.Natural, out float w);
            v.Habits[FoodKind.Natural] = w + 2f;
            return StoryTexts.Heeded(v.Id);
        }
        v.Derided = true;
        return StoryTexts.Derision(v.Id, v.Name);
    }

    static string Drink(GameState s, Villager v)
    {
        string key = $"drink_{v.Id}_{s.Day}";
        counters.TryGetValue(key, out int n);
        if (n >= 2) return StoryTexts.Drink(n, Story.Act(s.Day), v.Name);
        int cost = GlobalConfig.Instance.barRoundCost;
        if (!s.Money.CanAfford(cost)) return StoryTexts.NoMoney;
        s.Money.Spend(cost);
        counters[key] = n + 1;
        v.EatenToday.Add(FoodKind.Alcohol); // l'alcol toglie il marchio a lui; tu offri e basta
        return StoryTexts.Drink(n, Story.Act(s.Day), v.Name);
    }

    static string OwnDrink(GameState s)
    {
        if (Bump($"own_{s.Day}") >= 2) return StoryTexts.OwnDrinkEnough;
        s.Protagonist?.EatenToday.Add(FoodKind.Alcohol);
        return StoryTexts.OwnDrink(Story.Act(s.Day));
    }

    // ---------------------------------------------------------------- Vittorio

    static string ConspTheory(GameState s)
    {
        s.Clues.Add(StoryTexts.ConspClueId(s.Day)); // dal giorno 5: "consp_truck", la filiera del camion
        return StoryTexts.Theory(s.Day);
    }

    static string ConspFragment(GameState s)
    {
        if (!StoryTexts.ConspGivesFragment(s.Day)) return StoryTexts.ConspFragmentNone;
        return Story.AddFragment("vittorio_day" + s.Day) ? StoryTexts.ConspFragmentGiven : StoryTexts.ConspFragmentAgain;
    }
}
