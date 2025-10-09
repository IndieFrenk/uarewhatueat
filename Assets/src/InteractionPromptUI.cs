using UnityEngine;
using TMPro;

/// <summary>
/// Shows interaction prompts for player
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("Prompt Messages")]
    public string grabPrompt = "[E] Grab";
    public string placePrompt = "[Left Click] Place";
    public string placeInvalidPrompt = "[Left Click] Place (find valid surface)";
    public string throwPrompt = "[Right Click] Throw";
    public string rotatePrompt = "[R] Rotate";
    public string setPricePrompt = "[P] Set Price";

    private PlayerInteraction playerInteraction;
    private Camera playerCamera;

    void Start()
    {
        playerInteraction = FindObjectOfType<PlayerInteraction>();
        playerCamera = Camera.main;

        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    void Update()
    {
        if (playerInteraction == null) return;

        if (playerInteraction.IsHoldingItem())
        {
            ShowHoldingPrompts();
        }
        else
        {
            ShowGrabPrompt();
        }
    }

    /// <summary>
    /// Show prompts when holding item
    /// </summary>
    void ShowHoldingPrompts()
    {
        string message = "";
        
        if (playerInteraction.CanPlace())
        {
            message = placePrompt;
        }
        else
        {
            message = placeInvalidPrompt;
        }

        message += $"\n{throwPrompt}";
        message += $"\n{rotatePrompt}";
        message += $"\n{setPricePrompt}";

        ShowPrompt(message);
    }

    /// <summary>
    /// Show grab prompt when looking at item
    /// </summary>
    void ShowGrabPrompt()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, playerInteraction.grabDistance))
        {
            ShopItem item = hit.collider.GetComponent<ShopItem>();
            
            if (item != null && !item.isBeingHeld)
            {
                string message = grabPrompt;
                message += $"\n{item.itemName}";
                
                if (item.HasPrice())
                {
                    message += $" - €{item.assignedPrice:F2}";
                }
                else
                {
                    message += " - No price";
                }

                ShowPrompt(message);
                return;
            }
        }

        HidePrompt();
    }

    /// <summary>
    /// Show prompt
    /// </summary>
    void ShowPrompt(string message)
    {
        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    /// <summary>
    /// Hide prompt
    /// </summary>
    void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }
}