using UnityEngine;

/// <summary>
/// Giulia, la figlia. Mangia dolci di nascosto (più se ci sono in negozio e se il padre la lascia sola),
/// lascia segnali al mattino, e dal giorno 3 regala disegni delle fronti (pezzi della scritta).
/// </summary>
public class Daughter : Interactable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(Daughter)).AddComponent<Daughter>();

    GameState s;
    int sweetsLastNight, sweetsTotal, fruitDay, drawings;

    Villager Data => s.Daughter;

    void Start()
    {
        s = GameState.Current;
        if (WorldAnchors.HomeSpawn == null) { Destroy(gameObject); return; }
        // segnaposto: capsula da bambina, 1.2 m
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(transform, false);
        body.transform.localScale = new Vector3(0.45f, 0.6f, 0.45f);
        body.transform.localPosition = Vector3.up * 0.6f;
        body.GetComponent<Renderer>().material.color = new Color(0.85f, 0.6f, 0.55f);
        transform.position = WorldAnchors.HomeSpawn.position + new Vector3(-1.5f, 0f, 1.8f);
        s.DayEnding += OnDayEnding;
        s.PhaseChanged += OnPhase;
    }

    void OnDestroy()
    {
        if (s == null) return;
        s.DayEnding -= OnDayEnding;
        s.PhaseChanged -= OnPhase;
    }

    public override string Prompt => "Parla con Giulia";
    public override bool CanInteract => Data != null && !Data.Taken && s.Phase != GamePhase.Night;

    public override void Interact()
    {
        UIKit.Choices("Giulia", Mood(), new[] { "Guardale la fronte", "Chiedile cosa ha mangiato", "Dalle un frutto", "Chiudi" }, i =>
        {
            if (i == 0) UIKit.Toast(Forehead(Data.Mark), 5f);
            else if (i == 1) UIKit.Toast(WhatAte(), 5f);
            else if (i == 2) GiveFruit();
        });
    }

    string Mood()
    {
        int nightsHome = s.Day - 1 - s.NightsDaughterAlone;
        string mood = s.NightsDaughterAlone > nightsHome ? "Non ti guarda quasi. Ti aspettava ieri sera." : "Ti racconta della scuola, a pezzi.";
        return $"Giulia, otto anni. {mood}";
    }

    string Forehead(float mark) =>
        mark >= 0.7f ? "Il segno è netto, scuro, come disegnato col pennino." :
        mark >= 0.5f ? (s.Day >= 2 ? "Il segno c'è, ma è più chiaro di ieri." : "Il segno c'è, ma non è molto scuro.") :
        mark >= MarkRules.VisibleThreshold ? "Il segno è quasi sparito. Devi avvicinarti per vederlo." :
        "Non c'è più. La fronte è liscia, pulita. Troppo pulita.";

    string WhatAte()
    {
        if (sweetsLastNight == 0) return "«La pasta, papà. E basta.» Ti sembra vero.";
        int nightsHome = s.Day - 1 - s.NightsDaughterAlone;
        bool sincere = s.Day >= GlobalConfig.Instance.daughterHonestDay && nightsHome >= GlobalConfig.Instance.daughterNightsHome;
        return sincere
            ? "Abbassa gli occhi. «Le merendine dello scaffale in fondo. Le prendo quando tu sei al bar.»"
            : "«Niente.» Si pulisce le dita sulla gonna, in fretta.";
    }

    void GiveFruit()
    {
        if (fruitDay == s.Day) { UIKit.Toast("Ne ha già avuto uno oggi. Lo tiene sul comodino."); return; }
        fruitDay = s.Day;
        Data.EatenToday.Add(FoodKind.Natural);
        UIKit.Toast("Le dai una mela. La mangia piano, guardandoti.");
        if (s.Day >= GlobalConfig.Instance.daughterDrawStartDay && drawings < 3
            && s.Rng.NextDouble() < GlobalConfig.Instance.daughterDrawProbability
            && Story.AddFragment("disegno di Giulia " + (drawings + 1)))
        {
            drawings++;
            s.Clues.Add("disegno_giulia");
            UIKit.Toast("Ti dà un disegno: la fronte della maestra, con dei segni copiati con cura.", 5f);
        }
    }

    void OnDayEnding()
    {
        sweetsLastNight = 0;
        if (Data == null || Data.Taken) return;
        bool sweetsInShop = false;
        foreach (var kv in s.Stock)
            if (kv.Value > 0 && Catalog.Get(kv.Key)?.Sweet == true) { sweetsInShop = true; break; }
        if (!sweetsInShop) return;
        float p = GlobalConfig.Instance.daughterSweetProbability + 0.1f * s.NightsDaughterAlone;
        if (s.Rng.NextDouble() >= p) return;
        sweetsLastNight = s.Rng.Next(1, GlobalConfig.Instance.daughterSweets + 2);
        sweetsTotal += sweetsLastNight;
        for (int i = 0; i < sweetsLastNight; i++) Data.EatenToday.Add(FoodKind.Industrial);
    }

    static readonly string[] signs =
    {
        "Nello zaino di Giulia: carte di caramelle, piegate piccole piccole.",
        "La scatola delle merendine in magazzino è più leggera di ieri.",
        "Sul cuscino di Giulia, briciole di cioccolato.",
    };

    void OnPhase(GamePhase p)
    {
        if (p != GamePhase.Morning || Data == null || Data.Taken) return;
        if (sweetsLastNight > 0) UIKit.Toast(signs[(sweetsTotal - 1) % signs.Length], 6f, true);
        else if (!Data.HasVisibleMark) UIKit.Toast("Giulia dorme ancora. Le guardi la fronte: niente segno.", 6f, true);
    }
}
