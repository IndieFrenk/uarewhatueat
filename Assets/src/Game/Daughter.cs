using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Giulia, la figlia del protagonista. Mangia dolci di nascosto, il marchio sbiadisce gradualmente.
/// Interactable per parlare con lei e ricevere segnali della sua condizione.
/// </summary>
public class Daughter : Interactable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(Daughter)).AddComponent<Daughter>();

    private Villager data;
    private int lastDrawnDay;
    private int drawingsGiven;

    void Start()
    {
        // Filla il GameObject e crea la capsula
        gameObject.name = nameof(Daughter);
        if (WorldAnchors.HomeSpawn == null) { Destroy(gameObject); return; }

        // Crea capsula segnaposto (1.2m)
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.height = 1.2f;
        col.radius = 0.3f;
        col.isTrigger = true;

        gameObject.AddComponent<Rigidbody>().isKinematic = true;
        gameObject.transform.position = WorldAnchors.HomeSpawn.position + Vector3.forward * 0.5f;

        // Iscriviti ai segnali di gioco
        GameState.Current.DayEnding += OnDayEnding;
        GameState.Current.PhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (GameState.Current != null)
        {
            GameState.Current.DayEnding -= OnDayEnding;
            GameState.Current.PhaseChanged -= OnPhaseChanged;
        }
    }

    // --- Interactable ---

    public override string Prompt => "Parla con Giulia";
    public override bool CanInteract => GameState.Current?.Daughter != null;

    public override void Interact()
    {
        data = GameState.Current.Daughter;
        if (data == null) return;

        string body = DescribeDaughter();
        string[] options = new string[]
        {
            "Guardale la fronte",
            "Chiedile cosa ha mangiato",
            data.EatenToday.Count > 0 && GameState.Current.Day >= GlobalConfig.Instance.daughterDrawStartDay
                ? $"Le dai un frutto ({GetDaysWithoutFruit()} dai ultimi)" : "Le dai un frutto",
            "Chiudi"
        };

        UIKit.Choices("Giulia", body, options, idx =>
        {
            switch (idx)
            {
                case 0: DescribeForehead(); break;
                case 1: AskWhatAte(); break;
                case 2: GiveFruit(); break;
            }
        });
    }

    // --- Dialogo ---

    private string DescribeDaughter()
    {
        if (data == null) return "Non è qui.";

        return $"È mattina. Giulia sembra... " + (GameState.Current.NightsDaughterAlone > 0 ? "triste, " : "normale, ") +
            (data.EatenToday.Any(k => k == FoodKind.Industrial) ? "con le guance macchiate." : "in ordine.");
    }

    private void DescribeForehead()
    {
        if (data == null) return;

        string desc = data.Mark switch
        {
            >= 0.8f => "Un segno netto sulla sua fronte, come inchiostro blu.",
            >= 0.5f => "Il segno è ancora lì, ma più chiaro di ieri.",
            >= 0.35f => "Il marchio quasi sparito, pelle quasi normale.",
            _ => "Niente. La sua fronte è pulita, come sempre.",
        };

        UIKit.Toast(desc, 3f);
    }

    private void AskWhatAte()
    {
        if (data == null) return;

        bool ateSweets = data.EatenToday.Any(k => k == FoodKind.Industrial);
        bool shouldBeSincere = GameState.Current.Day >= GlobalConfig.Instance.daughterHonestDay
            && (GameState.LastDay - GameState.Current.Day) >= GlobalConfig.Instance.daughterNightsHome;

        string answer = ateSweets && !shouldBeSincere
            ? "Niente, papà. Ho mangiato verdure."
            : ateSweets && shouldBeSincere
                ? "Ho mangiato dolci dal negozio. Scusami, ma mi piacevano..."
                : "Ho mangiato bene oggi.";

        UIKit.Toast(answer, 4f);
    }

    private void GiveFruit()
    {
        if (data == null) return;

        data.EatenToday.Add(FoodKind.Natural);
        UIKit.Toast("Le dai un frutto. Sorride.", 2f);

        // Probabilità di ricevere un disegno
        if (GameState.Current.Day >= GlobalConfig.Instance.daughterDrawStartDay
            && GameState.Current.Rng.NextDouble() < GlobalConfig.Instance.daughterDrawProbability)
        {
            ReceiveDrawing();
        }
    }

    private void ReceiveDrawing()
    {
        GameState.Current.SymbolFragments++;
        drawingsGiven++;

        string drawing = drawingsGiven switch
        {
            1 => "Un disegno della fronte di qualcuno: segni blu confusi.",
            2 => "Un altro disegno, più nitido. I segni sembrano lettere.",
            3 => "Un terzo disegno. Riconosci parte di una parola.",
            _ => "Un altro pezzetto della scritta."
        };

        UIKit.Toast($"Giulia: '{drawing}'", 3f);
    }

    private int GetDaysWithoutFruit()
    {
        // Semplice: quanti giorni da quando l'ultima volta
        return Math.Max(1, GameState.Current.Day - lastDrawnDay);
    }

    // --- Eventi ---

    private void OnDayEnding()
    {
        if (data == null) data = GameState.Current.Daughter;
        if (data == null) return;

        // La figlia mangia dolci di nascosto con probabilità crescente
        if (ShouldEatSweets())
        {
            int quantity = GameState.Current.Rng.Next(1, 3);
            for (int i = 0; i < quantity; i++)
                data.EatenToday.Add(FoodKind.Industrial);
        }
    }

    private bool ShouldEatSweets()
    {
        // Ci sono dolci in negozio?
        bool hasSweets = false;
        foreach (var kv in GameState.Current.Stock)
        {
            var prod = Catalog.Get(kv.Key);
            if (prod != null && prod.Sweet && kv.Value > 0)
            {
                hasSweets = true;
                break;
            }
        }

        if (!hasSweets) return false;

        // Probabilità che mangi di nascosto, cresce se sola
        float prob = GlobalConfig.Instance.daughterSweetProbability;
        if (GameState.Current.NightsDaughterAlone > 0)
            prob += 0.1f * GameState.Current.NightsDaughterAlone;

        return GameState.Current.Rng.NextDouble() < prob;
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Morning || data == null) data = GameState.Current.Daughter;
        if (phase != GamePhase.Morning) return;

        // Mostra segnali al mattino
        if (data.Mark < 0.35f && lastDrawnDay != GameState.Current.Day)
        {
            UIKit.Toast("Noti che il marchio di Giulia si sbiadisce.", 3f);
            lastDrawnDay = GameState.Current.Day;
        }

        // Toast se ha mangiato dolci
        if (data.EatenToday.Any(k => k == FoodKind.Industrial))
            UIKit.Toast("Trovi wrapper di caramelle nello zaino di Giulia.", 3f);
    }
}
