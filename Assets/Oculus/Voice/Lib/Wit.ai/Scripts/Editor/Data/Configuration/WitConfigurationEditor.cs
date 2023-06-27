/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meta.WitAi.Data.Configuration.Tabs;
using Lib.Wit.Runtime.Requests;
using Meta.Conduit.Editor;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Utilities;
using Meta.Conduit;
using Meta.WitAi.Lib;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.Windows.Conponents;

namespace Meta.WitAi.Windows
{
    public class WitConfigurationEditor : UnityEditor.Editor
    {
        public WitConfiguration Configuration { get; private set; }
        private string _serverToken;
        private string _appName;
        private string _appID;
        private bool _initialized = false;
        public bool drawHeader = true;
        private bool _foldout = true;
        private int _requestTab = 0;
        private bool _manifestAvailable = false;
        private bool _syncInProgress = false;
        private bool _didCheckAutoTrainAvailability = false;
        private bool _isAutoTrainAvailable = false;

        /// <summary>
        /// Whether or not server specific functionality like sync
        /// should be disabled for this configuration
        /// </summary>
        protected virtual bool _disableServerPost => false;

        internal static readonly AssemblyWalker AssemblyWalker = new AssemblyWalker();
        private static ConduitStatistics _statistics;
        private static readonly AssemblyMiner AssemblyMiner = new AssemblyMiner(new WitParameterValidator());
        private static readonly ManifestGenerator ManifestGenerator = new ManifestGenerator(AssemblyWalker, AssemblyMiner);
        private static readonly ManifestLoader ManifestLoader = new ManifestLoader();
        private static readonly IWitVRequestFactory VRequestFactory = new WitVRequestFactory();

        private EnumSynchronizer _enumSynchronizer;

        private WitConfigurationEditorTab[] _tabs;

        private const string ENTITY_SYNC_CONSENT_KEY = "Conduit.EntitySync.Consent";

        // Generate
        private static ConduitStatistics Statistics
        {
            get
            {
                if (_statistics == null)
                {
                    _statistics = new ConduitStatistics(new PersistenceLayer());
                }
                return _statistics;
            }
        }

        protected virtual Texture2D HeaderIcon => WitTexts.HeaderIcon;
        public virtual string HeaderUrl => WitTexts.GetAppURL(Configuration.GetApplicationId(), WitTexts.WitAppEndpointType.Settings);
        protected virtual string DocsUrl => WitTexts.Texts.WitDocsUrl;
        protected virtual string OpenButtonLabel => WitTexts.Texts.WitOpenButtonLabel;

        public void Initialize()
        {
            _tabs =  AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(WitConfigurationEditorTab)))
                .Select(type => (WitConfigurationEditorTab)Activator.CreateInstance(type))
                .OrderBy(tab =>tab.TabOrder)
                .ToArray();

            // Refresh configuration & auth tokens
            Configuration = target as WitConfiguration;

