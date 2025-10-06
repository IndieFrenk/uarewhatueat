using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Gestisce la UI per assegnare prezzi agli oggetti
/// </summary>
public class PriceUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel del menu prezzi")]
    public GameObject priceMenuPanel;
    
    [Tooltip("Text per il nome dell'oggetto")]
    public TextMeshProUGUI itemNameText;
    
    [Tooltip("Text per il prezzo suggerito")]
    public TextMeshProUGUI suggestedPriceText;
    
    [Tooltip("Input field per il prezzo personalizzato")]
    public TMP_InputField priceInputField;
    
    [Tooltip("Bottone per confermare")]
    public Button confirmButton;
    
    [Tooltip("Bottone per usare il prezzo suggerito")]
    public Button useSuggestedButton;
    
    [Tooltip("Bottone per annullare")]
    public Button cancelButton;

    [Header("Price Tag Prefab")]
    [Tooltip("Prefab del cartellino del prezzo")]
    public GameObject priceTagPrefab;

    // Eventi
    public static Action<ShopItem, float> ShowPriceMenu;

    private ShopItem currentItem = null;
    private float suggestedPrice = 0f;

    void Start()
    {
        if (priceMenuPanel != null)
        {
            priceMenuPanel.SetActive(false);
        }

        // Sottoscrivi agli eventi
        ShowPriceMenu += OnShowPriceMenu;

        // Setup bottoni
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmPrice);
        }

        if (useSuggestedButton != null)
        {
            useSuggestedButton.onClick.AddListener(OnUseSuggestedPrice);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancel);
        }
    }

    void OnDestroy()
    {
        ShowPriceMenu -= OnShowPriceMenu;
    }

    /// <summary>
    /// Mostra il menu per assegnare il prezzo
    /// </summary>
    void OnShowPriceMenu(ShopItem item, float suggested)
    {
        if (item == null) return;

        currentItem = item;
        suggestedPrice = suggested;

        // Mostra il panel
        if (priceMenuPanel != null)
        {
            priceMenuPanel.SetActive(true);
        }

        // Aggiorna i testi
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if (suggestedPriceText != null)
        {
            suggestedPriceText.text = $"Prezzo suggerito: €{suggested:F2}";
        }

        // Resetta l'input field
        if (priceInputField != null)
        {
            priceInputField.text = suggested.ToString("F2");
            priceInputField.Select();
            priceInputField.ActivateInputField();
        }

        // Pausa il gioco o disabilita controlli
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Conferma il prezzo inserito
    /// </summary>
    void OnConfirmPrice()
    {
        if (currentItem == null) return;

        float price = 0f;
        
        if (priceInputField != null && float.TryParse(priceInputField.text, out price))
        {
            if (price > 0f)
            {
                currentItem.SetPrice(price);
                CreatePriceTag(currentItem, price);
                CloseMenu();
            }
            else
            {
                Debug.LogWarning("Il prezzo deve essere maggiore di 0!");
            }
        }
        else
        {
            Debug.LogWarning("Prezzo non valido!");
        }
    }

    /// <summary>
    /// Usa il prezzo suggerito
    /// </summary>
    void OnUseSuggestedPrice()
    {
        if (currentItem == null) return;

        currentItem.SetPrice(suggestedPrice);
        CreatePriceTag(currentItem, suggestedPrice);
        CloseMenu();
    }

    /// <summary>
    /// Annulla e chiudi il menu
    /// </summary>
    void OnCancel()
    {
        CloseMenu();
    }

    /// <summary>
    /// Chiude il menu prezzi
    /// </summary>
    void CloseMenu()
    {
        if (priceMenuPanel != null)
        {
            priceMenuPanel.SetActive(false);
        }

        currentItem = null;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Crea un cartellino del prezzo visivo sull'oggetto
    /// </summary>
    void CreatePriceTag(ShopItem item, float price)
    {
        if (priceTagPrefab == null || item == null) return;

        // Rimuovi vecchio cartellino se esiste
        PriceTag existingTag = item.GetComponentInChildren<PriceTag>();
        if (existingTag != null)
        {
            Destroy(existingTag.gameObject);
        }

        // Crea nuovo cartellino
        GameObject tagObject = Instantiate(priceTagPrefab, item.transform);
        tagObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        tagObject.transform.localRotation = Quaternion.identity;

        PriceTag tag = tagObject.GetComponent<PriceTag>();
        if (tag != null)
        {
            tag.SetPrice(price);
        }
    }
}