using UnityEngine;
using TMPro;

/// <summary>
/// Mostra prompt di interazione quando il giocatore può interagire con oggetti
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel del prompt")]
    public GameObject promptPanel;
    
    [Tooltip("Text del prompt")]
    public TextMeshProUGUI promptText;
    
    [Header("Prompt Messages")]
    [Tooltip("Messaggio per afferrare oggetti")]
    public string grabPrompt = "[E] Afferra";
    
    [Tooltip("Messaggio per posizionare su scaffale")]
    public string placeOnShelfPrompt = "[Click Sinistro] Posiziona su scaffale";
    
    [Tooltip("Messaggio per posizionare a terra")]
    public string placePrompt = "[Click Sinistro] Posiziona";
    
    [Tooltip("Messaggio per lanciare")]
    public string throwPrompt = "[Click Destro] Lancia";
    
    [Tooltip("Messaggio per ruotare")]
    public string rotatePrompt = "[R] Ruota";
    
    [Tooltip("Messaggio per assegnare prezzo")]
    public string setPricePrompt = "[P] Imposta prezzo";

    private PlayerInteraction playerInteraction;
    private Camera playerCamera;

    void Start()
    {
        playerInteraction = FindObjectOfType<PlayerInteraction>();
        playerCamera = Camera.main;
        
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }

    void Update()
    {
        UpdatePrompt();
    }

    /// <summary>
    /// Aggiorna il prompt in base al contesto
    /// </summary>
    void UpdatePrompt()
    {
        if (playerInteraction == null) return;

        GlobalConfig config = GlobalConfig.Instance;
        bool isHolding = playerInteraction.IsHoldingItem();

        if (isHolding)
        {
            // Sta tenendo un oggetto
            ShowHoldingPrompts();
        }
        else
        {
            // Non sta tenendo niente, controlla se può afferrare qualcosa
            CheckForGrabbableItems(config);
        }
    }

    /// <summary>
    /// Mostra i prompt quando si tiene un oggetto
    /// </summary>
    void ShowHoldingPrompts()
    {
        string message = "";
        
        // Controlla se può posizionare sullo scaffale
        if (playerInteraction.CanPlaceOnShelf())
        {
            message = placeOnShelfPrompt;
        }
        else
        {
            message = placePrompt;
        }

        message += $"\n{throwPrompt}";
        message += $"\n{rotatePrompt}";
        message += $"\n{setPricePrompt}";

        ShowPrompt(message);
    }

    /// <summary>
    /// Controlla se ci sono oggetti afferrabili
    /// </summary>
    void CheckForGrabbableItems(GlobalConfig config)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, config.grabDistance))
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            if (item != null && !item.IsBeingHeld())
            {
                string message = grabPrompt;
                
                // Aggiungi info sul prezzo se presente
                if (item.HasPrice())
                {
                    message += $"\n{item.itemName} - €{item.assignedPrice:F2}";
                }
                else
                {
                    message += $"\n{item.itemName} - Nessun prezzo";
                }
                
                ShowPrompt(message);
                return;
            }
        }

        HidePrompt();
    }

    /// <summary>
    /// Mostra il prompt con un messaggio
    /// </summary>
    void ShowPrompt(string message)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    /// <summary>
    /// Nasconde il prompt
    /// </summary>
    void HidePrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }
}