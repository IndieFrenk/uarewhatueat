using System;

static class PriceTests
{
    static void Check(bool cond, string msg)
    {
        if (!cond) throw new Exception($"Test fallito: {msg}");
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < 1e-4f;

    public static void Run()
    {
        const float free = 0.05f, zero = 0.40f;
        float P(int price) => PriceRules.BuyChance(price, 100, free, zero);

        Check(Near(P(100), 1f), "a listino si compra sempre");
        Check(Near(P(70), 1f), "sotto listino si compra sempre");
        Check(Near(P(105), 1f), "+5% ancora sempre");
        Check(P(120) > 0f && P(120) < 1f, "+20% probabilità intermedia");
        Check(Near(P(120), 1f - 0.15f / 0.35f), "+20% è sulla retta");
        Check(P(110) > P(120) && P(120) > P(130), "la probabilità scende col prezzo");
        Check(Near(P(140), 0f), "+40% nessuno compra");
        Check(Near(P(200), 0f), "oltre +40% nessuno compra");
        Check(Near(PriceRules.BuyChance(50, 0, free, zero), 1f), "senza listino non filtra");

        Check(Near(PriceRules.DiscountWeight(100, 100, 0.5f), 1f), "nessun bonus a listino");
        Check(Near(PriceRules.DiscountWeight(150, 100, 0.5f), 1f), "nessun bonus sopra listino");
        Check(Near(PriceRules.DiscountWeight(80, 100, 0.5f), 1.1f), "-20% = peso +10%");

        Check(Near(PriceRules.FootfallFactor(0, 0.1f, 0.5f), 1f), "nessun lamento = afflusso normale");
        Check(Near(PriceRules.FootfallFactor(3, 0.1f, 0.5f), 0.7f), "3 lamenti = -30%");
        Check(Near(PriceRules.FootfallFactor(99, 0.1f, 0.5f), 0.5f), "il calo ha un tetto");
    }
}
