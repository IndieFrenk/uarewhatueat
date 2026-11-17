using UnityEngine;

/// <summary>Un paesano seduto al bar (capsula segnaposto). Si parla con E.</summary>
public class BarPatron : Interactable
{
    public Villager V;

    public override string Prompt =>
        V.Id == "cospirazionista" && !GameState.Current.MetConspiracist ? "Avvicina l'uomo in fondo al bar" : $"Parla con {V.Name}";

    public override bool CanInteract =>
        V != null && GameState.Current != null && GameState.Current.Phase == GamePhase.Bar && !UIKit.MenuOpen;

    public override void Interact() => BarScene.Talk(V);
}
