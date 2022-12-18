using System;
using System.Diagnostics;
using System.IO;

namespace TraumaWeaverLauncher
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ProcessStartInfo pProcess = new ProcessStartInfo
            {
                EnvironmentVariables =
                {
                    ["DOTNET_STARTUP_HOOKS"] = $"{Directory.GetCurrentDirectory()}/TraumaWeaverLoader.dll"
                },
                UseShellExecute = false,
                CreateNoWindow = false,
                FileName = "dotnet",
                ArgumentList = { "Barotrauma.dll" }
            };

            Process.Start(pProcess);

            Console.WriteLine("Press ESC to exit...");
            while (true)
            {
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Escape)
                    break;
 
                Console.WriteLine("{0} --- ", k.Key);
            }
        }
    }
}
