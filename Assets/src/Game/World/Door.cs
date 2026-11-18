using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Porta tra due luoghi: con E, dissolvenza nera e il giocatore compare al punto d'arrivo.</summary>
public class Door : Interactable
{
    public string label = "Apri la porta";
    public Transform target;

    static bool busy;

    public override string Prompt => label;
    public override bool CanInteract => !busy && target != null;
    public override void Interact() => TeleportPlayer(target);

    /// <summary>Teletrasporto del giocatore con dissolvenza (usabile anche dagli altri sistemi: Door.TeleportPlayer(WorldAnchors.ShopSpawn)).</summary>
    public static void TeleportPlayer(Transform to)
    {
        if (busy || to == null || WorldAnchors.Player == null) return;
        UIKit.Ensure().StartCoroutine(Fade(to));
    }

    static IEnumerator Fade(Transform to)
    {
        busy = true;
        FPSController.InputBlocked = true;
        var go = UIKit.Panel(UIKit.I.Canvas.transform, Color.clear);
        UIKit.Place((RectTransform)go.transform, Vector2.zero, Vector2.one);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        float d = Mathf.Max(0.01f, GlobalConfig.Instance.doorFadeSeconds);
        for (float t = 0; t < d; t += Time.unscaledDeltaTime) { img.color = new Color(0, 0, 0, t / d); yield return null; }
        img.color = Color.black;
        Place(to);
        for (float t = 0; t < d; t += Time.unscaledDeltaTime) { img.color = new Color(0, 0, 0, 1 - t / d); yield return null; }
        Object.Destroy(go);
        FPSController.InputBlocked = false;
        busy = false;
    }

    /// <summary>Sposta il giocatore (senza dissolvenza) e lo gira come il punto d'arrivo.</summary>
    public static void Place(Transform to)
    {
        var p = WorldAnchors.Player;
        var cc = p.GetComponent<CharacterController>();
        cc.enabled = false; // altrimenti il CharacterController annulla lo spostamento
        p.SetPositionAndRotation(to.position, Quaternion.Euler(0, to.eulerAngles.y, 0));
        cc.enabled = true;
    }
}
