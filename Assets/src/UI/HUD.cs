using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    Text dayLabel, timeLabel, phaseLabel;
    Text moneyLabel, incomeLabel;
    Text crosshair, interactionPrompt;
    GameObject hudContainer;

    string[] dayNames = { "Lunedì", "Martedì", "Mercoledì", "Giovedì", "Venerdì", "Sabato", "Domenica" };
    string[] phaseNames = { "Mattina", "Negozio aperto", "Pausa pranzo", "Negozio aperto", "Sera", "Bar", "Notte", "Fine" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(HUD)).AddComponent<HUD>();

    void OnEnable()
    {
        UIKit.Ensure();
        hudContainer = new GameObject("HUDContainer");
        hudContainer.transform.SetParent(UIKit.I.Canvas.transform, false);

        // Alto sinistra: giorno
        dayLabel = UIKit.Label(hudContainer.transform, "", 24, TextAnchor.UpperLeft);
        UIKit.Place(dayLabel.rectTransform, new Vector2(0.02f, 0.9f), new Vector2(0.3f, 0.98f));

        // Alto sinistra: ora
        timeLabel = UIKit.Label(hudContainer.transform, "", 20, TextAnchor.UpperLeft);
        UIKit.Place(timeLabel.rectTransform, new Vector2(0.02f, 0.82f), new Vector2(0.25f, 0.9f));

        // Alto sinistra: fase
        phaseLabel = UIKit.Label(hudContainer.transform, "", 16, TextAnchor.UpperLeft);
        UIKit.Place(phaseLabel.rectTransform, new Vector2(0.02f, 0.75f), new Vector2(0.25f, 0.82f));

        // Alto destra: soldi
        moneyLabel = UIKit.Label(hudContainer.transform, "", 24, TextAnchor.UpperRight);
        UIKit.Place(moneyLabel.rectTransform, new Vector2(0.7f, 0.9f), new Vector2(0.98f, 0.98f));

        // Alto destra: incasso
        incomeLabel = UIKit.Label(hudContainer.transform, "", 20, TextAnchor.UpperRight);
        UIKit.Place(incomeLabel.rectTransform, new Vector2(0.7f, 0.82f), new Vector2(0.98f, 0.9f));

        // Centro: mirino
        crosshair = UIKit.Label(hudContainer.transform, ".", 36, TextAnchor.MiddleCenter);
        UIKit.Place(crosshair.rectTransform, new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f));

        // Sotto mirino: prompt interazione
        interactionPrompt = UIKit.Label(hudContainer.transform, "", 20, TextAnchor.MiddleCenter);
        UIKit.Place(interactionPrompt.rectTransform, new Vector2(0.25f, 0.4f), new Vector2(0.75f, 0.48f));

        // Sottoscrizione ai messaggi
        if (GameState.Current != null)
            GameState.Current.Message += msg => UIKit.Toast(msg);
    }

    void Update()
    {
        // Nascondi HUD se menu aperto
        if (hudContainer != null)
            hudContainer.SetActive(!UIKit.MenuOpen);

        if (GameState.Current == null) return;

        // Aggiorna giorno e ora
        var cfg = GlobalConfig.Instance;
        dayLabel.text = $"Giorno {GameState.Current.Day}/7 — {dayNames[cfg.currentDayOfWeek]}";
        timeLabel.text = $"{(int)cfg.currentHour:D2}:{(int)cfg.currentMinute:D2}";

        // Aggiorna fase
        phaseLabel.text = phaseNames[(int)GameState.Current.Phase];

        // Aggiorna soldi
        int savings = GameState.Current.Money.Savings;
        string savingsStr = FormatMoney(savings);
        int earned = GameState.Current.Money.EarnedToday;
        moneyLabel.text = $"{savingsStr} lire";
        incomeLabel.text = $"Incasso: {FormatMoney(earned)} lire";

        // Aggiorna prompt
        if (PlayerInteraction.Looking != null && PlayerInteraction.Looking.CanInteract)
            interactionPrompt.text = $"E: {PlayerInteraction.Looking.Prompt}";
        else
            interactionPrompt.text = "";
    }

    string FormatMoney(int value)
    {
        if (value < 1000)
            return value.ToString();
        return (value / 1000).ToString() + "." + (value % 1000).ToString("D3");
    }
}
