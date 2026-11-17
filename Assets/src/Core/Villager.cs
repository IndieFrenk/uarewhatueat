using System.Collections.Generic;

/// <summary>Un paesano (anche protagonista e figlia). Logica pura, niente UnityEngine.</summary>
public class Villager
{
    public string Id;
    public string Name;
    public string Role;          // "parroco", "macellaio", ...
    public float Mark;           // 0 = nessun marchio (commestibile), 1 = marchio pieno
    public bool Taken;           // «chiamato al cielo»
    public int TakenOnDay;
    public bool Smoker;
    public bool Drinker;
    /// <summary>Prima notte (numero del giorno) in cui può essere preso. 0 = sempre.</summary>
    public int FirstTakeDay;
    public bool Derided;         // il protagonista l'ha avvertito ed è stato deriso
    public bool Warned;
    /// <summary>Quanto spesso compra/mangia ogni tipo (pesi relativi).</summary>
    public Dictionary<FoodKind, float> Habits = new Dictionary<FoodKind, float>();
    /// <summary>Cosa ha mangiato oggi. Svuotato da MarkRules.ApplyDay.</summary>
    public List<FoodKind> EatenToday = new List<FoodKind>();

    public bool HasVisibleMark => Mark >= MarkRules.VisibleThreshold;
}
