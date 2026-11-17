using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Flusso della giornata: Morning → ShopMorning → Lunch → ShopAfternoon → Evening → (Bar) → Night → giorno dopo.
/// Le fasi di respiro (Morning, Lunch, Bar) hanno l'orologio fermo e si lasciano con N o con GameState.RequestAdvance().
/// Le fasi negozio finiscono da sole all'orario di chiusura. Sera e notte sono menu UIKit.
/// </summary>
public class GameFlow : MonoBehaviour
{
    static void Make() => new GameObject(nameof(GameFlow)).AddComponent<GameFlow>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += Make;

    GameState s;
    GlobalConfig cfg;
    Text prompt;
    GameObject blackout;
    string rollCall = ""; // chi manca all'appello, scoperto la mattina dopo

    void Start()
    {
        s = GameState.Current;
        cfg = GlobalConfig.Instance;
        if (FindAnyObjectByType<TimeManager>() == null) new GameObject(nameof(TimeManager)).AddComponent<TimeManager>();
        cfg.currentDayOfWeek = cfg.startDayOfWeek;
        prompt = UIKit.Label(UIKit.Ensure().Canvas.transform, "", 30, TextAnchor.MiddleCenter);
        UIKit.Place(prompt.rectTransform, new Vector2(0.25f, 0.03f), new Vector2(0.75f, 0.09f));
        s.AdvanceRequested += OnAdvanceRequested;
        Enter(GamePhase.Morning);
    }

    void OnDestroy()
    {
        if (s != null) s.AdvanceRequested -= OnAdvanceRequested;
        TimeManager.Paused = false;
    }

    static bool IsRest(GamePhase p) => p == GamePhase.Morning || p == GamePhase.Lunch || p == GamePhase.Bar;

    void Update()
    {
        if (s == null) return;
        bool rest = IsRest(s.Phase) && !UIKit.MenuOpen && !FPSController.InputBlocked;
        prompt.text = rest ? PromptFor(s.Phase) : "";
        if (rest && Input.GetKeyDown(KeyCode.N)) Next();
        float hour = cfg.currentHour + cfg.currentMinute / 60f;
        if (s.Phase == GamePhase.ShopMorning && hour >= cfg.lunchCloseTime) Next();
        else if (s.Phase == GamePhase.ShopAfternoon && hour >= cfg.eveningCloseTime) Next();
        else if (s.Phase == GamePhase.Bar)
        {
            TimeManager.Paused = UIKit.MenuOpen; // nei dialoghi il tempo sta fermo
            // ponytail: a 24:00 TimeManager riavvolge e cambia giorno; si chiude un soffio prima
            if (hour >= Mathf.Min(cfg.barCloseTime, 23.98f)) Next();
        }
    }

    static string PromptFor(GamePhase p)
    {
        switch (p)
        {
            case GamePhase.Morning: return "N: apri il negozio";
            case GamePhase.Lunch: return "N: riapri il negozio";
            case GamePhase.Bar: return "N: esci dal bar e vai a letto";
            default: return "";
        }
    }

    // Gli altri sistemi chiedono di avanzare solo dalle fasi di respiro.
    void OnAdvanceRequested() { if (IsRest(s.Phase)) Next(); }

    void Next()
    {
        switch (s.Phase)
        {
            case GamePhase.Morning: Enter(GamePhase.ShopMorning); break;
            case GamePhase.ShopMorning: Enter(GamePhase.Lunch); break;
            case GamePhase.Lunch: Enter(GamePhase.ShopAfternoon); break;
            case GamePhase.ShopAfternoon: Enter(GamePhase.Evening); break;
            case GamePhase.Bar: Enter(GamePhase.Night); break;
        }
    }

