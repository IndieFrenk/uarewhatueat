using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prodotto fisico del negozio: si prende, si posa sullo scaffale, ha un prezzo in lire.
/// Il legame col catalogo è productId (Catalog.Get). Prezzo assegnato = prezzo di vendita.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ShopItem : MonoBehaviour
{
    /// <summary>Tutti i prodotti attivi nel mondo (i disattivati, es. nel carrello di un cliente, non ci sono).</summary>
    public static readonly List<ShopItem> All = new List<ShopItem>();

    [Header("Item Info")]
    public string productId = "";
    public string itemName = "Prodotto";
    public int assignedPrice = 0; // lire, 0 = nessun prezzo

    [Header("State")]
    public bool isBeingHeld = false;
    public bool isPlaced = false;

    private Rigidbody rb;
    private Collider col;

    public Product Product => string.IsNullOrEmpty(productId) ? null : Catalog.Get(productId);

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    /// <summary>Crea un prodotto segnaposto: forma e colore dipendono dal tipo.</summary>
    public static ShopItem Spawn(Product p, Vector3 pos)
    {
        PrimitiveType shape = PrimitiveType.Cube;
        Vector3 size = new Vector3(0.18f, 0.18f, 0.18f);
        Color color = new Color(0.3f, 0.75f, 0.3f); // naturale: verde
        if (p.Kind == FoodKind.Alcohol) { shape = PrimitiveType.Cylinder; size = new Vector3(0.07f, 0.13f, 0.07f); color = new Color(0.4f, 0.22f, 0.1f); }
        else if (p.Kind == FoodKind.Tobacco) { size = new Vector3(0.08f, 0.12f, 0.03f); color = Color.white; }
        else if (p.Sweet) { size = new Vector3(0.14f, 0.14f, 0.14f); color = new Color(1f, 0.6f, 0.75f); }
        else if (p.Kind == FoodKind.Industrial)
        {
            shape = PrimitiveType.Cylinder; size = new Vector3(0.1f, 0.1f, 0.1f);
            color = (p.Id.GetHashCode() & 1) == 0 ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.95f, 0.8f, 0.15f);
        }

        var go = GameObject.CreatePrimitive(shape);
        go.name = p.Name;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material.color = color;
        int layer = LayerMask.NameToLayer("ShopItem");
        if (layer >= 0) go.layer = layer;
        go.transform.position = pos + Vector3.up * go.GetComponent<Renderer>().bounds.extents.y; // pos = punto d'appoggio
        go.AddComponent<Rigidbody>();
        var item = go.AddComponent<ShopItem>();
        item.productId = p.Id;
        item.itemName = p.Name;
        return item;
    }

    /// <summary>Called when grabbed by player</summary>
    public void OnGrab()
    {
        isBeingHeld = true;
        isPlaced = false;
        rb.isKinematic = true;
        rb.useGravity = false;
        col.isTrigger = true; // trigger mentre è in mano, niente fisica strana
    }

    /// <summary>Called when placed by player</summary>
    public void OnPlace()
    {
        isBeingHeld = false;
        isPlaced = true;
        if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.isKinematic = true;
        rb.useGravity = false;
        col.isTrigger = false;
        transform.hasChanged = false;
    }

    /// <summary>Called when thrown by player</summary>
    public void OnThrow(Vector3 velocity)
    {
        isBeingHeld = false;
        isPlaced = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Random.insideUnitSphere * 2f;
        col.isTrigger = false;
    }

    /// <summary>Prezzo di listino del catalogo (0 se il prodotto non è nel catalogo).</summary>
    public int GetSuggestedPrice() => Product?.BasePrice ?? 0;

    public void SetPrice(int lire)
    {
        assignedPrice = lire;
        Debug.Log($"{itemName}: prezzo {lire} lire");
    }

    public bool HasPrice() => assignedPrice > 0;
}
