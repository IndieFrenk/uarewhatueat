using UnityEngine;

/// <summary>
/// Spawna lo stock iniziale sugli scaffali e tiene GameState.Stock = pezzi sugli scaffali con prezzo assegnato.
/// </summary>
public class ShopStock : MonoBehaviour
{
    float next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(ShopStock)).AddComponent<ShopStock>();

    void Start()
    {
        var slots = WorldAnchors.ShelfSlots;
        int n = Catalog.All.Count;
        if (slots != null && n > 0)
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                var p = Catalog.All[i % n];
                var it = ShopItem.Spawn(p, slots[i].position);
                it.SetPrice(p.BasePrice); // lo stock iniziale parte a listino
                it.OnPlace();
            }
        Refresh();
    }

    // ponytail: scansione a 1 Hz, basta per pochi decine di pezzi; eventi se diventano centinaia.
    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 1f;
        Refresh();
    }

    /// <summary>Ricalcola GameState.Stock. Chiamato anche da chi toglie o prezza prodotti.</summary>
    public static void Refresh()
    {
        var stock = GameState.Current?.Stock;
        if (stock == null) return;
        stock.Clear();
        foreach (var it in ShopItem.All)
        {
            if (!it.isPlaced || !it.HasPrice() || it.Product == null) continue;
            stock.TryGetValue(it.productId, out int n);
            stock[it.productId] = n + 1;
        }
    }
}
