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

using MarkerPoint = OVRTelemetry.MarkerPoint;

internal static class OVRTelemetryConstants
{
    public static class OVRManager
    {
        public static class MarkerId
        {
            public const int Init = 163069401;
        }

        public static readonly MarkerPoint InitializeInsightPassthrough =
            new MarkerPoint("InitializeInsightPassthrough");

        public static readonly MarkerPoint InitPermissionRequest = new MarkerPoint("InitPermissionRequest");
    }

    public static class Scene
    {
        public static class MarkerId
        {
            public const int SpatialAnchorSetComponentStatus = 163055742;
            public const int SpatialAnchorSave = 163056007;
            public const int SpatialAnchorQuery = 163057870;
            public const int SpatialAnchorErase = 163059334;
            public const int SpatialAnchorCreate = 163068641;
        }
    }

    public static class Editor
    {
        public const int Start = 163067235;
    }
}
