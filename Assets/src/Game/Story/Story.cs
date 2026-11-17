using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pezzi della scritta, indizi e quaderno (tasto J). Logica comune a bar, TV e Vittorio.
/// Gli altri sistemi possono chiamare Story.AddFragment("disegno_figlia_1") per dare un pezzo.
/// </summary>
public static class Story
{
    const int PerPage = 3;
    static bool notebookOpen;

    /// <summary>Atto 1: giorni 1-2, atto 2: 3-5, atto 3: 6-7.</summary>
    public static int Act(int day) => day <= 2 ? 1 : day <= 5 ? 2 : 3;

    /// <summary>Un pezzo della scritta per fonte (una volta sola per fonte). Ritorna true se era nuovo.</summary>
    public static bool AddFragment(string fonte)
    {
        var s = GameState.Current;
        if (s == null || !s.Clues.Add("frag_" + fonte)) return false;
        s.SymbolFragments++;
        if (s.SymbolFragments == GameState.FragmentsToDecode)
        {
            s.Clues.Add("scritta_decifrata");
            s.Say("Hai decifrato la scritta: «non commestibile».");
        }
        else s.Say("Hai trovato un altro pezzo della scritta.");
        return true;
    }

    /// <summary>La scritta come la si legge adesso: simboli, poi «non…», poi la frase intera.</summary>
    public static string Inscription()
    {
        int f = GameState.Current?.SymbolFragments ?? 0;
        if (f >= GameState.FragmentsToDecode) return "«NON COMMESTIBILE»";
        if (f >= 2) return "«non ¤§¦¶ø¢¤§¦¶ø»";
        return "¤§¶  ¦ø¢  ¤§¦¶ø¢¤";
    }

    /// <summary>Frase leggibile per un indizio, o null se non va mostrato.</summary>
    static string Note(string id)
    {
        if (id.StartsWith("frag_") || id == "scritta_decifrata") return null;
        if (id.StartsWith("tv_day") && int.TryParse(id.Substring(6), out int d)) return StoryTexts.TgNote(d);
        if (id.StartsWith("bar_"))
        {
            var p = id.Split('_'); // bar, paesano, a1
            return p.Length == 3 && int.TryParse(p[2].Substring(1), out int a) ? StoryTexts.ChatNote(p[1], a) : null;
        }
        if (id.StartsWith("consp_")) return StoryTexts.ConspNote(id);
        return id.Contains(" ") ? id : null; // gli altri sistemi possono aggiungere indizi già in frase
    }

    static string SortKey(string id) =>
        id.StartsWith("tv_") ? "1" + id : id.StartsWith("bar_") ? "2" + id[id.Length - 1] + id : id.StartsWith("consp_") ? "3" + id : "4" + id;

    static List<string> Notes(GameState s) =>
        s.Clues.OrderBy(SortKey).Select(Note).Where(n => n != null).ToList();

    static string Missing(GameState s)
    {
        var gone = s.Villagers.Where(v => v.Taken).OrderBy(v => v.TakenOnDay).ToList();
        return gone.Count == 0
            ? "Per ora in paese ci sono ancora tutti."
            : "Mancano all'appello: " + string.Join(", ", gone.Select(v => $"{v.Name} (giorno {v.TakenOnDay})")) + ".";
    }

    // ---------------------------------------------------------------- quaderno

    /// <summary>Tasto J: apre o chiude il quaderno.</summary>
    public static void ToggleNotebook()
    {
        if (notebookOpen && UIKit.MenuOpen) { notebookOpen = false; UIKit.CloseMenu(); return; }
        notebookOpen = false;
        if (!UIKit.MenuOpen) OpenNotebook(0);
    }

    public static void OpenNotebook(int page)
    {
        var s = GameState.Current;
        if (s == null) return;
        var notes = Notes(s);
        int pages = 1 + (notes.Count + PerPage - 1) / PerPage;
        page = Mathf.Clamp(page, 0, pages - 1);

        string body = page == 0
            ? $"La scritta sulle fronti:\n\n      {Inscription()}\n\nPezzi raccolti: {s.SymbolFragments}/{GameState.FragmentsToDecode}\n\n{Missing(s)}\n\n"
              + (notes.Count == 0 ? "Per ora non c'è scritto altro." : $"Appunti: {notes.Count}.")
            : string.Join("\n\n", notes.Skip((page - 1) * PerPage).Take(PerPage).Select(n => "- " + n));

        var labels = new List<string>();
        var targets = new List<int>();
        if (page < pages - 1) { labels.Add(page == 0 ? "Sfoglia gli appunti" : "Pagina successiva"); targets.Add(page + 1); }
        if (page > 0) { labels.Add("Pagina precedente"); targets.Add(page - 1); }
        labels.Add("Chiudi il quaderno"); targets.Add(-1);

        notebookOpen = true;
        UIKit.Choices($"Quaderno ({page + 1}/{pages})", body, labels.ToArray(), i =>
        {
            notebookOpen = false;
            if (targets[i] >= 0) OpenNotebook(targets[i]);
        });
    }
}
