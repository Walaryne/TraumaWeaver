using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Barotrauma;
using HarmonyLib;

[assembly: IgnoresAccessChecksTo("DedicatedServer.dll")]
namespace TraumaWeaverServer
{

    public static class MainPatcher
    {
        public static void doHarmony()
        {
            var harmony = new Harmony("TraumaWeaverLoader");
        

            harmony.Patch(AccessTools.Method(typeof(Program), nameof(Program.Main)),
                          transpiler: new HarmonyMethod(typeof(Patches), nameof(Patches.MainTranspiler)));
        }
    }

    public class Hooks
    {
        public static void onMain()
        {
            Console.WriteLine("TraumaWeaverServer " + Assembly.GetExecutingAssembly().GetName().Version + " Loaded");
            Console.WriteLine("Loading mods...");
            DirectoryInfo serverDllMods = Directory.CreateDirectory("ServerDllMods");
            var mods = serverDllMods.EnumerateFiles();
            foreach (FileInfo mod in mods)
            {
                Console.WriteLine($"Loading {mod.Name}");
                try
                {
                    Assembly modAssembly = Assembly.LoadFrom(mod.FullName);
                    modAssembly.GetTypes().Find(e => e.Name == "Mod").GetMethod("ModMain")?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not load {mod.FullName}\nException:\n{e}");
                }
            }
        }
    }
    
    public class Patches
    {
        public static IEnumerable<CodeInstruction> MainTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(AccessTools.Method(typeof(AppDomain), "get_CurrentDomain")) && !found)
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(Hooks.onMain)));
                    found = true;
                }
                yield return instruction;
            }
            if (!found)
            {
                Console.WriteLine("Cannot find Program.Main");
            }
        }
    }
}
