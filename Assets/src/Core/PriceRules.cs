using System;

/// <summary>
/// Elasticità del prezzo. Logica pura (testabile con tools/LogicTests).
/// Rapporto = prezzo / listino. Fino a +freeMargin si compra sempre; poi la probabilità scende
/// linearmente fino a 0 a +zeroAt; sopra non si compra. Sotto listino il prodotto "pesa" di più nella scelta.
/// </summary>
public static class PriceRules
{
    /// <summary>Probabilità (0..1) che il cliente compri il prodotto a quel prezzo.</summary>
    public static float BuyChance(int price, int basePrice, float freeMargin, float zeroAt)
    {
        if (basePrice <= 0) return 1f;
        float over = (float)price / basePrice - 1f;
        if (over <= freeMargin) return 1f;
        if (over >= zeroAt) return 0f;
        return 1f - (over - freeMargin) / (zeroAt - freeMargin);
    }

    /// <summary>Moltiplicatore del peso di scelta: 1 a listino o sopra, fino a 1+maxBonus a prezzo zero (sconto 100%).</summary>
    public static float DiscountWeight(int price, int basePrice, float maxBonus)
    {
        if (basePrice <= 0 || price >= basePrice) return 1f;
        return 1f + maxBonus * (1f - (float)price / basePrice);
    }

    /// <summary>Fattore sull'afflusso di clienti (1 = normale, minimo 1-maxPenalty) in base ai lamenti del giorno prima.</summary>
    public static float FootfallFactor(int complaintsYesterday, float perComplaint, float maxPenalty)
        => 1f - Math.Min(maxPenalty, Math.Max(0, complaintsYesterday) * perComplaint);
}
