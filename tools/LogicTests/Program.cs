using System;

// Ogni controllo è un assert: se la logica si rompe il processo esce con errore.
static class Program
{
    static int Main()
    {
        try
        {
            DataTests.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Test failed: {ex.Message}");
            return 1;
        }
    }
}
