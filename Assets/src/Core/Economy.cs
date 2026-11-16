using System;

/// <summary>Soldi in lire. Il saldo non va mai sotto zero (niente bancarotta).</summary>
public class Economy
{
    public int Savings { get; private set; }
    public int StartingSavings { get; }
    public int EarnedToday, SpentToday;

    public Economy(int startingSavings) => throw new NotImplementedException();
    public void Earn(int lire) => throw new NotImplementedException();
    /// <summary>Spende al massimo quanto c'è. Ritorna la cifra spesa davvero.</summary>
    public int Spend(int lire) => throw new NotImplementedException();
    public bool CanAfford(int lire) => Savings >= lire;
    public void EndDay() { EarnedToday = 0; SpentToday = 0; }
}
