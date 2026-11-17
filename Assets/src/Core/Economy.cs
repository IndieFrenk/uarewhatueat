using System;

/// <summary>Soldi in lire. Il saldo non va mai sotto zero (niente bancarotta).</summary>
public class Economy
{
    public int Savings { get; private set; }
    public int StartingSavings { get; }
    public int EarnedToday, SpentToday;

    public Economy(int startingSavings)
    {
        Savings = startingSavings;
        StartingSavings = startingSavings;
    }

    public void Earn(int lire)
    {
        if (lire <= 0) return;
        Savings += lire;
        EarnedToday += lire;
    }

    /// <summary>Spende al massimo quanto c'è. Ritorna la cifra spesa davvero.</summary>
    public int Spend(int lire)
    {
        int speso = Math.Min(lire, Savings);
        Savings -= speso;
        SpentToday += speso;
        return speso;
    }
    public bool CanAfford(int lire) => Savings >= lire;
    public void EndDay() { EarnedToday = 0; SpentToday = 0; }
}
