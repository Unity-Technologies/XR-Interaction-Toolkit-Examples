/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Meta.WitAi;

namespace Meta.Conduit
{
    /// <summary>
    /// The dispatcher is responsible for deciding which method to invoke when a request is received as well as parsing
    /// the parameters and passing them to the handling method.
    /// </summary>
    internal class ConduitDispatcher : IConduitDispatcher
    {
        /// <summary>
        /// The Conduit manifest which captures the structure of the voice-enabled methods.
        /// </summary>
        public Manifest Manifest { get; private set; }

        /// <summary>
        /// The manifest loader.
        /// </summary>
        private readonly IManifestLoader _manifestLoader;

        /// <summary>
        /// Resolves instances (objects) based on their type.
        /// </summary>
        private readonly IInstanceResolver _instanceResolver;

        /// <summary>
        /// Maps internal parameter names to fully qualified parameter names (roles/slots).
        /// </summary>
        private readonly Dictionary<string, string> _parameterToRoleMap = new Dictionary<string, string>();


        /// <summary>
        /// List of actions that we won't log warnings about any more to avoid spamming the logs.
        /// </summary>
        private readonly HashSet<string> _ignoredActionIds = new HashSet<string>();

        public ConduitDispatcher(IManifestLoader manifestLoader, IInstanceResolver instanceResolver)
        {
            _manifestLoader = manifestLoader;
            _instanceResolver = instanceResolver;
        }

