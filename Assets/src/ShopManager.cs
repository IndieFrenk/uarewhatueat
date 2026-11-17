using UnityEngine;

/// <summary>
/// Porta del negozio: con E vicino alla porta si apre (mattina e dopo pranzo).
/// Il flusso vero (fasi, orari di chiusura, isShopOpen) sta in GameFlow: qui si chiede solo di avanzare.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Tooltip("Transform della porta del negozio")]
    public Transform shopDoorTransform;

    [Tooltip("Distanza massima dalla porta per aprire")]
    public float doorInteractionDistance = 3f;

    void Update()
    {
        var s = GameState.Current;
        if (s == null || shopDoorTransform == null || WorldAnchors.Player == null) return;
        if (s.Phase != GamePhase.Morning && s.Phase != GamePhase.Lunch) return;
        if (FPSController.InputBlocked || !Input.GetKeyDown(KeyCode.E)) return;
        if (Vector3.Distance(WorldAnchors.Player.position, shopDoorTransform.position) <= doorInteractionDistance)
            s.RequestAdvance();
    }
}
