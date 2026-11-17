using UnityEngine;

/// <summary>La TV di casa: di mattina si può guardare il TG. Ogni TG visto lascia l'indizio tv_dayN.</summary>
public class TvSet : Interactable
{
    public override string Prompt => "Accendi la TV";

    public override bool CanInteract =>
        GameState.Current != null && GameState.Current.Phase == GamePhase.Morning && !UIKit.MenuOpen;

    public override void Interact() => Show(0);

    void Show(int page)
    {
        var s = GameState.Current;
        var pages = StoryTexts.Tg(s.Day);
        s.Clues.Add($"tv_day{s.Day}");
        bool last = page >= pages.Length - 1;
        var options = last ? new[] { "Spegni la TV" } : new[] { "Continua a guardare", "Spegni la TV" };
        UIKit.Choices($"Telegiornale del mattino - giorno {s.Day}", pages[page], options, i =>
        {
            if (!last && i == 0) Show(page + 1);
        });
    }
}
