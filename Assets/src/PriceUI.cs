using System;
using UnityEngine;

/// <summary>
/// Menu per assegnare il prezzo (in lire) al prodotto in mano. Si apre con P (vedi PlayerInteraction).
/// Costruito con UIKit: prezzi proposti rispetto al listino.
/// </summary>
public class PriceUI : MonoBehaviour
{
    /// <summary>Oggetto in mano e prezzo di listino.</summary>
    public static Action<ShopItem, int> ShowPriceMenu;

    static readonly float[] Mult = { 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.5f };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () =>
    {
        if (FindAnyObjectByType<PriceUI>() == null) new GameObject(nameof(PriceUI)).AddComponent<PriceUI>();
    };

    void OnEnable() => ShowPriceMenu += Show;
    void OnDisable() => ShowPriceMenu -= Show;

    void Show(ShopItem item, int basePrice)
    {
        if (item == null) return;
        if (basePrice <= 0) { UIKit.Toast("Questo prodotto non ha un prezzo di listino."); return; }

        var prices = new int[Mult.Length];
        var labels = new string[Mult.Length + 1];
        for (int i = 0; i < Mult.Length; i++)
        {
            prices[i] = Mathf.Max(10, Mathf.RoundToInt(basePrice * Mult[i] / 10f) * 10);
            int pct = Mathf.RoundToInt((Mult[i] - 1f) * 100f);
            labels[i] = (pct == 0 ? "Listino" : (pct > 0 ? "+" : "") + pct + "%") + ":  " + prices[i] + " lire";
        }
        labels[Mult.Length] = "Annulla";

        float range = GlobalConfig.Instance != null ? GlobalConfig.Instance.priceAcceptanceRange : 0.2f;
        int cost = item.Product != null ? item.Product.Cost : 0;
        string body = $"Listino: {basePrice} lire. Costo d'acquisto: {cost} lire.\n" +
                      $"Oltre {Mathf.RoundToInt(basePrice * (1f + range))} lire i clienti dicono \"troppo caro\".\n" +
                      (item.HasPrice() ? $"Prezzo attuale: {item.assignedPrice} lire." : "Nessun prezzo assegnato.");

        UIKit.Choices(item.itemName, body, labels, i =>
        {
            if (i >= prices.Length || item == null) return;
            item.SetPrice(prices[i]);
            UIKit.Toast($"{item.itemName}: {prices[i]} lire");
            ShopStock.Refresh();
        });
    }
}
