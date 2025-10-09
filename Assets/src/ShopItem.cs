using UnityEngine;

/// <summary>
/// Simple shop item that can be grabbed, placed, and priced
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ShopItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Product";
    public string barcode = "";
    public float basePrice = 1.0f;
    public float assignedPrice = 0f;

    [Header("State")]
    public bool isBeingHeld = false;
    public bool isPlaced = false;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        if (string.IsNullOrEmpty(barcode))
        {
            barcode = Random.Range(100000000, 999999999).ToString();
        }

        // Ensure correct setup
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    /// <summary>
    /// Called when grabbed by player
    /// </summary>
    public void OnGrab()
    {
        isBeingHeld = true;
        isPlaced = false;
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (col != null)
        {
            col.isTrigger = true; // Make trigger while held to avoid physics issues
        }
    }

    /// <summary>
    /// Called when placed by player
    /// </summary>
    public void OnPlace()
    {
        isBeingHeld = false;
        isPlaced = true;
        
        if (rb != null)
        {
            // CRITICAL: Stop ALL movement immediately
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Make kinematic so physics doesn't affect it
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (col != null)
        {
            col.isTrigger = false; // Solid collider so it can be grabbed again
        }

        // Force transform to stop moving
        transform.hasChanged = false;
    }

    /// <summary>
    /// Called when thrown by player
    /// </summary>
    public void OnThrow(Vector3 velocity)
    {
        isBeingHeld = false;
        isPlaced = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
        }
        
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    /// <summary>
    /// Get suggested price with variation
    /// </summary>
    public float GetSuggestedPrice()
    {
        float variation = Random.Range(-0.1f, 0.1f);
        return basePrice * (1f + variation);
    }

    /// <summary>
    /// Set price for this item
    /// </summary>
    public void SetPrice(float price)
    {
        assignedPrice = price;
        Debug.Log($"{itemName}: Price set to €{price:F2}");
    }

    /// <summary>
    /// Check if item has a price
    /// </summary>
    public bool HasPrice()
    {
        return assignedPrice > 0f;
    }
}