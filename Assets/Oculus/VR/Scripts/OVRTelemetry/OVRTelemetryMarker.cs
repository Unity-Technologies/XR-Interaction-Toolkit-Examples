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
using static OVRTelemetry;

internal struct OVRTelemetryMarker : IDisposable
{
    internal struct OVRTelemetryMarkerState
    {
        public bool Sent { get; set; }
        public OVRPlugin.Qpl.ResultType Result { get; set; }

        public OVRTelemetryMarkerState(bool sent, OVRPlugin.Qpl.ResultType result)
        {
            Result = result;
            Sent = sent;
        }
    }


    private OVRTelemetryMarkerState State { get; set; }

    public bool Sent => State.Sent;
    public OVRPlugin.Qpl.ResultType Result => State.Result;

    public int MarkerId { get; }
    public int InstanceKey { get; }

    private readonly TelemetryClient _client;

    public OVRTelemetryMarker(
        int markerId,
        int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
        long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
        : this(
            OVRTelemetry.Client,
            markerId,
            instanceKey,
            timestampMs)
    {
    }

    internal OVRTelemetryMarker(
        TelemetryClient client,
        int markerId,
        int instanceKey = OVRPlugin.Qpl.DefaultInstanceKey,
        long timestampMs = OVRPlugin.Qpl.AutoSetTimestampMs)
    {
        MarkerId = markerId;
        InstanceKey = instanceKey;
        _client = OVRTelemetry.Client;
        State = new OVRTelemetryMarkerState(false, OVRPlugin.Qpl.ResultType.Success);

        _client.MarkerStart(markerId, instanceKey, timestampMs);
    }

    public OVRTelemetryMarker SetResult(OVRPlugin.Qpl.ResultType result)
    {
        State = new OVRTelemetryMarkerState(Sent, result);
        return this;
    }

    public OVRTelemetryMarker AddAnnotation(string annotationKey, string annotationValue)
    {
        _client.MarkerAnnotation(MarkerId, annotationKey, annotationValue, InstanceKey);
        return this;
    }

    public OVRTelemetryMarker Send()
    {
        State = new OVRTelemetryMarkerState(true, Result);
        _client.MarkerEnd(MarkerId, Result, InstanceKey);
        return this;
    }

    public OVRTelemetryMarker AddPoint(OVRTelemetry.MarkerPoint point)
    {
        _client.MarkerPointCached(MarkerId, point.NameHandle, InstanceKey);
        return this;
    }

    public void Dispose()
    {
        if (!Sent)
        {
            Send();
        }
    }
}
