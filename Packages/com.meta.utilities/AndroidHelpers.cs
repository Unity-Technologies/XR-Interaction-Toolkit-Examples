// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public static class AndroidHelpers
    {
        private static AndroidJavaObject s_intent;

        static AndroidHelpers()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
        s_intent = activity.Call<AndroidJavaObject>("getIntent");
#endif
        }

        public static bool HasIntentExtra(string argumentName) => s_intent?.Call<bool>("hasExtra", argumentName) ?? false;

        public static string GetStringIntentExtra(string extraName) =>
            HasIntentExtra(extraName) ?
                s_intent.Call<string>("getStringExtra", extraName) :
                null;

        public static float? GetFloatIntentExtra(string extraName) =>
            HasIntentExtra(extraName) ?
                s_intent.Call<float>("getFloatExtra", extraName, 0.0f) :
                null;
    }
}
