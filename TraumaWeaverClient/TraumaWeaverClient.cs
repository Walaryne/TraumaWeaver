using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
namespace TraumaWeaverClient
{

    public static class MainPatcher
    {
        public static void doHarmony()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveHandler;
            var harmony = new Harmony("TraumaWeaverClient");
        

            harmony.Patch(AccessTools.Method(typeof(Program), nameof(Program.Main)),
                          transpiler: new HarmonyMethod(typeof(Patches), nameof(Patches.MainTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(MainMenuScreen), nameof(MainMenuScreen.Draw)),
                          transpiler: new HarmonyMethod(typeof(Patches), nameof(Patches.MainMenuScreenTranspiler)));
        }

        private static Assembly ResolveHandler(object o, ResolveEventArgs args)
        {
            if (args.Name == "MonoGame.Framework.Linux.NetStandard, Version=3.7.0.0, Culture=neutral, PublicKeyToken=null")
            {
                return Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                                  assembly => assembly.GetName().FullName == "MonoGame.Framework.Windows.NetStandard, Version=3.7.0.0, Culture=neutral, PublicKeyToken=null");
            }
            return null;
        }
    }

    public class Hooks
    {
        public static void onMain()
        {
            Console.WriteLine("TraumaWeaverClient " + Assembly.GetExecutingAssembly().GetName().Version + " Loaded");
            Console.WriteLine("Loading mods...");
            DirectoryInfo clientDllMods = Directory.CreateDirectory("ClientDllMods");
            var mods = clientDllMods.EnumerateFiles();
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
        
        public static void onMainMenuScreenDraw(double deltaTime, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            String versionString = $"TraumaWeaverClient {Assembly.GetExecutingAssembly().GetName().Version} Loaded";
            GUIStyle.SmallFont.DrawString(spriteBatch, versionString, new Vector2(HUDLayoutSettings.Padding, GameMain.GraphicsHeight - GUIStyle.SmallFont.LineHeight * 3f - HUDLayoutSettings.Padding * 0.75f), Color.Teal);
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

        public static IEnumerable<CodeInstruction> MainMenuScreenTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.LoadsField(typeof(GameMain).GetField("Version")))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Hooks).GetMethod(nameof(Hooks.onMainMenuScreenDraw)));
                    found = true;
                }
                yield return instruction;
            }
            if (!found)
            {
                Console.WriteLine("Cannot find String.Concat");
            }
        }
    }
}
