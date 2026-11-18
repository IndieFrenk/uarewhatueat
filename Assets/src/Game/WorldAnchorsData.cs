using UnityEngine;

/// <summary>Copia serializzata di WorldAnchors sul "Mondo": col mondo cotto nella scena (uarewhatueat/Cuoci il mondo) li rimette a posto all'avvio.</summary>
public class WorldAnchorsData : MonoBehaviour
{
    public Transform player, shopEntrance, register, storeroomDrop, orderBook, homeSpawn, shopSpawn, barSpawn;
    public Transform[] queuePoints, shelfSpots, shelfSlots, barSeats;
    public Light[] shopLights;

    public void Save()
    {
        player = WorldAnchors.Player; shopEntrance = WorldAnchors.ShopEntrance; register = WorldAnchors.Register;
        storeroomDrop = WorldAnchors.StoreroomDrop; orderBook = WorldAnchors.OrderBook;
        homeSpawn = WorldAnchors.HomeSpawn; shopSpawn = WorldAnchors.ShopSpawn; barSpawn = WorldAnchors.BarSpawn;
        queuePoints = WorldAnchors.QueuePoints; shelfSpots = WorldAnchors.ShelfSpots; shelfSlots = WorldAnchors.ShelfSlots;
        barSeats = WorldAnchors.BarSeats; shopLights = WorldAnchors.ShopLights;
    }

    void Awake()
    {
        WorldAnchors.Player = player; WorldAnchors.ShopEntrance = shopEntrance; WorldAnchors.Register = register;
        WorldAnchors.StoreroomDrop = storeroomDrop; WorldAnchors.OrderBook = orderBook;
        WorldAnchors.HomeSpawn = homeSpawn; WorldAnchors.ShopSpawn = shopSpawn; WorldAnchors.BarSpawn = barSpawn;
        WorldAnchors.QueuePoints = queuePoints; WorldAnchors.ShelfSpots = shelfSpots; WorldAnchors.ShelfSlots = shelfSlots;
        WorldAnchors.BarSeats = barSeats; WorldAnchors.ShopLights = shopLights;
    }
}
