using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rappresenta uno scaffale dove posizionare gli oggetti
/// </summary>
public class Shelf : MonoBehaviour
{
    [Header("Shelf Settings")]
    [Tooltip("Numero massimo di oggetti su questo scaffale")]
    public int maxItems = 10;
    
    [Tooltip("Spaziatura tra gli oggetti")]
    public float itemSpacing = 0.3f;
    
    [Tooltip("Layer degli oggetti su questo scaffale")]
    public LayerMask itemLayer;
    
    [Header("Visual Feedback")]
    [Tooltip("Colore quando lo scaffale è disponibile")]
    public Color availableColor = Color.green;
    
    [Tooltip("Colore quando lo scaffale è pieno")]
    public Color fullColor = Color.red;
    
    [Tooltip("Renderer dello scaffale per feedback visivo")]
    public Renderer shelfRenderer;

    private List<ShopItem> itemsOnShelf = new List<ShopItem>();
    private List<Vector3> slotPositions = new List<Vector3>();
    private bool isHighlighted = false;

    void Start()
    {
        GenerateSlotPositions();
    }

    /// <summary>
    /// Genera le posizioni degli slot sullo scaffale
    /// </summary>
    void GenerateSlotPositions()
    {
        slotPositions.Clear();
        
        // Calcola le dimensioni dello scaffale
        Bounds bounds = GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        float shelfWidth = bounds.size.x;
        float startX = -shelfWidth / 2f;
        
        for (int i = 0; i < maxItems; i++)
        {
            float xPos = startX + (i * itemSpacing) + (itemSpacing / 2f);
            Vector3 localSlotPos = new Vector3(xPos, 0.1f, 0f);
            slotPositions.Add(localSlotPos);
        }
    }

    /// <summary>
    /// Trova lo slot più vicino disponibile
    /// </summary>
    public Vector3 GetNearestAvailableSlot(Vector3 worldPosition, out bool hasSlot)
    {
        hasSlot = false;
        
        if (itemsOnShelf.Count >= maxItems)
        {
            return Vector3.zero;
        }

        Vector3 nearestSlot = Vector3.zero;
        float nearestDistance = float.MaxValue;

        foreach (Vector3 localSlot in slotPositions)
        {
            Vector3 worldSlot = transform.TransformPoint(localSlot);
            
            // Controlla se lo slot è occupato
            bool occupied = false;
            foreach (ShopItem item in itemsOnShelf)
            {
                if (Vector3.Distance(item.transform.position, worldSlot) < 0.2f)
                {
                    occupied = true;
                    break;
                }
            }
            
            if (!occupied)
            {
                float distance = Vector3.Distance(worldPosition, worldSlot);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = worldSlot;
                    hasSlot = true;
                }
            }
        }

        return nearestSlot;
    }

    /// <summary>
    /// Aggiunge un oggetto allo scaffale
    /// </summary>
    public bool AddItem(ShopItem item, Vector3 position)
    {
        if (itemsOnShelf.Count >= maxItems)
        {
            Debug.Log("Scaffale pieno!");
            return false;
        }

        if (!itemsOnShelf.Contains(item))
        {
            itemsOnShelf.Add(item);
            item.PlaceOnShelf(this, position);
            UpdateVisualFeedback();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Rimuove un oggetto dallo scaffale
    /// </summary>
    public void RemoveItem(ShopItem item)
    {
        if (itemsOnShelf.Contains(item))
        {
            itemsOnShelf.Remove(item);
            UpdateVisualFeedback();
        }
    }

    /// <summary>
    /// Evidenzia lo scaffale
    /// </summary>
    public void Highlight(bool highlight)
    {
        isHighlighted = highlight;
        UpdateVisualFeedback();
    }

    /// <summary>
    /// Aggiorna il feedback visivo dello scaffale
    /// </summary>
    void UpdateVisualFeedback()
    {
        if (shelfRenderer == null) return;

        if (!isHighlighted)
        {
            // Ripristina il materiale originale
            shelfRenderer.material.color = Color.white;
            return;
        }

        // Mostra colore in base alla disponibilità
        Color targetColor = itemsOnShelf.Count < maxItems ? availableColor : fullColor;
        shelfRenderer.material.color = targetColor;
    }

    /// <summary>
    /// Ottiene tutti gli oggetti sullo scaffale
    /// </summary>
    public List<ShopItem> GetItems()
    {
        return new List<ShopItem>(itemsOnShelf);
    }

    /// <summary>
    /// Controlla se lo scaffale ha spazio
    /// </summary>
    public bool HasSpace()
    {
        return itemsOnShelf.Count < maxItems;
    }

    /// <summary>
    /// Ottiene il numero di oggetti sullo scaffale
    /// </summary>
    public int GetItemCount()
    {
        return itemsOnShelf.Count;
    }
}