        /// <summary>
        /// Parses the manifest provided and registers its callbacks for dispatching.
        /// </summary>
        /// <param name="manifestFilePath">The path to the manifest file.</param>
        public void Initialize(string manifestFilePath)
        {
            if (Manifest != null)
            {
                return;
            }

            Manifest = _manifestLoader.LoadManifest(manifestFilePath);

            if (Manifest == null)
            {
                return;
            }

            // Map fully qualified role names to internal parameters.
            foreach (var action in Manifest.Actions)
            {
                foreach (var parameter in action.Parameters)
                {
                    if (!_parameterToRoleMap.ContainsKey(parameter.InternalName))
                    {
                        _parameterToRoleMap.Add(parameter.InternalName, parameter.QualifiedName);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the method matching the specified action ID.
        /// This should NOT be called before the dispatcher is initialized.
        /// The parameters must be populated in the parameter provider before calling this method.
        /// </summary>
        /// <param name="parameterProvider">The parameter provider.</param>
        /// <param name="actionId">The action ID (which is also the intent name).</param>
        /// <param name="relaxed">When set to true, will allow matching parameters by type when the names mismatch.</param>
        /// <param name="confidence">The confidence level (between 0-1) of the intent that's invoking the action.</param>
        /// <param name="partial">Whether partial responses should be accepted or not</param>
        /// <returns>True if all invocations succeeded. False if at least one failed or no callbacks were found.</returns>
        public bool InvokeAction(IParameterProvider parameterProvider, string actionId, bool relaxed,
            float confidence = 1f, bool partial = false)
        {
            if (!Manifest.ContainsAction(actionId))
            {
                var hasBeenHandledWithoutConduit = Manifest.WitResponseMatcherIntents.Contains(actionId);
                if (!_ignoredActionIds.Contains(actionId) && !hasBeenHandledWithoutConduit)
                {
                    _ignoredActionIds.Add(actionId);
                    InvokeError(actionId, new Exception($"Conduit did not find intent '{actionId}' in manifest."));
                    VLog.W($"Conduit did not find intent '{actionId}' in manifest.");
                }
                return false;
            }

            parameterProvider.PopulateRoles(_parameterToRoleMap);

            var filter =
                new InvocationContextFilter(parameterProvider, Manifest.GetInvocationContexts(actionId), relaxed);

            var invocationContexts = filter.ResolveInvocationContexts(actionId, confidence, partial);
            if (invocationContexts.Count < 1)
            {
                // Only log if this is non-partial and inverse does not contain any matches either
                if (!partial && filter.ResolveInvocationContexts(actionId, confidence, true).Count < 1)
                {
                   VLog.W(
                        $"Failed to resolve {(partial ? "partial" : "final")} method for {actionId} with supplied context");
                   InvokeError(actionId, new Exception($"Failed to resolve {(partial ? "partial" : "final")} method for {actionId} with supplied context")
                   );
                }

                return false;
            }

            var allSucceeded = true;
            foreach (var invocationContext in invocationContexts)
            {
                try
                {
                    if (!InvokeMethod(invocationContext, parameterProvider, relaxed))
                    {
                        allSucceeded = false;
                    }
                }
                catch (Exception e)
                {
                    VLog.W($"Failed to invoke {invocationContext.MethodInfo.Name}. {e}");
                    allSucceeded = false;
                    InvokeError( invocationContext.MethodInfo.Name, e);
                }
            }

            return allSucceeded;
        }

        public bool InvokeError(string actionId, Exception exception)
        {
            var contexts = Manifest.GetErrorHandlerContexts();
            foreach (var context in contexts)
            {
                var parameterProvider = new ParameterProvider();
                parameterProvider.AddParameter("intent", actionId);
                parameterProvider.AddParameter("error", exception);

                InvokeMethod(context, parameterProvider, true);
            }

            return true;
        }

        /// <summary>
        /// Invokes a method on all callbacks of a specific invocation context. If the method is static, then only a
        /// single call is made. If it's an instance method, then it is invoked on all instances.
        /// </summary>
        /// <param name="invocationContext">The invocation context.</param>
        /// <param name="parameterProvider">The parameter provider.</param>
        /// <param name="relaxed">True if parameters should be matched by type if name matching fails.</param>
        /// <returns>True if the method was invoked successfully on all valid targets.</returns>
        private bool InvokeMethod(InvocationContext invocationContext, IParameterProvider parameterProvider,
            bool relaxed)
        {
            var method = invocationContext.MethodInfo;
            var formalParametersInfo = method.GetParameters();
            var parameterObjects = new object[formalParametersInfo.Length];
            for (var i = 0; i < formalParametersInfo.Length; i++)
            {
                var log = new StringBuilder();
                parameterObjects[i] = parameterProvider.GetParameterValue(formalParametersInfo[i],
                    invocationContext.ParameterMap, relaxed);

                if (parameterObjects[i] == null)
                {
                    InvokeError( invocationContext.MethodInfo.Name, new Exception(
                            $"Failed to find method param while invoking\nType: {invocationContext.Type.FullName}\nMethod: {invocationContext.MethodInfo.Name}\nParameter Issues\n{log}"
                        ));

                    VLog.W(
                        $"Failed to find method param while invoking\nType: {invocationContext.Type.FullName}\nMethod: {invocationContext.MethodInfo.Name}\nParameter Issues\n{log}");
                    return false;
                }
            }

            if (method.IsStatic)
            {
                try
                {
                    method.Invoke(null, parameterObjects.ToArray());
                }
                catch (Exception e)
                {
                    VLog.W($"Failed to invoke static method {method.Name}. {e}");
                    InvokeError( invocationContext.MethodInfo.Name, e);
                    return false;
                }

                return true;
            }
            else
            {
                var allSucceeded = true;
                foreach (var obj in _instanceResolver.GetObjectsOfType(invocationContext.Type))
                {
                    try
                    {
                        method.Invoke(obj, parameterObjects.ToArray());
                    }
                    catch (Exception e)
                    {
                        VLog.W($"Failed to invoke method {method.Name}. {e} on {obj}");
                        allSucceeded = false;
                        InvokeError( invocationContext.MethodInfo.Name,e);
                        continue;
                    }
                }

                return allSucceeded;
            }
        }

        /// <summary>
        /// Filters possible invocation context for an invocation request.
        /// </summary>
        internal class InvocationContextFilter
        {
            /// <summary>
            /// All possible invocation contexts for the specified action.
            /// </summary>
            private readonly List<InvocationContext> _actionContexts;

            /// <summary>
            /// The parameter provider.
            /// </summary>
            private readonly IParameterProvider _parameterProvider;

            /// <summary>
            /// When set to true, parameters with matching types but not name will be resolved when exact matches cannot be found.
            /// </summary>
            private readonly bool _relaxed;

            /// <summary>
            /// Initializes the filter for a given action.
            /// </summary>
            /// <param name="parameterProvider">The parameter provider.</param>
            /// <param name="actionContexts">
            /// All the invocation contexts for the action we want to invoke.
            /// This typically comes from the manifest.
            /// </param>
            /// <param name="relaxed"> When true, will allow matching by type alone when name doesn't match.</param>
            public InvocationContextFilter(IParameterProvider parameterProvider, List<InvocationContext> actionContexts,
                bool relaxed = false)
            {
                _parameterProvider = parameterProvider;
                _actionContexts = actionContexts;
                _relaxed = relaxed;
            }

            /// <summary>
            /// Finds invocation contexts that are applicable to the given action and supplied parameter set.
            /// </summary>
            /// <param name="actionId">The action ID.</param>
            /// <param name="confidence">The confidence level between 0 and 1.</param>
            /// <param name="partial">Whether this is a partial invocation.</param>
            /// <returns></returns>
            public List<InvocationContext> ResolveInvocationContexts(string actionId, float confidence, bool partial)
            {
                // We may have multiple overloads, find the correct match.
                return _actionContexts != null ? _actionContexts.Where(context => CompatibleInvocationContext(context, confidence, partial))
                    .ToList() : new List<InvocationContext>();
            }

            /// <summary>
            /// Returns true if the invocation context is compatible with the actual parameters the parameter provider
            /// is supplying. False otherwise.
            /// If the context is compatible, its parameter map will be updated if necessary.
            /// </summary>
            /// <param name="invocationContext">The invocation context.</param>
            /// <param name="confidence">The intent confidence level.</param>
            /// <param name="partial">Whether this is a partial invocation.</param>
            /// <returns>True if the invocation can be made with the actual parameters. False otherwise.</returns>
            private bool CompatibleInvocationContext(InvocationContext invocationContext, float confidence, bool partial)
            {
                var parameterMap = new Dictionary<string, string>();
                var parameters = invocationContext.MethodInfo.GetParameters();
                if (invocationContext.ValidatePartial != partial)
                {
                    return false;
                }

                if (invocationContext.MinConfidence > confidence || confidence > invocationContext.MaxConfidence)
                {
                    //todo: throw error for out of confidence
                    return false;
                }

                var exactMatches = new HashSet<string>();

                var log = new StringBuilder();
                var allParametersMatched = true;
                foreach (var parameter in parameters)
                {
                    if (_parameterProvider.ContainsParameter(parameter, log))
                    {
                        exactMatches.Add(parameter.Name);
                        continue;
                    }

                    VLog.D(!_relaxed
                        ? $"Could not find value for parameter: {parameter.Name}"
                        : $"Could not find exact value for parameter: {parameter.Name}. Will attempt resolving by type.");

                    allParametersMatched = false;
                }

                if (allParametersMatched)
                {
                    return true;
                }

                if (!_relaxed)
                {
                    VLog.D(
                        $"Failed to resolve parameters. \nType: {invocationContext.Type.FullName}\nMethod: {invocationContext.MethodInfo.Name}\n{log}");
                    return false;
                }

                var actualTypes = new HashSet<Type>();
                foreach (var parameter in parameters)
                {
                    if (exactMatches.Contains(parameter.Name))
                    {
                        continue;
                    }

                    if (actualTypes.Contains(parameter.ParameterType))
                    {
                        VLog.D(
                            $"Failed to resolve parameters by type. More than one value of type {parameter.ParameterType} were provided.");
                        return false;
                    }

                    actualTypes.Add(parameter.ParameterType);

                    var actualParameterNames = _parameterProvider.GetParameterNamesOfType(parameter.ParameterType)
                        .Where(parameterName => !exactMatches.Contains(parameterName)).ToList();

                    if (actualParameterNames.Count != 1)
                    {
                        VLog.D($"Failed to find compatible value for {parameter.Name}");

                        return false;
                    }

                    parameterMap[parameter.Name] = actualParameterNames[0];
                }

                invocationContext.ParameterMap = parameterMap;
                return true;
            }
        }
    }
}
