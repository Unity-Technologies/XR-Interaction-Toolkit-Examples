// Copyright (c) Meta Platforms, Inc. and affiliates.
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Editor tool for mass fixing hand grab settings
    /// </summary>
    public class FixHandGrabs
    {
        [MenuItem("Tools/NorthStar/Fix Hand grabs")]
        private static void OnMenuShow()
        {
            var rule = GlobalSettings.PlayerSettings.DefaultPalmGrabRule;
            var property = typeof(HandGrabInteractable).GetField("_palmGrabRules", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var obj in Object.FindObjectsByType<HandGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                property.SetValue(obj, rule);
                EditorUtility.SetDirty(obj);
            }
        }
    }
}