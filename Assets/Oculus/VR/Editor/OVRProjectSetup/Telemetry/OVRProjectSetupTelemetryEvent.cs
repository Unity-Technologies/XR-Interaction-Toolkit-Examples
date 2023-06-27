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

using static OVRTelemetry;

internal static class OVRProjectSetupTelemetryEvent
{
    public static class EventTypes
    {
        // Attention : Need to be kept in sync with QPL Event Ids
        public const int Fix = 163058027;
        public const int Option = 163058846;
        public const int GoToSource = 163056520;
        public const int Summary = 163063879;
        public const int Open = 163056010;
        public const int Close = 163056958;
        public const int InteractionFlow = 163069594;
    }

    public static class AnnotationTypes
    {
        public const string Uid = "Uid";
        public const string Level = "Level";
        public const string Type = "Type";
        public const string Value = "Value";
        public const string BuildTargetGroup = "BuildTargetGroup";
        public const string Group = "Group";
        public const string Blocking = "Blocking";
        public const string Count = "Count";
        public const string Origin = "Origin";
        public const string TimeSpent = "TimeSpent";
        public const string Interaction = "Interaction";
        public const string ValueAfter = "ValueAfter";
        public const string BuildTargetGroupAfter = "BuildTargetGroupAfter";
    }

    public static class MarkerPoints
    {
        public static readonly MarkerPoint Process = new MarkerPoint("Process");
        public static readonly MarkerPoint Open = new MarkerPoint("Open");
        public static readonly MarkerPoint Interact = new MarkerPoint("Interact");
        public static readonly MarkerPoint Close = new MarkerPoint("Close");
    }
}
