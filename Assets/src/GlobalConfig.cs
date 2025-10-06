using UnityEngine;

/// <summary>
/// Rappresenta un oggetto vendibile nel negozio
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ShopItem : MonoBehaviour
{
    [Header("Item Info")]
    [Tooltip("Nome del prodotto")]
    public string itemName = "Prodotto";
    
    [Tooltip("Codice a barre univoco")]
    public string barcode = "";
    
    [Tooltip("Prezzo base suggerito")]
    public float basePrice = 1.0f;
    
    [Tooltip("Categoria del prodotto")]
    public ItemCategory category = ItemCategory.Food;
    
    [Header("Item State")]
    [Tooltip("Prezzo assegnato dal giocatore (0 = nessun prezzo)")]
    public float assignedPrice = 0f;
    
    [Tooltip("È posizionato su uno scaffale?")]
    public bool isOnShelf = false;
    
    [Tooltip("Riferimento allo scaffale su cui è posizionato")]
    public Shelf currentShelf = null;

    private Rigidbody rb;
    private Collider col;
    private bool isBeingHeld = false;
    private Transform originalParent;

    public enum ItemCategory
    {
        Food,
        Beverage,
        Cleaning,
        Personal,
        Other
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        originalParent = transform.parent;
        
        // Genera un barcode casuale se vuoto
        if (string.IsNullOrEmpty(barcode))
        {
            barcode = GenerateBarcode();
        }
    }

    /// <summary>
    /// Genera un codice a barre casuale
    /// </summary>
    string GenerateBarcode()
    {
        return Random.Range(100000000, 999999999).ToString();
    }

    /// <summary>
    /// Calcola il prezzo suggerito con variazione casuale
    /// </summary>
    public float GetSuggestedPrice()
    {
        GlobalConfig config = GlobalConfig.Instance;
        float variation = Random.Range(-config.priceSuggestionVariation, config.priceSuggestionVariation);
        return basePrice * (1f + variation);
    }

    /// <summary>
    /// Assegna un prezzo all'oggetto
    /// </summary>
    public void SetPrice(float price)
    {
        assignedPrice = price;
        Debug.Log($"{itemName}: Prezzo impostato a €{price:F2}");
    }

    /// <summary>
    /// Controlla se l'oggetto ha un prezzo
    /// </summary>
    public bool HasPrice()
    {
        return assignedPrice > 0f;
    }

    /// <summary>
    /// Rende l'oggetto afferrabile
    /// </summary>
    public void OnGrabbed()
    {
        isBeingHeld = true;
        isOnShelf = false;
        currentShelf = null;
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Disabilita solo le collisioni fisiche, mantieni il rendering
        if (col != null)
        {
            col.isTrigger = true; // Diventa trigger invece di disabilitare completamente
        }
    }

    /// <summary>
    /// Rilascia l'oggetto
    /// </summary>
    public void OnReleased()
    {
        isBeingHeld = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        if (col != null)
        {
            col.isTrigger = false; // Riabilita collisioni solide
        }
    }

    /// <summary>
    /// Posiziona l'oggetto su uno scaffale
    /// </summary>
    public void PlaceOnShelf(Shelf shelf, Vector3 position)
    {
        isOnShelf = true;
        currentShelf = shelf;
        transform.position = position;
        transform.parent = shelf.transform;
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        Debug.Log($"{itemName} posizionato su scaffale");
    }

    /// <summary>
    /// Rimuove l'oggetto dallo scaffale
    /// </summary>
    public void RemoveFromShelf()
    {
        if (currentShelf != null)
        {
            currentShelf.RemoveItem(this);
        }
        
        isOnShelf = false;
        currentShelf = null;
        transform.parent = originalParent;
    }

    /// <summary>
    /// Controlla se l'oggetto è attualmente tenuto dal giocatore
    /// </summary>
    public bool IsBeingHeld()
    {
        return isBeingHeld;
    }
}