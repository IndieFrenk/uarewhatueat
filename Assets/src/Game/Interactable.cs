using UnityEngine;

/// <summary>Qualsiasi cosa si usa con E guardandola (porte, specchio, TV, cassa, NPC...).</summary>
public abstract class Interactable : MonoBehaviour
{
    public abstract string Prompt { get; }
    public virtual bool CanInteract => true;
    public abstract void Interact();
}
