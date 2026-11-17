using System;
using System.Collections.Generic;
using UnityEngine;

public class TruckOrders : MonoBehaviour
{
    private class OrderMenu : Interactable
    {
        public override string Prompt => "Ordina al camion";
        public override void Interact() => TruckOrders.Instance?.OpenMenu();
    }

    public static TruckOrders Instance { get; private set; }

    private int currentPage;
    private int totalCost;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(TruckOrders)).AddComponent<TruckOrders>();

    void OnEnable()
    {
        Instance = this;
        if (WorldAnchors.StoreroomDrop == null) { Destroy(gameObject); return; }

        var anchor = WorldAnchors.StoreroomDrop;
        if (anchor == null) return;
        var registerGo = new GameObject("OrderBook");
        registerGo.transform.position = anchor.position + Vector3.up * 0.1f;
        registerGo.transform.SetParent(anchor.parent);

        var collider = registerGo.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.15f, 0.15f, 0.15f);
        registerGo.AddComponent<OrderMenu>();

        GameState.Current.PhaseChanged += OnPhaseChanged;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OpenMenu()
    {
        currentPage = 0;
        totalCost = 0;
        ShowPage();
    }

    void ShowPage()
    {
        const int itemsPerPage = 6;
        int start = currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, Catalog.All.Count);
        int pageCount = (Catalog.All.Count + itemsPerPage - 1) / itemsPerPage;

        var options = new List<string>();

        // Prodotti
        for (int i = start; i < end; i++)
        {
            var p = Catalog.All[i];
            int ordered = GameState.Current.PendingOrder.ContainsKey(p.Id) ? GameState.Current.PendingOrder[p.Id] : 0;
            int cost = p.Cost;
            if (p.Kind == FoodKind.Industrial)
                cost = (int)(cost * (1f - GlobalConfig.Instance.truckDiscountIndustrial));
            string offer = p.Kind == FoodKind.Industrial ? " IN OFFERTA" : "";
            options.Add($"{p.Name} — {cost} lire (ordinati: {ordered}){offer}");
        }

        // Navigazione
        if (currentPage > 0) options.Add("< Indietro");
        if (currentPage < pageCount - 1) options.Add("Avanti >");
        options.Add("Chiudi");

        string body = $"Totale ordinato: {totalCost} lire";
        UIKit.Choices("Ordine al camion", body, options.ToArray(), idx => OnMenuChoice(idx, start, end, pageCount));
    }

    void OnMenuChoice(int idx, int pageStart, int pageEnd, int pageCount)
    {
        int productCount = pageEnd - pageStart;

        // Prodotti
        if (idx < productCount)
        {
            var p = Catalog.All[pageStart + idx];
            int qty = GlobalConfig.Instance.truckOrderQuantity;
            if (!GameState.Current.PendingOrder.ContainsKey(p.Id))
                GameState.Current.PendingOrder[p.Id] = 0;
            GameState.Current.PendingOrder[p.Id] += qty;
            int unitCost = p.Cost;
            if (p.Kind == FoodKind.Industrial)
                unitCost = (int)(unitCost * (1f - GlobalConfig.Instance.truckDiscountIndustrial));
            totalCost += unitCost * qty;
            ShowPage();
            return;
        }

        int navIdx = idx - productCount;
        bool hasBack = currentPage > 0;
        bool hasNext = currentPage < pageCount - 1;

        // "< Indietro"
        if (hasBack && navIdx == 0)
        {
            currentPage--;
            ShowPage();
            return;
        }

        // "Avanti >"
        int nextIdx = hasBack ? 1 : 0;
        if (hasNext && navIdx == nextIdx)
        {
            currentPage++;
            ShowPage();
            return;
        }

        // "Chiudi"
        UIKit.CloseMenu();
    }

    void OnPhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.ShopMorning || GameState.Current.PendingOrder.Count == 0) return;

        var anchor = WorldAnchors.StoreroomDrop;
        if (anchor == null) return;
        int spent = 0;
        foreach (var entry in new Dictionary<string, int>(GameState.Current.PendingOrder))
        {
            var p = Catalog.Get(entry.Key);
            int unitCost = p == null ? 0 : (int)(p.Cost * (1f - (p.Kind == FoodKind.Industrial ? GlobalConfig.Instance.truckDiscountIndustrial : 0f)));
            if (p == null || unitCost <= 0) continue;
            // solo i pezzi che si possono pagare: il saldo non va sotto zero
            int qty = Mathf.Min(entry.Value, GameState.Current.Money.Savings / unitCost);
            int payed = GameState.Current.Money.Spend(qty * unitCost);

            for (int i = 0; i < qty; i++)
                ShopItem.Spawn(p, anchor.position + new Vector3((i % 5) * 0.25f, 0.3f + (i / 5) * 0.25f, 0f));
            spent += payed;
        }
        GameState.Current.PendingOrder.Clear();
        UIKit.Toast($"È passato il camion: consegnati {spent} lire di ordini");
    }
}
