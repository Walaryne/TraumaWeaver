using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Barotrauma;
using HarmonyLib;


[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "CheckNamespace")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class StartupHook
{
    public static void Initialize()
    {
        if (Assembly.GetEntryAssembly()?.GetName().Name != "DedicatedServer") return;
        
        AssemblyLoadContext.Default.Resolving += SharedHostPolicy.SharedAssemblyResolver.LoadAssemblyFromSharedLocation;
        TraumaWeaver.Utils.AssemblyLoadPathHelper("0Harmony.dll");
        TraumaWeaver.Utils.AssemblyLoadPathHelper("Mono.Cecil.dll");
        TraumaWeaver.Utils.AssemblyLoadPathHelper("MonoMod.RuntimeDetour.dll");
        TraumaWeaver.Utils.AssemblyLoadPathHelper("MonoMod.Utils.dll");
        TraumaWeaver.MainPatcher.doHarmony();
    }
}

namespace TraumaWeaver
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
    public static class Utils
    {
        public static Assembly AssemblyLoadPathHelper(string assemblyName)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/" + assemblyName);
        }
    }
    
    public class Hooks
    {
        public static void onMain()
        {
            Console.WriteLine("TraumaWeaver " + Assembly.GetExecutingAssembly().GetName().Version + " Loaded");
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

namespace SharedHostPolicy
{
    internal static class SharedAssemblyResolver
    {
        public static Assembly LoadAssemblyFromSharedLocation(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string sharedAssemblyPath = Directory.GetCurrentDirectory(); // find assemblyName in shared location...
            return sharedAssemblyPath != null ? AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedAssemblyPath) : null;
        }
    }
}