            // Get app server token
            _serverToken = WitAuthUtility.GetAppServerToken(Configuration);
            if (CanConfigurationRefresh(Configuration) && WitConfigurationUtility.IsServerTokenValid(_serverToken))
            {
                // Get client token if needed
                _appID = Configuration.GetApplicationId();
                if (string.IsNullOrEmpty(_appID))
                {
                    Configuration.SetServerToken(_serverToken);
                }
                // Refresh additional data
                else
                {
                    SafeRefresh();
                }
            }
        }

        public void OnDisable()
        {
            Statistics.Persist();
        }

        public override void OnInspectorGUI()
        {
            // Init if needed
            if (!_initialized || Configuration != target)
            {
                Initialize();
                _initialized = true;
            }

            // Draw header
            WitEditorUI.LayoutHeaderText(target.name, HeaderUrl, DocsUrl);
            

            // Layout content
            LayoutContent();
        }

        private void GenerateManifestIfNeeded()
        {
            if (!Configuration.useConduit || Configuration == null)
            {
                return;
            }

            // Get full manifest path & ensure it exists
            string manifestPath = Configuration.GetManifestEditorPath();
            _manifestAvailable = File.Exists(manifestPath);

            // Auto-generate manifest
            if (!_manifestAvailable)
            {
                GenerateManifest(Configuration, false);
            }
        }

        private void LayoutConduitContent()
        {
            var isServerTokenValid = WitConfigurationUtility.IsServerTokenValid(_serverToken);
            if (!isServerTokenValid && !_disableServerPost)
            {
                GUILayout.TextArea(WitTexts.Texts.ConfigurationConduitMissingTokenLabel, WitStyles.LabelError);
            }

            // Set conduit
            var useConduit = GUILayout.Toggle(Configuration.useConduit, "Use Conduit (Beta)");
            if (Configuration.useConduit != useConduit)
            {
                Configuration.useConduit = useConduit;
                EditorUtility.SetDirty(Configuration);
            }

            GenerateManifestIfNeeded();

            // Configuration buttons
            EditorGUI.indentLevel++;
            GUILayout.Space(EditorGUI.indentLevel * WitStyles.ButtonMargin);
            {
                GUI.enabled = Configuration.useConduit;
                var useRelaxedMatching = GUILayout.Toggle(Configuration.relaxedResolution, new GUIContent("Relaxed Resolution", "Allows resolving parameters by value if an exact match was not found. Disable to improve runtime performance."));
                if (Configuration.relaxedResolution != useRelaxedMatching)
                {
                    Configuration.relaxedResolution = useRelaxedMatching;
                    EditorUtility.SetDirty(Configuration);
                }

                GUILayout.BeginHorizontal();
                {
                    if (WitEditorUI.LayoutTextButton(_manifestAvailable ? "Update Manifest" : "Generate Manifest"))
                    {
                        GenerateManifest(Configuration, true);
                    }

                    GUI.enabled = Configuration.useConduit && _manifestAvailable;
                    if (WitEditorUI.LayoutTextButton("Select Manifest") && _manifestAvailable)
                    {
                        Selection.activeObject =
                            AssetDatabase.LoadAssetAtPath<TextAsset>(Configuration.GetManifestEditorPath());
                    }

                    GUI.enabled = Configuration.useConduit;
                    if (WitEditorUI.LayoutTextButton("Specify Assemblies"))
                    {
                        PresentAssemblySelectionDialog();
                    }

                    if (isServerTokenValid && !_disableServerPost)
                    {
                        GUI.enabled = Configuration.useConduit && _manifestAvailable && !_syncInProgress;
                        if (WitEditorUI.LayoutTextButton("Sync Entities"))
                        {
                            SyncEntities();
                            GUIUtility.ExitGUI();
                            return;
                        }
                        if (_isAutoTrainAvailable)
                        {
                            if (WitEditorUI.LayoutTextButton("Auto Train") && _manifestAvailable)
                            {
                                SyncEntities(() => { AutoTrainOnWitAi(Configuration); });
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        protected virtual void LayoutContent()
        {
            // Begin vertical box
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Check for app name/id update
            ReloadAppData();

            // Title Foldout
            GUILayout.BeginHorizontal();
            string foldoutText = WitTexts.Texts.ConfigurationHeaderLabel;
            if (!string.IsNullOrEmpty(_appName))
            {
                foldoutText = foldoutText + " - " + _appName;
            }

            _foldout = WitEditorUI.LayoutFoldout(new GUIContent(foldoutText), _foldout);
            // Refresh button
            if (CanConfigurationRefresh(Configuration))
            {
                if (string.IsNullOrEmpty(_appName))
                {
                    bool isValid =  WitConfigurationUtility.IsServerTokenValid(_serverToken);
                    GUI.enabled = isValid;
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationRefreshButtonLabel))
                    {
                        ApplyServerToken(_serverToken);
                    }
                }
                else
                {
                    bool isRefreshing = Configuration.IsUpdatingData();
                    GUI.enabled = !isRefreshing;
                    if (WitEditorUI.LayoutTextButton(isRefreshing ? WitTexts.Texts.ConfigurationRefreshingButtonLabel : WitTexts.Texts.ConfigurationRefreshButtonLabel))
                    {
                        SafeRefresh();
                    }
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(WitStyles.ButtonMargin);

            // Show configuration app data
            if (_foldout)
            {
                // Indent
                EditorGUI.indentLevel++;

                // Server access token
                bool updated = false;
                WitEditorUI.LayoutPasswordField(WitTexts.ConfigurationServerTokenContent, ref _serverToken, ref updated);
                if (updated && WitConfigurationUtility.IsServerTokenValid(_serverToken))
                {
                    ApplyServerToken(_serverToken);
                }

                // Additional data
                if (Configuration)
                {
                    LayoutConfigurationData();
                }

                // Undent
                EditorGUI.indentLevel--;
            }

            // End vertical box layout
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            LayoutConduitContent();
            GUILayout.EndVertical();

            // Layout configuration request tabs
            LayoutConfigurationRequestTabs();

            // Additional open wit button
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(OpenButtonLabel, WitStyles.TextButton))
            {
                Application.OpenURL(HeaderUrl);
            }
        }
        // Reload app data if needed
        private void ReloadAppData()
        {
            // Check for changes
            string checkID = "";
            string checkName = "";
            if (Configuration != null)
            {
                checkID = Configuration.GetApplicationId();
                if (!string.IsNullOrEmpty(checkID))
                {
                    checkName = Configuration.GetApplicationInfo().name;
                }
            }
            // Reset
            if (!string.Equals(_appName, checkName) || !string.Equals(_appID, checkID))
            {
                // Refresh app data
                _appName = checkName;
                _appID = checkID;

                // Do not clear token if failed to set
                string newToken = WitAuthUtility.GetAppServerToken(Configuration);
                if (!string.IsNullOrEmpty(newToken))
                {
                    _serverToken = newToken;
                }
            }
        }
        // Apply server token
        public void ApplyServerToken(string newToken)
        {
            if (newToken != _serverToken)
            {
                _serverToken = newToken;
                Configuration.ResetData();
            }

            WitAuthUtility.ServerToken = _serverToken;
            Configuration.SetServerToken(_serverToken);

            GenerateManifestIfNeeded();
        }
        // Whether or not to allow a configuration to refresh
        protected virtual bool CanConfigurationRefresh(WitConfiguration configuration)
        {
            return configuration;
        }
        // Layout configuration data
        protected virtual void LayoutConfigurationData()
        {
            // Reset update
            bool updated = false;
            // Client access field
            string clientAccessToken = Configuration.GetClientAccessToken();
            WitEditorUI.LayoutPasswordField(WitTexts.ConfigurationClientTokenContent, ref clientAccessToken, ref updated);
            if (updated && string.IsNullOrEmpty(clientAccessToken))
            {
                VLog.E("Client access token is not defined. Cannot perform requests with '" + Configuration.name + "'.");
            }
            // Timeout field
            WitEditorUI.LayoutIntField(WitTexts.ConfigurationRequestTimeoutContent, ref Configuration.timeoutMS, ref updated);
            // Updated
            if (updated)
            {
                Configuration.SetClientAccessToken(clientAccessToken);
            }

            // Show configuration app data
            LayoutConfigurationEndpoint();
        }
        // Layout endpoint data
        protected virtual void LayoutConfigurationEndpoint()
        {
            // Generate if needed
            if (Configuration.endpointConfiguration == null)
            {
                Configuration.endpointConfiguration = new WitEndpointConfig();
                EditorUtility.SetDirty(Configuration);
            }

            // Handle via serialized object
            var serializedObj = new SerializedObject(Configuration);
            var serializedProp = serializedObj.FindProperty("endpointConfiguration");
            EditorGUILayout.PropertyField(serializedProp);
            serializedObj.ApplyModifiedProperties();
        }
        // Tabs
        protected virtual void LayoutConfigurationRequestTabs()
        {
            // Application info
            Data.Info.WitAppInfo appInfo = Configuration.GetApplicationInfo();

            // Indent
            EditorGUI.indentLevel++;

            // Iterate tabs
            if (_tabs != null)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < _tabs.Length; i++)
                {
                    // Enable if not selected
                    GUI.enabled = _requestTab != i;
                    // If valid and clicked, begin selecting
                    if (null != appInfo.id &&_tabs[i].ShouldTabShow(appInfo))
                    {
                        if (WitEditorUI.LayoutTabButton(_tabs[i].GetTabText(true)))
                        {
                            _requestTab = i;
                        }
                    }
                    // If invalid, stop selecting
                    else if (_requestTab == i)
                    {
                        _requestTab = -1;
                    }
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Layout selected tab using property id
                string propertyID = _requestTab >= 0 && _requestTab < _tabs.Length
                    ? _tabs[_requestTab].TabID
                    : string.Empty;
                if (!string.IsNullOrEmpty(propertyID) && Configuration != null)
                {
                    SerializedObject serializedObj = new SerializedObject(Configuration);
                    SerializedProperty serializedProp = serializedObj.FindProperty(_tabs[_requestTab].GetPropertyName(propertyID));
                    if (serializedProp == null)
                    {
                        WitEditorUI.LayoutErrorLabel(_tabs[_requestTab].GetTabText(false));
                    }
                    else if (!serializedProp.isArray)
                    {
                        EditorGUILayout.PropertyField(serializedProp);
                    }
                    else if (serializedProp.arraySize == 0)
                    {
                        WitEditorUI.LayoutErrorLabel(_tabs[_requestTab].GetTabText(false));
                    }
                    else
                    {
                        for (int i = 0; i < serializedProp.arraySize; i++)
                        {
                            SerializedProperty serializedPropChild = serializedProp.GetArrayElementAtIndex(i);
                            EditorGUILayout.PropertyField(serializedPropChild);
                        }
                    }

                    serializedObj.ApplyModifiedProperties();
                }
            }

            // Undent
            EditorGUI.indentLevel--;
        }

        // Safe refresh
        protected virtual void SafeRefresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (WitConfigurationUtility.IsServerTokenValid(_serverToken))
            {
                Configuration.SetServerToken(_serverToken);
            }
            else if (WitConfigurationUtility.IsClientTokenValid(Configuration.GetClientAccessToken()))
            {
                Configuration.RefreshAppInfo();
            }
            if (Configuration.useConduit)
            {
                CheckAutoTrainAvailabilityIfNeeded();
            }
        }

        private void CheckAutoTrainAvailabilityIfNeeded()
        {
            if (_didCheckAutoTrainAvailability || !WitConfigurationUtility.IsServerTokenValid(_serverToken)) {
                return;
            }

            _didCheckAutoTrainAvailability = true;
            CheckAutoTrainIsAvailable(Configuration, (isAvailable) => {
                _isAutoTrainAvailable = isAvailable;
            });
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
            foreach (var configuration in WitConfigurationUtility.GetLoadedConfigurations())
            {
                if (configuration != null && configuration.useConduit)
                {
                    GenerateManifest(configuration, false);
                }
            }
        }

        /// <summary>
        /// Generates a manifest and optionally opens it in the editor.
        /// </summary>
        /// <param name="configuration">The configuration that we are generating the manifest for.</param>
        /// <param name="openManifest">If true, will open the manifest file in the code editor.</param>
        private static void GenerateManifest(WitConfiguration configuration, bool openManifest)
        {
            AssemblyWalker.AssembliesToIgnore = new HashSet<string>(configuration.excludedAssemblies);

            // Generate
            var startGenerationTime = DateTime.UtcNow;
            var appInfo = configuration.GetApplicationInfo();
            var manifest = ManifestGenerator.GenerateManifest(appInfo.name, appInfo.id);
            var endGenerationTime = DateTime.UtcNow;

            // Get file path
            var fullPath = configuration.GetManifestEditorPath();
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                fullPath = GetManifestPullPath(configuration, true);
            }

            // Write to file
            try
            {
                var writer = new StreamWriter(fullPath);
                writer.NewLine = "\n";
                writer.WriteLine(manifest);
                writer.Close();
            }
            catch (Exception e)
            {
                VLog.E($"Conduit manifest failed to generate\nPath: {fullPath}\n{e}");
                return;
            }

            Statistics.SuccessfulGenerations++;
            Statistics.AddFrequencies(AssemblyMiner.SignatureFrequency);
            Statistics.AddIncompatibleFrequencies(AssemblyMiner.IncompatibleSignatureFrequency);
            var generationTime = endGenerationTime - startGenerationTime;
            var unityPath = fullPath.Replace(Application.dataPath, "Assets");
            AssetDatabase.ImportAsset(unityPath);

            var configName = configuration.name;
            var manifestName = Path.GetFileNameWithoutExtension(unityPath);
            #if UNITY_2021_2_OR_NEWER
            var configPath = AssetDatabase.GetAssetPath(configuration);
            configName = $"<a href=\"{configPath}\">{configName}</a>";
            manifestName = $"<a href=\"{unityPath}\">{manifestName}</a>";
            #endif
            VLog.D($"Conduit manifest generated\nConfiguration: {configName}\nManifest: {manifestName}\nGeneration Time: {generationTime.TotalMilliseconds} ms");

            if (openManifest)
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
            }
        }

        // Show dialog to disable/enable assemblies
        private void PresentAssemblySelectionDialog()
        {
            var assemblyNames = AssemblyWalker.GetAllAssemblies().Select(a => a.FullName).ToList();
            AssemblyWalker.AssembliesToIgnore = new HashSet<string>(Configuration.excludedAssemblies);
            WitMultiSelectionPopup.Show(assemblyNames, AssemblyWalker.AssembliesToIgnore, (disabledAssemblies) => {
                AssemblyWalker.AssembliesToIgnore = new HashSet<string>(disabledAssemblies);
                Configuration.excludedAssemblies = new List<string>(AssemblyWalker.AssembliesToIgnore);
                GenerateManifestIfNeeded();
            });
        }

        // Sync entities
        private void SyncEntities(Action successCallback = null)
        {
            if (!EditorUtility.DisplayDialog("Synchronizing with Wit.Ai entities", "This will synchronize local enums with Wit.Ai entities. Part of this process involves generating code locally and may result in overwriting existing code. Please make sure to backup your work before proceeding.", "Proceed", "Cancel", DialogOptOutDecisionType.ForThisSession, ENTITY_SYNC_CONSENT_KEY))
            {
                VLog.D("Entity Sync cancelled");
                return;
            }

            // Fail without server token
            var validServerToken = WitConfigurationUtility.IsServerTokenValid(_serverToken);
            if (!validServerToken)
            {
                VLog.E($"Conduit Sync Failed\nError: Invalid server token");
                return;
            }

            // Generate
            if (_enumSynchronizer == null)
            {
                _enumSynchronizer = new EnumSynchronizer(Configuration, AssemblyWalker, new FileIo(), VRequestFactory);
            }

            // Sync
            _syncInProgress = true;
            EditorUtility.DisplayProgressBar("Conduit Entity Sync", "Generating Manifest.", 0f );
            GenerateManifest(Configuration, false);
            var manifest = ManifestLoader.LoadManifest(Configuration.ManifestLocalPath);
            const float initializationProgress = 0.1f;
            EditorUtility.DisplayProgressBar("Conduit Entity Sync", "Synchronizing entities. Please wait...", initializationProgress);
            VLog.D("Synchronizing enums with Wit.Ai entities");
            CoroutineUtility.StartCoroutine(_enumSynchronizer.SyncWitEntities(manifest, (success, data) =>
                {
                    _syncInProgress = false;
                    EditorUtility.ClearProgressBar();
                    if (!success)
                    {
                        VLog.E($"Conduit failed to synchronize entities\nError: {data}");
                    }
                    else
                    {
                        VLog.D("Conduit successfully synchronized entities");
                        successCallback?.Invoke();
                    }
                },
                (status, progress) =>
                {
                    EditorUtility.DisplayProgressBar("Conduit Entity Sync", status,
                        initializationProgress + (1f - initializationProgress) * progress);
                }));
        }

        private static void AutoTrainOnWitAi(WitConfiguration configuration)
        {
            var manifest = ManifestLoader.LoadManifest(configuration.ManifestLocalPath);
            var intents = ManifestGenerator.ExtractManifestData();
            VLog.D($"Auto training on WIT.ai: {intents.Count} intents.");

            configuration.ImportData(manifest, (isSuccess, error) =>
            {
                if (isSuccess)
                {
                    EditorUtility.DisplayDialog("Auto Train", "Successfully started auto train process on WIT.ai.",
                        "OK");
                }
                else
                {
                    VLog.E($"Failed to import generated manifest JSON into WIT.ai: {error}. Manifest:\n{manifest}");
                    EditorUtility.DisplayDialog("Auto Train", "Failed to start auto train process on WIT.ai.", "OK");
                }
            });
        }

        private static void CheckAutoTrainIsAvailable(WitConfiguration configuration, Action<bool> onComplete)
        {
            Meta.WitAi.Data.Info.WitAppInfo appInfo = configuration.GetApplicationInfo();
            string manifestText = ManifestGenerator.GenerateEmptyManifest(appInfo.name, appInfo.id);
            var manifest = ManifestLoader.LoadManifestFromString(manifestText);
            configuration.ImportData(manifest, (result, error) => onComplete(result), true);
        }

        private static string GetManifestPullPath(WitConfiguration configuration, bool shouldCreateDirectoryIfNotExist = false)
        {
            string directory = Application.dataPath + "/Oculus/Voice/Resources";
            if (shouldCreateDirectoryIfNotExist)
            {
                IOUtility.CreateDirectory(directory, true);
            }
            return directory + "/" + configuration.ManifestLocalPath;
        }
    }
}
