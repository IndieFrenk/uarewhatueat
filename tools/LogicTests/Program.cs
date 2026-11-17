using System;

// Ogni controllo è un assert: se la logica si rompe il processo esce con errore.
static class Program
{
    static int Main()
    {
        try
        {
            RulesTests.Run();
            DataTests.Run();
            EndingsTests.Run();
            PriceTests.Run();
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERRORE: {e.Message}");
            return 1;
        }
    }
}