    void Enter(GamePhase p)
    {
        switch (p)
        {
            case GamePhase.Morning:
                Clock(cfg.wakeUpHour + cfg.wakeUpMinute / 60f, true, false);
                Teleport(WorldAnchors.HomeSpawn);
                UIKit.Toast($"Giorno {s.Day} di {GameState.LastDay}. {rollCall}", 8f); // prima di SetPhase: i segnali di Giulia si accodano
                rollCall = "";
                break;
            case GamePhase.ShopMorning: Clock(cfg.morningOpenTime, false, true); break;
            case GamePhase.Lunch: Clock(cfg.lunchCloseTime, true, false); break;
            case GamePhase.ShopAfternoon: Clock(cfg.afternoonOpenTime, false, true); break;
            case GamePhase.Evening:
                Clock(cfg.eveningCloseTime, true, false);
                Teleport(WorldAnchors.HomeSpawn);
                break;
            case GamePhase.Bar:
                Clock(cfg.barOpenTime, false, false); // l'orologio corre fino alla chiusura del bar
                cfg.isBarOpen = true;
                Teleport(WorldAnchors.BarSpawn);
                break;
            default: // Night, Ending
                TimeManager.Paused = true;
                cfg.isShopOpen = false;
                cfg.isBarOpen = false;
                break;
        }
        s.SetPhase(p);

        switch (p)
        {
            case GamePhase.ShopMorning:
            case GamePhase.ShopAfternoon:
                bool far = WorldAnchors.Player != null && WorldAnchors.ShopEntrance != null &&
                           Vector3.Distance(WorldAnchors.Player.position, WorldAnchors.ShopEntrance.position) > cfg.shopFarDistance;
                UIKit.Toast(far ? "È ora di aprire: vai al negozio." : "Il negozio è aperto.");
                break;
            case GamePhase.Lunch: UIKit.Toast("Chiuso per pranzo."); break;
            case GamePhase.Evening: Dinner(); break;
            case GamePhase.Bar: UIKit.Toast("Il bar. Tua figlia è a casa da sola."); break;
            case GamePhase.Night: DoNight(); break;
            case GamePhase.Ending: ShowEnding(); break;
        }
    }

    void Clock(float hour, bool paused, bool shopOpen)
    {
        TimeManager.SetClock(hour);
        TimeManager.Paused = paused;
        cfg.isShopOpen = shopOpen;
        cfg.isBarOpen = false;
    }

    static void Teleport(Transform at)
    {
        var p = WorldAnchors.Player;
        if (p == null || at == null) return;
        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // altrimenti il controller annulla lo spostamento
        p.SetPositionAndRotation(at.position, at.rotation);
        if (cc != null) cc.enabled = true;
    }

    // --- sera ---

    void Dinner()
    {
        UIKit.Choices("Cena per tua figlia", "Tua figlia ti aspetta a tavola.\nCosa le prepari?",
            new[] { "Pasta al sugo fatta in casa", "Merendine e surgelato", "Salti la cena" }, i =>
            {
                if (i < 2)
                {
                    var kind = i == 0 ? FoodKind.Natural : FoodKind.Industrial;
                    s.Daughter.EatenToday.Add(kind);
                    s.Protagonist.EatenToday.Add(kind);
                }
                AfterDinner();
            });
    }

    void AfterDinner()
    {
        UIKit.Choices("La serata", "Fuori è buio e il bar è aperto.\nSe esci, lei resta in casa da sola.",
            new[] { "Vai al bar", "Resti a casa con lei" }, i =>
            {
                if (i == 0)
                {
                    s.NightsDaughterAlone++;
                    Enter(GamePhase.Bar); // bere è una scelta nel menu del bar
                }
                else Enter(GamePhase.Night);
            });
    }

    // --- notte ---

    void DoNight()
    {
        // affitto e bollette: il saldo non va sotto zero (Spend si ferma a quanto c'è)
        int fixedCost = s.Money.Spend(cfg.dailyRent + cfg.dailyBills);
        UIKit.Toast($"Affitto e bollette: -{fixedCost} lire.", 5f);
        int earned = s.Money.EarnedToday, spent = s.Money.SpentToday; // Night.Resolve chiude la giornata e li azzera
        var taken = Night.Resolve(s);
        var names = taken.Where(v => v != s.Daughter && v != s.Protagonist).Select(v => v.Name).ToList();
        rollCall = names.Count == 0 ? "All'appello ci sono tutti." : "All'appello mancano: " + string.Join(", ", names) + ".";
        Blackout();
        UIKit.Choices($"Notte del giorno {s.Day}",
            $"Incasso del giorno: {earned} lire.\nSpese: {spent} lire (di cui {fixedCost} tra affitto e bollette).\nRisparmi: {s.Money.Savings} lire.\n\nIl paese dorme.",
            new[] { "Dormi" }, _ =>
            {
                CloseBlackout();
                s.Day++;
                cfg.NextDay();
                Enter(s.Daughter.Taken || s.Protagonist.Taken || s.Day > GameState.LastDay ? GamePhase.Ending : GamePhase.Morning);
            });
    }

    // --- finale ---

    void ShowEnding()
    {
        var e = Endings.Evaluate(s);
        Blackout();
        UIKit.Choices(Endings.Title(e), Endings.Body(e, s), new[] { "Ricomincia" }, _ =>
        {
            TimeManager.Paused = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    // Sfondo nero a schermo intero sotto il menu (va creato prima di UIKit.Choices).
    void Blackout()
    {
        CloseBlackout();
        blackout = UIKit.Panel(UIKit.Ensure().Canvas.transform, Color.black);
        UIKit.Place((RectTransform)blackout.transform, Vector2.zero, Vector2.one);
    }

    void CloseBlackout() { if (blackout != null) Destroy(blackout); }
}
