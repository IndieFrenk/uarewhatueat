using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optional shelf component for visual feedback and item tracking
/// Items don't need shelves to be placed - they can be placed on any surface
/// </summary>
public class Shelf : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Renderer shelfRenderer;
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.5f, 1f, 0.5f, 1f);

    [Header("Info")]
    public int itemsOnShelf = 0;

    private Color originalColor;
    private bool isHighlighted = false;

    void Start()
    {
        if (shelfRenderer == null)
        {
            shelfRenderer = GetComponent<Renderer>();
            if (shelfRenderer == null)
                shelfRenderer = GetComponentInChildren<Renderer>();
        }

        if (shelfRenderer != null && shelfRenderer.material != null)
        {
            originalColor = shelfRenderer.material.color;
        }
    }

    void Update()
    {
        // Count items on this shelf
        CountItemsOnShelf();
    }

    /// <summary>
    /// Count how many items are placed on this shelf
    /// </summary>
    void CountItemsOnShelf()
    {
        // Get shelf bounds
        Collider shelfCollider = GetComponent<Collider>();
        if (shelfCollider == null) return;

        Bounds bounds = shelfCollider.bounds;
        
        // Expand bounds slightly upward to catch items on top
        bounds.Expand(new Vector3(0f, 0.2f, 0f));

        // Find all items in this area
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents);
        
        int count = 0;
        foreach (Collider col in colliders)
        {
            ShopItem item = col.GetComponent<ShopItem>();
            if (item != null && item.isPlaced)
            {
                count++;
            }
        }

        itemsOnShelf = count;
    }

    /// <summary>
    /// Highlight this shelf
    /// </summary>
    public void Highlight(bool highlight)
    {
        isHighlighted = highlight;

        if (shelfRenderer != null && shelfRenderer.material != null)
        {
            if (highlight)
            {
                shelfRenderer.material.color = highlightColor;
            }
            else
            {
                shelfRenderer.material.color = originalColor;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Bounds bounds = col.bounds;
            bounds.Expand(new Vector3(0f, 0.2f, 0f));
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}