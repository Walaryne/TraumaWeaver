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
                CreateNoWindow = true,
                FileName = "dotnet",
                ArgumentList = { "Barotrauma.dll" }
            };

            Process.Start(pProcess);

        }
    }
}
