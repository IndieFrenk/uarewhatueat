using System.Linq;

/// <summary>Finali del gioco, secondo TRAMA.md. Logica pura: valuta lo stato a fine partita.</summary>
public static class Endings
{
    /// <summary>Più della metà dei paesani presi = «la maggior parte del paese».</summary>
    public const float MajorityTaken = 0.5f;
    /// <summary>Quota massima di industriale sul venduto per dire «ha venduto poco industriale».</summary>
    public const float LowIndustrialShare = 0.35f;
    /// <summary>Amaro: risparmi finali al massimo questa quota di quelli di partenza.</summary>
    public const float BitterSavingsFraction = 0.3f;
    /// <summary>Amaro: quota di paesani che l'hanno deriso (e non comprano più) per dire «il paese non compra più».</summary>
    public const float BitterBoycottShare = 0.5f;

    public static bool Decoded(GameState s) => s.SymbolFragments >= GameState.FragmentsToDecode;

    public static Ending Evaluate(GameState s)
    {
        if (s.Daughter.Taken) return Ending.Loss;
        int n = s.Villagers.Count;
        float taken = n == 0 ? 0 : s.Villagers.Count(v => v.Taken) / (float)n;
        float boycott = n == 0 ? 0 : s.Villagers.Count(v => v.Derided) / (float)n;
        int sold = s.IndustrialSoldTotal + s.NaturalSoldTotal;
        float industrialShare = sold == 0 ? 0 : s.IndustrialSoldTotal / (float)sold;

        if (Decoded(s) && s.Money.Savings <= s.Money.StartingSavings * BitterSavingsFraction && boycott >= BitterBoycottShare)
            return Ending.Bitter;
        if (taken <= MajorityTaken && s.MetConspiracist && industrialShare <= LowIndustrialShare)
            return Ending.SharedSalvation;
        return Ending.SavedAlone; // figlia salva senza collaborazione (o paese perso)
    }

    public static string Title(Ending e)
    {
        switch (e)
        {
            case Ending.Loss: return "Il posto vuoto";
            case Ending.SavedAlone: return "Salvi, ma soli";
            case Ending.SharedSalvation: return "Chi resta";
            case Ending.Bitter: return "Il conto";
            default: return "";
        }
    }

    public static string Body(Ending e, GameState s)
    {
        string t;
        switch (e)
        {
            case Ending.Loss:
                t = "Una mattina la sedia di tua figlia è vuota. Il letto è fatto, la finestra chiusa.\n"
                  + "Per giorni hai contato merce e lire, e i segnali erano tutti là: le carte nello zaino, il marchio sempre più pallido.\n"
                  + "In paese dicono che è stata chiamata al cielo. Tu continui ad aprire il negozio, perché non sai fare altro.";
                break;
            case Ending.SavedAlone:
                t = "Tua figlia dorme nel suo letto, al sicuro. Fuori, le case si svuotano una dopo l'altra.\n"
                  + "Hai salvato quello che potevi portare in braccio. Agli altri non hai detto niente, o non hai fatto abbastanza.\n"
                  + "Il paese è silenzioso, e il silenzio pesa più di qualsiasi colpa.";
                break;
            case Ending.SharedSalvation:
                t = "Il negozio non è più quello di prima: meno plastica, più pane e verdura.\n"
                  + "Con l'aiuto del forestiero, qualcuno ha ascoltato. Non tutti. Ma in piazza, la domenica, c'è ancora gente.\n"
                  + "Tua figlia ride. Tua moglie non torna, ma almeno non è successo di nuovo.";
                break;
            case Ending.Bitter:
                t = "Ora sai tutto. La scritta è chiara, e nessuno ti crede: hanno smesso di comprare da te.\n"
                  + "Il negozio è quasi vuoto, i risparmi sono finiti. Tua figlia è salva, ma il paese ti volta le spalle.\n"
                  + "Avevi ragione, e non è servito a niente.";
                break;
            default: return "";
        }
        if (Decoded(s)) t += "\n\nSulla fronte degli altri, finalmente leggibile: «non commestibile».";
        return t;
    }
}
