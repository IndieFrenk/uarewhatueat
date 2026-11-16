using System;
using System.Collections.Generic;

/// <summary>Catalogo dei prodotti del camion. Dati statici.</summary>
public static class Catalog
{
    public static readonly List<Product> All = new List<Product>();
    public static Product Get(string id) => All.Find(p => p.Id == id);
}
