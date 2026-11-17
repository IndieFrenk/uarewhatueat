using System;
using System.Collections.Generic;

static class RulesTests
{
    static void Check(bool cond, string msg)
    {
        if (!cond) throw new Exception($"Test fallito: {msg}");
    }

    public static void Run()
    {
        // Test: Spend non va sotto zero
        {
            var econ = new Economy(100);
            int speso = econ.Spend(50);
            Check(speso == 50, "Spend: dovrebbe spendere 50");
            Check(econ.Savings == 50, "Spend: Savings dovrebbe essere 50");

            speso = econ.Spend(100);
            Check(speso == 50, "Spend: dovrebbe spendere solo 50 rimasti");
            Check(econ.Savings == 0, "Spend: Savings dovrebbe essere 0");
            Check(econ.Savings >= 0, "Spend: Savings non deve andare sotto zero");
        }

        // Test: Earn funziona e ignora negativi
        {
            var econ = new Economy(100);
            econ.Earn(50);
            Check(econ.Savings == 150, "Earn: dovrebbe aggiungere 50");
            Check(econ.EarnedToday == 50, "Earn: EarnedToday dovrebbe essere 50");

            econ.EarnedToday = 0;
            econ.Earn(-10);
            Check(econ.Savings == 150, "Earn: negativi dovrebbero essere ignorati");
            Check(econ.EarnedToday == 0, "Earn: EarnedToday dovrebbe rimanere 0");
        }

        // Test: SpentToday accumula
        {
            var econ = new Economy(1000);
            econ.Spend(100);
            econ.Spend(50);
            Check(econ.SpentToday == 150, "Spend: SpentToday dovrebbe accumulare");
        }

        // Test: Mark clamp 0..1
        {
            var v = new Villager { Mark = 0.5f, Drinker = false, Smoker = false };
            v.EatenToday.Add(FoodKind.Natural);
            MarkRules.ApplyDay(v);
            Check(v.Mark >= 0f && v.Mark <= 1f, "ApplyDay: Mark deve stare in 0..1");
            Check(v.Mark > 0.5f, "ApplyDay: Natural dovrebbe aumentare il marchio");
        }

        // Test: Cibo naturale alza il marchio
        {
            var v = new Villager { Mark = 0.3f, Drinker = false, Smoker = false };
            v.EatenToday.Add(FoodKind.Natural);
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.3f + MarkRules.NaturalGain, "ApplyDay: Natural +0.08");
            Check(v.EatenToday.Count == 0, "ApplyDay: EatenToday deve essere svuotato");
        }

        // Test: Industriale abbassa il marchio
        {
            var v = new Villager { Mark = 0.5f, Drinker = false, Smoker = false };
            v.EatenToday.Add(FoodKind.Industrial);
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.5f + MarkRules.IndustrialLoss, "ApplyDay: Industrial -0.07");
        }

        // Test: Alcol abbassa il marchio
        {
            var v = new Villager { Mark = 0.5f, Drinker = false, Smoker = false };
            v.EatenToday.Add(FoodKind.Alcohol);
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.5f + MarkRules.AlcoholLoss, "ApplyDay: Alcohol -0.09");
        }

        // Test: Fumo abbassa il marchio
        {
            var v = new Villager { Mark = 0.5f, Drinker = false, Smoker = false };
            v.EatenToday.Add(FoodKind.Tobacco);
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.5f + MarkRules.TobaccoLoss, "ApplyDay: Tobacco -0.09");
        }

        // Test: Drinker abituale abbassa il marchio
        {
            var v = new Villager { Mark = 0.5f, Drinker = true, Smoker = false };
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.5f + MarkRules.DrinkerDaily, "ApplyDay: Drinker -0.04");
        }

        // Test: Smoker abituale abbassa il marchio
        {
            var v = new Villager { Mark = 0.5f, Drinker = false, Smoker = true };
            MarkRules.ApplyDay(v);
            Check(v.Mark == 0.5f + MarkRules.SmokerDaily, "ApplyDay: Smoker -0.04");
        }

        // Test: Mark pieno (>= VisibleThreshold) → prob <= 0.02
        {
            var v = new Villager { Mark = 1f, Taken = false };
            float prob = MarkRules.TakeProbability(v, 7);
            Check(prob <= 0.02f, "TakeProbability: Mark pieno deve limitare a max 0.02");
        }

        // Test: Mark 0 al giorno 7 → probabilità alta
        {
            var v = new Villager { Mark = 0f, Taken = false };
            float prob = MarkRules.TakeProbability(v, 7);
            Check(prob > 0.3f, "TakeProbability: Mark 0, giorno 7 → probabilità alta (~0.4)");
        }

        // Test: Taken → probabilità 0
        {
            var v = new Villager { Mark = 0f, Taken = true };
            float prob = MarkRules.TakeProbability(v, 7);
            Check(prob == 0f, "TakeProbability: Taken deve dare 0");
        }

        // Test: ResolveNight con seed fisso
        {
            var rng = new Random(42);
            var villagers = new List<Villager>();
            for (int i = 0; i < 1000; i++)
            {
                villagers.Add(new Villager { Id = i.ToString(), Mark = 0f, Taken = false });
            }

            var taken = MarkRules.ResolveNight(villagers, 7, rng);

            // Circa il 40% di 1000 → ~400, tolleranza larga: 300-500
            int percentTaken = taken.Count * 100 / 1000;
            Check(percentTaken >= 25 && percentTaken <= 55,
                  $"ResolveNight: giorno 7, 1000 paesani mark=0 → aspetto ~40%, ho {percentTaken}%");
        }

        Console.WriteLine("RulesTests: 14 test passed.");
    }
}
