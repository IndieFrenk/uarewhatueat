using System.Collections.Generic;

/// <summary>Asse del gioco: industriale contro naturale. Alcol e fumo contano come industriale.</summary>
public enum FoodKind { Natural, Industrial, Alcohol, Tobacco }

/// <summary>Un prodotto del catalogo. Prezzi in lire.</summary>
public class Product
{
    public string Id;
    public string Name;
    public FoodKind Kind;
    public bool Sweet;        // dolci: la figlia li mangia di nascosto
    public int Cost;          // prezzo d'acquisto dal camion
    public int BasePrice;     // prezzo di vendita suggerito
    public int ShelfLifeDays; // freschi pochi giorni, confezionati tanti

    public Product(string id, string name, FoodKind kind, int cost, int basePrice, int shelfLifeDays, bool sweet = false)
    {
        Id = id; Name = name; Kind = kind; Cost = cost; BasePrice = basePrice; ShelfLifeDays = shelfLifeDays; Sweet = sweet;
    }
}
