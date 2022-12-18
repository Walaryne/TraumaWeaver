using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;


[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "CheckNamespace")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class StartupHook
{
    enum LaunchType
    {
        CLIENT,
        SERVER
    }
    
    public static void Initialize()
    {

        LaunchType launch;

        switch (Assembly.GetEntryAssembly()?.GetName().Name)
        {
            case "DedicatedServer":
                {
                    launch = LaunchType.SERVER;
                    break;
                }
            case "Barotrauma":
                {
                    launch = LaunchType.CLIENT;
                    break;
                }
            default:
                {
                    return;
                }
        }
        
        TraumaWeaverLoader.Utils.AssemblyLoadPathHelper("0Harmony.dll");
        TraumaWeaverLoader.Utils.AssemblyLoadPathHelper("Mono.Cecil.dll");
        TraumaWeaverLoader.Utils.AssemblyLoadPathHelper("MonoMod.RuntimeDetour.dll");
        TraumaWeaverLoader.Utils.AssemblyLoadPathHelper("MonoMod.Utils.dll");

        Assembly assembly = null;

        switch (launch)
        {
            case LaunchType.CLIENT:
                {
                    assembly = Assembly.LoadFile(Directory.GetCurrentDirectory() + "/TraumaWeaverClient.dll");
                    break;
                }
            case LaunchType.SERVER:
                {
                    assembly = Assembly.LoadFile(Directory.GetCurrentDirectory() + "/TraumaWeaverServer.dll");
                    break;
                }
        }


        if (assembly != null)
        {
            Array.Find(assembly.GetTypes(), e => e.Name == "MainPatcher")?.GetMethod("doHarmony")?.Invoke(null, null);
        }
    }
}

namespace TraumaWeaverLoader
{
    public static class Utils
    {
        public static Assembly AssemblyLoadPathHelper(string assemblyName)
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Directory.GetCurrentDirectory() + "/" + assemblyName);
        }
    }
}