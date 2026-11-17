using System;

// Ogni controllo è un assert: se la logica si rompe il processo esce con errore.
static class Program
{
    static int Main()
    {
        EndingsTests.Run();
        Console.WriteLine("LogicTests: ok.");
        return 0;
    }
}
