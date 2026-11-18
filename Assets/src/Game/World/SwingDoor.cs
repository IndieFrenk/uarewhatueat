using UnityEngine;

/// <summary>Porta a battente (magazzino): E apre e chiude. Il pannello è un figlio con il collider.</summary>
public class SwingDoor : Interactable
{
    public string label = "la porta";
    bool open;

    public override string Prompt => (open ? "Chiudi " : "Apri ") + label;
    public override void Interact()
    {
        open = !open;
        transform.localRotation = Quaternion.Euler(0, open ? 90 : 0, 0);
    }
}
