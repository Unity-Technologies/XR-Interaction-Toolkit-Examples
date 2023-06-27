/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal abstract class OVRProjectSetupSettingItem<T>
{
    public T Default { get; }
    public string Uid { get; }
    public string Label { get; }
    public string Key { get; }
    public abstract T Value { get; set; }

    protected OVRProjectSetupSettingItem(string uid, T defaultValue, string label = null)
    {
        Default = defaultValue;
        Uid = uid;
        Label = label ?? uid;
        Key = OVRProjectSetup.KeyPrefix + "." + Uid;
    }

    public abstract void AppendToMenu(GenericMenu menu);

    public void Reset()
    {
        Value = Default;
    }

    public void OnSet()
    {
        OVRTelemetry.Start(OVRProjectSetupTelemetryEvent.EventTypes.Option)
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Uid, Uid)
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Value, Value.ToString())
            .Send();
    }
}

internal abstract class OVRProjectSetupSettingBool : OVRProjectSetupSettingItem<bool>
{
    protected OVRProjectSetupSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override void AppendToMenu(GenericMenu menu)
    {
        menu.AddItem(new GUIContent(Label), Value, () => Value = !Value);
    }
}

internal abstract class OVRProjectSetupSettingFloat : OVRProjectSetupSettingItem<float>
{
    protected OVRProjectSetupSettingFloat(string uid, float defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override void AppendToMenu(GenericMenu menu)
    {
        // Do not Append to a GenericMenu
    }
}

internal class OVRProjectSetupOnlyOnceSettingBool : OVRProjectSetupSettingBool
{
    private static readonly HashSet<string> OnlyOnceSettings = new HashSet<string>();

    public OVRProjectSetupOnlyOnceSettingBool(string uid) : base(uid, true)
    {
        Init();
    }

    private void Init()
    {
        previousTimestamp = new OVRProjectSetupInternalUserSettingFloat($"{Uid}_timestamp", 0.0f);
        previousTimeSinceStartup = new OVRProjectSetupInternalUserSettingFloat($"{Uid}_timesincestartup", 0.0f);
    }

    private OVRProjectSetupSettingFloat previousTimestamp;
    private OVRProjectSetupSettingFloat previousTimeSinceStartup;

    public override bool Value
    {
        get => OnlyOnce();
        set
        {
            if (value == Default)
            {
                // If back to Default, we remove it from the dictionary to avoid clutter
                OnlyOnceSettings.Remove(Uid);
                previousTimestamp.Reset();
                previousTimeSinceStartup.Reset();
            }
        }
    }

    private bool OnlyOnce()
    {
        if (OnlyOnceSettings.Contains(Uid))
        {
            // If the tuple was found, this means we already went through this test
            // So either it was the first time the previous time
            // or it wasn't the first time...so neither is current time
            return false;
        }

        // From now on, we can be sure next time won't be the first
        // So we add it to the HashSet
        OnlyOnceSettings.Add(Uid);

        var currentTimestamp = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
        // Better timestamp would be absolute number of seconds
        // But this is too big to be stored in float or int, and EditorPrefs do not allow double or long.
        var currentTimeSinceStartup = (float)EditorApplication.timeSinceStartup;

        // The tuple was not found, so this is the first time we're testing it
        // since last compile
        // We're getting the previous saved timestamps
        var timeStampDiff = currentTimestamp - previousTimestamp.Value;
        var timeSinceStartupDiff = currentTimeSinceStartup - previousTimeSinceStartup.Value;

        // If the difference between timestamps is very close
        // to the difference between timeSinceStartups
        // Then we're very probably on the same instance of the Editor
        const float closeThreshold = 5.0f;
        if (Mathf.Abs(timeSinceStartupDiff - timeStampDiff) < closeThreshold)
        {
            return false;
        }

        // And we're storing the current timestamp for next time
        previousTimestamp.Value = currentTimestamp;
        previousTimeSinceStartup.Value = currentTimeSinceStartup;
        return true;
    }
}

internal class OVRProjectSetupInternalUserSettingFloat : OVRProjectSetupSettingFloat
{
    public OVRProjectSetupInternalUserSettingFloat(string uid, float defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override float Value
    {
        get => EditorPrefs.GetFloat(Key, Default);
        set => EditorPrefs.SetFloat(Key, value);
    }
}

internal class OVRProjectSetupInternalUserSettingBool : OVRProjectSetupSettingBool
{
    public OVRProjectSetupInternalUserSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override bool Value
    {
        get => EditorPrefs.GetBool(Key, Default);
        set => EditorPrefs.SetBool(Key, value);
    }
}

internal class OVRProjectSetupUserSettingFloat : OVRProjectSetupInternalUserSettingFloat
{
    public OVRProjectSetupUserSettingFloat(string uid, float defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override float Value
    {
        set
        {
            base.Value = value;
            OnSet();
        }
    }
}

internal class OVRProjectSetupUserSettingBool : OVRProjectSetupInternalUserSettingBool
{
    public OVRProjectSetupUserSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override bool Value
    {
        set
        {
            base.Value = value;
            OnSet();
        }
    }
}

internal class OVRProjectSettingBool : OVRProjectSetupSettingBool
{
    public OVRProjectSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override bool Value
    {
        get => OVRProjectSetupSettings.GetProjectConfig(create: false)?.GetProjectSetupBool(Key, Default) ?? Default;
        set
        {
            OVRProjectSetupSettings.GetProjectConfig()?.SetProjectSetupBool(Key, value);
            OnSet();
        }
    }

    public bool HasRecordedValue => OVRProjectSetupSettings.GetProjectConfig(create: false)?.HasBool(Key) ?? false;
}

internal class OVRProjectSetupProjectSettingBool : OVRProjectSetupSettingBool
{
    public OVRProjectSetupProjectSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override bool Value
    {
        get => OVRProjectSetupSettings.GetProjectConfig(create: false)?.GetProjectSetupBool(Key, Default) ?? Default;
        set
        {
            if (value == Default)
            {
                // If back to Default, we remove it from the dictionary to avoid clutter
                OVRProjectSetupSettings.GetProjectConfig()?.RemoveProjectSetupBool(Key);
            }
            else
            {
                OVRProjectSetupSettings.GetProjectConfig()?.SetProjectSetupBool(Key, value);
            }

            OnSet();
        }
    }
}

internal class OVRProjectSetupConstSettingBool : OVRProjectSetupSettingBool
{
    public OVRProjectSetupConstSettingBool(string uid, bool defaultValue, string label = null)
        : base(uid, defaultValue, label)
    {
    }

    public override bool Value
    {
        get => Default;
        set { }
    }

    public override void AppendToMenu(GenericMenu menu)
    {
    }
}
