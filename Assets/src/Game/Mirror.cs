using UnityEngine;

/// <summary>Lo specchio di casa: l'unico modo di vedere la propria fronte.</summary>
public class Mirror : Interactable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () =>
    {
        var go = GameObject.Find("Specchio"); // costruito da WorldBuilder
        if (go == null) return;
        if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
        go.AddComponent<Mirror>();
    };

    public override string Prompt => "Guardati allo specchio";
    public override bool CanInteract => GameState.Current?.Protagonist != null;

    public override void Interact()
    {
        float m = GameState.Current.Protagonist.Mark;
        UIKit.Toast(
            m >= 0.7f ? "Sulla fronte hai un segno scuro, netto. Non sai da quando c'è." :
            m >= 0.5f ? "Il segno c'è. Più chiaro di come te lo ricordavi." :
            m >= MarkRules.VisibleThreshold ? "Un'ombra sulla fronte. Ti avvicini: forse è un segno, forse è stanchezza." :
            "La fronte è pulita. Hai gli occhi di uno che beve troppo.", 5f);
    }
}
