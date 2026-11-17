using System;
using System.Collections.Generic;

/// <summary>Catalogo dei prodotti del camion. Dati statici.</summary>
public static class Catalog
{
    public static readonly List<Product> All = new List<Product>()
    {
        // Naturali freschi (margine 15-30%)
        new Product("pane_forno", "Pane del forno", FoodKind.Natural, cost: 1000, basePrice: 1300, shelfLifeDays: 1),
        new Product("uova_paese", "Uova del paese", FoodKind.Natural, cost: 3000, basePrice: 4000, shelfLifeDays: 3),
        new Product("mortadella", "Mortadella", FoodKind.Natural, cost: 15000, basePrice: 18000, shelfLifeDays: 2),
        new Product("prosciutto", "Prosciutto nostrano", FoodKind.Natural, cost: 20000, basePrice: 24000, shelfLifeDays: 3),
        new Product("formaggio_malga", "Formaggio di malga", FoodKind.Natural, cost: 12000, basePrice: 15000, shelfLifeDays: 4),
        new Product("castagne", "Castagne", FoodKind.Natural, cost: 7000, basePrice: 9000, shelfLifeDays: 5),
        new Product("olio_oliva", "Olio d'oliva", FoodKind.Natural, cost: 12000, basePrice: 15000, shelfLifeDays: 180),
        new Product("passata", "Passata di pomodoro", FoodKind.Natural, cost: 1500, basePrice: 2000, shelfLifeDays: 90),
        new Product("farina", "Farina di frumento", FoodKind.Natural, cost: 1500, basePrice: 2000, shelfLifeDays: 120),
        new Product("latte", "Latte fresco", FoodKind.Natural, cost: 1000, basePrice: 1300, shelfLifeDays: 2),
        new Product("vino_rosso", "Vino rosso", FoodKind.Alcohol, cost: 4000, basePrice: 5000, shelfLifeDays: 365),
        new Product("vino_sfuso", "Vino sfuso", FoodKind.Alcohol, cost: 3000, basePrice: 4000, shelfLifeDays: 365),

        // Industriali (margine 60-120%)
        new Product("patatine", "Patatine in busta", FoodKind.Industrial, cost: 1500, basePrice: 3500, shelfLifeDays: 90),
        new Product("brodo_dadi", "Brodo in dadi", FoodKind.Industrial, cost: 800, basePrice: 2000, shelfLifeDays: 180),
        new Product("cracker", "Cracker", FoodKind.Industrial, cost: 1200, basePrice: 3000, shelfLifeDays: 150),
        new Product("zucchero", "Zucchero", FoodKind.Industrial, cost: 1200, basePrice: 2500, shelfLifeDays: 365),
        new Product("lievito", "Lievito di birra", FoodKind.Industrial, cost: 600, basePrice: 1500, shelfLifeDays: 180),
        new Product("birra", "Birra", FoodKind.Alcohol, cost: 1500, basePrice: 3500, shelfLifeDays: 180),
        new Product("amaro", "Amaro", FoodKind.Alcohol, cost: 10000, basePrice: 16000, shelfLifeDays: 365),

        // Dolci e snack (Industrial, Sweet=true)
        new Product("merendine", "Merendine al cioccolato", FoodKind.Industrial, cost: 1800, basePrice: 3500, shelfLifeDays: 150, sweet: true),
        new Product("biscotti", "Biscotti", FoodKind.Industrial, cost: 1500, basePrice: 3000, shelfLifeDays: 180, sweet: true),
        new Product("caramelle", "Caramelle", FoodKind.Industrial, cost: 2000, basePrice: 4000, shelfLifeDays: 365, sweet: true),
        new Product("cacao", "Cacao in polvere", FoodKind.Industrial, cost: 3000, basePrice: 6000, shelfLifeDays: 365, sweet: true),

        // Tabacco
        new Product("sigarette", "Sigarette", FoodKind.Tobacco, cost: 3500, basePrice: 5000, shelfLifeDays: 365),
        new Product("sigari", "Sigari", FoodKind.Tobacco, cost: 5000, basePrice: 8000, shelfLifeDays: 365),
    };

    public static Product Get(string id) => All.Find(p => p.Id == id);
}
