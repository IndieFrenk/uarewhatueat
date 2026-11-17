using UnityEngine;

/// <summary>
/// Punti del mondo riempiti da WorldBuilder e usati dagli altri sistemi (clienti, consegne, scene).
/// </summary>
public static class WorldAnchors
{
    public static Transform Player;
    public static Transform ShopEntrance;      // dove entrano/escono i clienti
    public static Transform Register;          // dove sta il cliente a pagare
    public static Transform[] QueuePoints;     // fila alla cassa, [0] = primo
    public static Transform[] ShelfSpots;      // punti davanti agli scaffali per i clienti
    public static Transform[] ShelfSlots;      // dove spawnare i prodotti sugli scaffali
    public static Transform StoreroomDrop;     // dove il camion lascia la merce (sul bancale)
    public static Transform OrderBook;         // registro degli ordini sulla scrivania del magazzino
    public static Transform HomeSpawn, ShopSpawn, BarSpawn;
    public static Transform[] BarSeats;
    public static Light[] ShopLights;
}
