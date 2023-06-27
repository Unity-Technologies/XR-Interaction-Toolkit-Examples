// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using VRBuilder.Editor;
using UnityEditor;
using VRBuilder.Editor.Settings;

/// <summary>
/// Checks for assemblies specified and adds/removes the symbol according to their existence.
/// </summary>
[InitializeOnLoad]
internal class AssemblySymbolChecker
{
    static AssemblySymbolChecker()
    {
        CheckForAssembly("VRBuilder.Core", "VR_BUILDER");
        CheckForAssembly("VRBuilder.XRInteraction", "VR_BUILDER_XR_INTERACTION");
        CheckForAssembly("VRBuilder.Animations", "VR_BUILDER_ANIMATIONS");
        CheckForAssembly("VRBuilder.StatesAndData", "VR_BUILDER_STATES_DATA");
        CheckForAssembly("Unity.Netcode.Components", "UNITY_NETCODE");

        if (InteractionComponentSettings.Instance.EnableXRInteractionComponent)
        {
            AddSymbol("VR_BUILDER_ENABLE_XR_INTERACTION");
        }
        else
        {
            RemoveSymbol("VR_BUILDER_XR_INTERACTION");
            RemoveSymbol("VR_BUILDER_ENABLE_XR_INTERACTION");
        }
    }

    /// <summary>
    /// Tries to find the given assembly name, and add/removes the symbol according to the existence of it.
    /// </summary>
    /// <param name="assemblyName">The assembly name looked for, just the name, no full name.</param>
    /// <param name="symbol">The symbol added/removed.</param>
    public static void CheckForAssembly(string assemblyName, string symbol)
    {
        if (EditorReflectionUtils.AssemblyExists(assemblyName))
        {
            AddSymbol(symbol);
        }
        else
        {
            RemoveSymbol(symbol);
        }
    }

    /// <summary>
    /// Tries to find the given assembly name, and add/removes the symbol according to the existence of it.
    /// </summary>
    /// <param name="assemblyName">The assembly name looked for, just the name, no full name.</param>
    /// <param name="className">The class name looked for.</param>
    /// <param name="symbol">The symbol added/removed.</param>
    public static void CheckForClass(string assemblyName, string className, string symbol)
    {
        if (EditorReflectionUtils.ClassExists(assemblyName, className))
        {
            AddSymbol(symbol);
        }
        else
        {
            RemoveSymbol(symbol);
        }
    }

    private static void AddSymbol(string symbol)
    {
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        List<string> symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList();

        if (symbols.Contains(symbol) == false)
        {
            symbols.Add(symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbols.ToArray()));
        }
    }

    private static void RemoveSymbol(string symbol)
    {
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        List<string> symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList();

        if (symbols.Contains(symbol))
        {
            symbols.Remove(symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbols.ToArray()));
        }
    }
}
