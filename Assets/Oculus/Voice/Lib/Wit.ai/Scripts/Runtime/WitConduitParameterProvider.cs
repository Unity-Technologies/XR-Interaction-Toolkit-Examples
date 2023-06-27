/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Reflection;
using System.Text;
using Meta.WitAi.Data;
using Meta.WitAi.Json;
using Meta.Conduit;

namespace Meta.WitAi
{
    [Obsolete ("Use ParameterProvider.SetSpecializedParameter() instead of this class")]
    internal class WitConduitParameterProvider : ParameterProvider
    {
        protected override object GetSpecializedParameter(ParameterInfo formalParameter)
        {
            if (formalParameter.ParameterType == typeof(WitResponseNode) && ActualParameters.ContainsKey(WitResponseNodeReservedName.ToLower()))
            {
                return ActualParameters[WitResponseNodeReservedName.ToLower()];
            }
            if (formalParameter.ParameterType == typeof(VoiceSession) && ActualParameters.ContainsKey(VoiceSessionReservedName.ToLower()))
            {
                return ActualParameters[VoiceSessionReservedName.ToLower()];
            }

            // Log warning when not found
            StringBuilder error = new StringBuilder();
            error.AppendLine("Specialized parameter not found");
            error.AppendLine($"Parameter Type: {formalParameter.ParameterType}");
            error.AppendLine($"Parameter Name: {formalParameter.Name}");
            error.AppendLine($"Actual Parameters: {ActualParameters.Keys.Count}");
            foreach (var key in ActualParameters.Keys)
            {
                string val = ActualParameters[key] == null ? "NULL" : ActualParameters[key].GetType().ToString();
                error.AppendLine($"\t{key}: {val}");
            }
            VLog.W(error.ToString());
            return null;
        }

        protected override bool SupportedSpecializedParameter(ParameterInfo formalParameter)
        {
            return formalParameter.ParameterType == typeof(WitResponseNode) || formalParameter.ParameterType == typeof(VoiceSession);
        }
    }
}
