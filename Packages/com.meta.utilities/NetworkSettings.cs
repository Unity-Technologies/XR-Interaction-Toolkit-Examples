// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// This saves and retrieved editor settings for networking for faster testing in editor, without changing prefabs
    /// and scenes.
    /// </summary>
    public static class NetworkSettings
    {
        private static readonly string s_autostartKey = $"{Application.productName}.NetworkSettingsToolbar.Autostart";
        private static readonly string s_useDeviceRoomKey = $"{Application.productName}.NetworkSettingsToolbar.UseDeviceRoom";
        private static readonly string s_roomNameKey = $"{Application.productName}.NetworkSettingsToolbar.RoomName";

        public static bool Autostart
        {
            get => EditorPrefs.GetBool(s_autostartKey);
            set => EditorPrefs.SetBool(s_autostartKey, value);
        }
        public static bool UseDeviceRoom
        {
            get => EditorPrefs.GetBool(s_useDeviceRoomKey);
            set => EditorPrefs.SetBool(s_useDeviceRoomKey, value);
        }
        public static string RoomName
        {
            get => EditorPrefs.GetString(s_roomNameKey);
            set => EditorPrefs.SetString(s_roomNameKey, value);
        }
    }
}

#else

namespace Meta.Utilities
{
    public static class NetworkSettings
    {
        public static bool Autostart => throw new System.NotImplementedException();
        public static bool UseDeviceRoom => throw new System.NotImplementedException();
        public static string RoomName => throw new System.NotImplementedException();
    }
}

#endif
