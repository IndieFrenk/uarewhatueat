using System;
using System.Collections.Generic;

/// <summary>I paesani di partenza, il protagonista e la figlia.</summary>
public static class Roster
{
    /// <summary>Giorno in cui Vittorio compare al bar per la prima volta.</summary>
    public const int ConspAppearDay = 3;

    /// <summary>Riempie state.Protagonist, state.Daughter e state.Villagers.</summary>
    public static void Populate(GameState state)
    {
        // Protagonista: bevitore, beve alcol e mangia industriale
        state.Protagonist = new Villager
        {
            Id = "protagonista",
            Name = "Tu",
            Role = "bottegaio",
            Mark = 0f,
            Drinker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.2f },
                { FoodKind.Industrial, 0.5f },
                { FoodKind.Alcohol, 0.9f },
                { FoodKind.Tobacco, 0.1f }
            }
        };

        // Figlia: mangia dolci (Industrial), ma naturale base, Mark ancora alto
        state.Daughter = new Villager
        {
            Id = "figlia",
            Name = "Giulia",
            Role = "figlia",
            Mark = 0.6f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.7f },
                { FoodKind.Industrial, 0.6f },
                { FoodKind.Alcohol, 0.0f },
                { FoodKind.Tobacco, 0.0f }
            }
        };

        // Paesani
        state.Villagers.Add(new Villager
        {
            Id = "parroco",
            Name = "Don Mario",
            Role = "parroco",
            Mark = 0.7f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.9f },
                { FoodKind.Industrial, 0.1f },
                { FoodKind.Alcohol, 0.3f },
                { FoodKind.Tobacco, 0.0f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "barista",
            Name = "Tonino",
            Role = "barista",
            Mark = 0.2f,
            Drinker = true,
            Smoker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.3f },
                { FoodKind.Industrial, 0.6f },
                { FoodKind.Alcohol, 0.9f },
                { FoodKind.Tobacco, 0.8f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "macellaio",
            Name = "Rocco",
            Role = "macellaio",
            Mark = 0.8f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.9f },
                { FoodKind.Industrial, 0.2f },
                { FoodKind.Alcohol, 0.4f },
                { FoodKind.Tobacco, 0.1f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "fioraia",
            Name = "Assunta",
            Role = "fioraia",
            Mark = 0.9f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 1.0f },
                { FoodKind.Industrial, 0.0f },
                { FoodKind.Alcohol, 0.1f },
                { FoodKind.Tobacco, 0.0f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "oste",
            Name = "Peppe",
            Role = "oste",
            Mark = 0.5f,
            Drinker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.7f },
                { FoodKind.Industrial, 0.3f },
                { FoodKind.Alcohol, 0.8f },
                { FoodKind.Tobacco, 0.2f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "vecchia",
            Name = "Nonna Caterina",
            Role = "vecchia",
            Mark = 0.4f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.95f },
                { FoodKind.Industrial, 0.1f },
                { FoodKind.Alcohol, 0.2f },
                { FoodKind.Tobacco, 0.0f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "vittorio",
            Name = "Vittorio",
            Role = "cospirazionista",
            FirstTakeDay = ConspAppearDay + 1, // non prendibile finché non compare al bar (e la notte stessa)
            Mark = 0.3f,
            Smoker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.2f },
                { FoodKind.Industrial, 0.8f },
                { FoodKind.Alcohol, 0.6f },
                { FoodKind.Tobacco, 0.9f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "lucia",
            Name = "Lucia",
            Role = "maestra",
            Mark = 0.3f,
            Smoker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.5f },
                { FoodKind.Industrial, 0.5f },
                { FoodKind.Alcohol, 0.3f },
                { FoodKind.Tobacco, 0.7f }
            }
        });

        // Paesani minori
        state.Villagers.Add(new Villager
        {
            Id = "salvatore",
            Name = "Salvatore",
            Role = "paesano",
            Mark = 0.85f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.95f },
                { FoodKind.Industrial, 0.1f },
                { FoodKind.Alcohol, 0.2f },
                { FoodKind.Tobacco, 0.0f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "concetta",
            Name = "Concetta",
            Role = "paesano",
            Mark = 0.15f,
            Smoker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.2f },
                { FoodKind.Industrial, 0.9f },
                { FoodKind.Alcohol, 0.7f },
                { FoodKind.Tobacco, 0.6f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "mimmo",
            Name = "Mimmo",
            Role = "paesano",
            Mark = 0.5f,
            Drinker = true,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.4f },
                { FoodKind.Industrial, 0.6f },
                { FoodKind.Alcohol, 0.8f },
                { FoodKind.Tobacco, 0.3f }
            }
        });

        state.Villagers.Add(new Villager
        {
            Id = "rosa",
            Name = "Rosa",
            Role = "paesano",
            Mark = 0.7f,
            Habits = new Dictionary<FoodKind, float>
            {
                { FoodKind.Natural, 0.8f },
                { FoodKind.Industrial, 0.3f },
                { FoodKind.Alcohol, 0.2f },
                { FoodKind.Tobacco, 0.1f }
            }
        });
    }
}
