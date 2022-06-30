using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DigitalOpus.MB.Core
{

    public class MB3_EditorMethods : MB2_EditorMethodsInterface
    {

        enum saveTextureFormat
        {
            png,
            tga,
        }

        private saveTextureFormat SAVE_FORMAT = saveTextureFormat.png;

        private List<Texture2D> _texturesWithReadWriteFlagSet = new List<Texture2D>();
        
        private Dictionary<Texture2D, TextureFormatInfo_AbstractDefaultPlatform> _textureFormatMap_DefaultAbstract = new Dictionary<Texture2D, TextureFormatInfo_AbstractDefaultPlatform>();
        private Dictionary<Texture2D, TextureFormatInfo_PlatformOverride> _textureFormatMap_PlatformOverride = new Dictionary<Texture2D, TextureFormatInfo_PlatformOverride>();

        public MobileTextureSubtarget AndroidBuildTexCompressionSubtarget;
#if UNITY_TIZEN
        //public MobileTextureSubtarget TizenBuildTexCompressionSubtarget;
#endif
        public void Clear()
        {
            _texturesWithReadWriteFlagSet.Clear();
            _textureFormatMap_DefaultAbstract.Clear();
            _textureFormatMap_PlatformOverride.Clear();
        }

        public void OnPreTextureBake()
        {
            AndroidBuildTexCompressionSubtarget = MobileTextureSubtarget.Generic;

            // the texture override in build settings for some platforms causes poor quality
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.Generic)
            {
                AndroidBuildTexCompressionSubtarget = EditorUserBuildSettings.androidBuildSubtarget; //remember so we can restore later
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
            }
#if UNITY_TIZEN
            TizenBuildTexCompressionSubtarget = MobileTextureSubtarget.Generic;
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Tizen &&
                EditorUserBuildSettings.tizenBuildSubtarget != MobileTextureSubtarget.Generic)
            {
                TizenBuildTexCompressionSubtarget = EditorUserBuildSettings.tizenBuildSubtarget; //remember so we can restore later
                EditorUserBuildSettings.tizenBuildSubtarget = MobileTextureSubtarget.Generic;
            }
#endif
        }

        public void OnPostTextureBake()
        {
            if (AndroidBuildTexCompressionSubtarget != MobileTextureSubtarget.Generic)
            {
                EditorUserBuildSettings.androidBuildSubtarget = AndroidBuildTexCompressionSubtarget;
                AndroidBuildTexCompressionSubtarget = MobileTextureSubtarget.Generic;
            }
#if UNITY_TIZEN
            if (TizenBuildTexCompressionSubtarget != MobileTextureSubtarget.Generic)
            {
                EditorUserBuildSettings.tizenBuildSubtarget = TizenBuildTexCompressionSubtarget;
                TizenBuildTexCompressionSubtarget = MobileTextureSubtarget.Generic;
            }
#endif
        }

#if UNITY_5_5_OR_NEWER
        class TextureFormatInfo_AbstractDefaultPlatform
        {
            public TextureImporterCompression abstractDefaultTextureCompression;
            public bool abstractDefaultDoCrunchCompression;
            public TextureImporterFormat abstractDefaultPlatformFormat;
            public int platformCompressionQuality;
            public String platform;
            public bool isNormalMap;
            public bool doPlatformOverride;

            public TextureFormatInfo_AbstractDefaultPlatform(TextureImporterCompression textureCompression, bool doCrunch, string platformString, TextureImporterFormat abstractDefaultPlatformFormat, int platformCompressionQuality, bool isNormMap, bool overridden)
            {
                this.abstractDefaultTextureCompression = textureCompression;
                abstractDefaultDoCrunchCompression = doCrunch;
                platform = platformString;
                this.abstractDefaultPlatformFormat = abstractDefaultPlatformFormat;
                this.platformCompressionQuality = platformCompressionQuality;
                this.isNormalMap = isNormMap;
                doPlatformOverride = overridden;
#if UNITY_2018_3_OR_NEWER
                Debug.Assert(abstractDefaultPlatformFormat == TextureImporterFormat.Automatic ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.Alpha8 ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.RGBA32 ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.RGB24 ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.RGB16 ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.R16 ||
                             abstractDefaultPlatformFormat == TextureImporterFormat.R8, "Platform format should be an abstract format, Auto, Alpha8, RGBA32, RGB24, RGB16, R16, R8");
#endif
            }
        }

        class TextureFormatInfo_PlatformOverride
        {
            public TextureImporterFormat format;
            public String platform;
            public bool isNormalMap;
            public bool doPlatformOverride;

            public TextureFormatInfo_PlatformOverride(string platformString, TextureImporterFormat platformFormat, bool isNormMap, bool overridden)
            {
                platform = platformString;
                format = platformFormat;
                this.isNormalMap = isNormMap;
                doPlatformOverride = overridden;
            }
        }



        public bool IsNormalMap(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                if (((TextureImporter)ai).textureType == TextureImporterType.NormalMap) return true;
            }
            return false;
        }

        public void ConvertTextureFormat_DefaultPlatform(Texture2D tx, TextureFormat targetFormat, bool isNormalMap)
        {
            //pixel values don't copy correctly from one texture to another when isNormal is set so unset it.
            bool isFormatMapping;
            TextureImporterFormat importerFormat = Map_TextureFormat_2_TextureImporterFormat(targetFormat, out isFormatMapping);
            if (!isFormatMapping)
            {
                importerFormat = TextureImporterFormat.RGBA32;
            }

            TextureFormatInfo_AbstractDefaultPlatform toFormat = new TextureFormatInfo_AbstractDefaultPlatform(TextureImporterCompression.Uncompressed, false, MBVersionEditor.GetPlatformString(), importerFormat, 0, isNormalMap, false);
            _SetTextureFormat_DefaultPlatform(tx, toFormat, true, false);
        }

        public void ConvertTextureFormat_PlatformOverride(Texture2D tx, TextureFormat targetFormat, bool isNormalMap)
        {
            //pixel values don't copy correctly from one texture to another when isNormal is set so unset it.
            bool isFormatMapping;
            TextureImporterFormat importerFormat = Map_TextureFormat_2_TextureImporterFormat(targetFormat, out isFormatMapping);
            if (!isFormatMapping)
            {
                importerFormat = TextureImporterFormat.RGBA32;
            }

            TextureFormatInfo_PlatformOverride toFormat = new TextureFormatInfo_PlatformOverride(MBVersionEditor.GetPlatformString(), importerFormat, isNormalMap, true);
            _SetTextureFormat_PlatformOverride(tx, toFormat, true, false);
        }

        private void _SetTextureFormat_DefaultPlatform(Texture2D tx, TextureFormatInfo_AbstractDefaultPlatform toThisFormat, bool addToList, bool setNormalMap)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is UnityEditor.TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                bool doImport = false;

                bool is2017 = Application.unityVersion.StartsWith("20");
                if (is2017)
                {
                    doImport = _Set_DefaultPlatform_TextureFormatAndEnableDisablePlatformOverride_2017(tx, toThisFormat, addToList, setNormalMap, textureImporter);
                }
                else
                {
                    doImport = _SetTextureFormatUnity5(tx, toThisFormat, addToList, setNormalMap, textureImporter);
                }
                if (doImport) AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx), ImportAssetOptions.ForceUpdate);
            }
        }

        private void _SetTextureFormat_PlatformOverride(Texture2D tx, TextureFormatInfo_PlatformOverride toThisFormat, bool addToList, bool setNormalMap)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is UnityEditor.TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                bool doImport = _Set_PlatformOverride_2017(tx, toThisFormat, addToList, setNormalMap, textureImporter);
                if (doImport) AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx), ImportAssetOptions.ForceUpdate);
            }
        }

        private bool _ChangeNormalMapTypeIfNecessary(TextureImporter textureImporter, bool setNormalMap)
        {
            bool doImport = false;
            if (textureImporter.textureType == TextureImporterType.NormalMap && !setNormalMap)
            {
                textureImporter.textureType = TextureImporterType.Default;
                doImport = true;
            }

            if (textureImporter.textureType != TextureImporterType.NormalMap && setNormalMap)
            {
                textureImporter.textureType = TextureImporterType.NormalMap;
                doImport = true;
            }
            return doImport;
        }

        private void RememberTextureFormatChange(Texture2D tx, TextureFormatInfo_AbstractDefaultPlatform tfi)
        {
            Debug.Assert(!_textureFormatMap_DefaultAbstract.ContainsKey(tx),"We have already converted the format for this texture " + tx + " we should only do this once.");
            Debug.Assert(!_textureFormatMap_PlatformOverride.ContainsKey(tx), "We have added a TextureImporter platform override for this texture " + tx + " we should not also be changing the default format.");
            _textureFormatMap_DefaultAbstract.Add(tx, tfi);
        }

        private void RememberTextureFormatChange(Texture2D tx, TextureFormatInfo_PlatformOverride tfi)
        {
            Debug.Assert(!_textureFormatMap_PlatformOverride.ContainsKey(tx), "We have already added a platform override for texture " + tx + " we should only do this once.");
            Debug.Assert(!_textureFormatMap_DefaultAbstract.ContainsKey(tx), "We have added a converted the format for default platform for texture " + tx + " we should not also be changing the platform override.");
            _textureFormatMap_PlatformOverride.Add(tx, tfi);
        }


        private bool _Set_PlatformOverride_2017(Texture2D tx, TextureFormatInfo_PlatformOverride toThisFormat, bool rememberRestoreSettings, bool setNormalMap, TextureImporter textureImporter)
        {
            bool is2017 = Application.unityVersion.StartsWith("20");
            if (!is2017)
            {
                Debug.LogError("Wrong texture format converter. 2017 Should not be called for Unity Version " + Application.unityVersion);
                return false;
            }

            // Reimport takes a long time so we only want to reimport if necessary.
            bool doImport = false;

            // Record the old format so we can restore after changing format.
            string restoreBuildPlatform = GetPlatformString();

            // Get the restore settings
            // First check if there is an override for this platform.
            TextureImporterPlatformSettings platformOverriddenTips = textureImporter.GetPlatformTextureSettings(restoreBuildPlatform);
            TextureFormatInfo_PlatformOverride restoreTfi;
            {
                restoreTfi = new TextureFormatInfo_PlatformOverride(restoreBuildPlatform, platformOverriddenTips.format, textureImporter.textureType == TextureImporterType.NormalMap, platformOverriddenTips.overridden);
            }

            string targetBuildPlatform = toThisFormat.platform;

            // Check if anything needs changing and if so remember that we need to reimport;
            {
                if (targetBuildPlatform != null)
                {
                    if (platformOverriddenTips.overridden != toThisFormat.doPlatformOverride)
                    {
                        // Disable/enable the platform override
                        platformOverriddenTips.overridden = toThisFormat.doPlatformOverride;
                        textureImporter.SetPlatformTextureSettings(platformOverriddenTips);
                        doImport = true;
                    }
                }

                if (_ChangeNormalMapTypeIfNecessary(textureImporter, setNormalMap))
                {
                    doImport = true;
                }

                if (platformOverriddenTips.format != toThisFormat.format)
                {
                    platformOverriddenTips.format = toThisFormat.format;
                    textureImporter.SetPlatformTextureSettings(platformOverriddenTips);
                    doImport = true;
                }
            }

            if (doImport)
            {
                string s;
                if (rememberRestoreSettings)
                {
                    s = "Setting texture platform override for ";
                }
                else
                {
                    s = "Restoring texture platform override for ";
                }
                s += String.Format("{0}  FROM: isNormal{1} format={2} hadOverride={3} TO: isNormal={4} format={5} hadOverride={6}",
                                tx, restoreTfi.isNormalMap, restoreTfi.format, restoreTfi.doPlatformOverride,
                                    setNormalMap, toThisFormat.format, toThisFormat.doPlatformOverride);

                Debug.Log(s);
                if (doImport && rememberRestoreSettings && !_textureFormatMap_PlatformOverride.ContainsKey(tx))
                {
                    RememberTextureFormatChange(tx, restoreTfi);
                }
            }

            return doImport;
        }

        /// <summary>
        /// Useful for from <--> to truecolor <--> compressed.
        /// Not useful for setting a texture to a specific compression format (eg DXT5)
        /// 
        /// Importer has "Default" PlatformImportSettings which can be overridden by "platform overrides".
        /// This enables/disables the override and sets the "Defalut" to/from something compressed.
        /// </summary>
        private bool _Set_DefaultPlatform_TextureFormatAndEnableDisablePlatformOverride_2017(Texture2D tx, TextureFormatInfo_AbstractDefaultPlatform toThisFormat, bool rememberRestoreSettings, bool setNormalMap, TextureImporter textureImporter)
        {
            /*
             * HOW THE TEXTURE IMPORTER WORKS.
             *    Importer has "Default" PlatformImportSettings which can be overridden on a platform by platform basis.
             *    Default:
             *          Format can be
             *              Automatic => uses TextureImporter.GetAutomaticFormat
             *              RGB, ARGB, etc... These are abstract formats (list of channels, bitdepth), not specific algorithms like PVRT, ETC.
             *          Compression is Normal *DEFAULT, None, High, Low. These are abstract. Will get translated to specific algorithm depending on platform and Format.
             *    Overrides per platform (iOS, Android ...)
             *          Format is a concrete algorithm like ETC, ASTC, BVRT
             *          Compressor Quality (Fastest, Normal, Best) These are abstract, not sure how this affects format
             *
             *    Crunch compression: See blog post: https://blogs.unity3d.com/2017/11/15/updated-crunch-texture-compression-library/. Is lossy, Is for distribution
             *    (textures are decompressed before being loaded into GPU). Only for DXT, RGB Crunched ETC, RGBA Crunched ETC2 format. If you enable the
             *    “Use Crunch Compression” option in the Default tab, all the textures on Android platform will be compressed with ETC Crunch by default.
             *
             * WHAT MESH BAKER NEEDS.
             *      Needs to be able to read the pixels. Also textures should be in "true-color" RGB or ARGB format for best fidelity.
             *          Turn off platform override if it is enabled
             *          Set default to "uncompressed", "RGB or ARGB"
             *      
            */
            bool is2017 = Application.unityVersion.StartsWith("20");
            if (!is2017)
            {
                Debug.LogError("Wrong texture format converter. 2017 Should not be called for Unity Version " + Application.unityVersion);
                return false;
            }

            // Reimport takes a long time so we only want to reimport if necessary.
            bool doImport = false;

            // Record the old format so we can restore after changing format.
            string restoreBuildPlatform = GetPlatformString();

            // Get the restore settings
            // First check if there is an override for this platform.

            bool currentHasOverride;
            {
                TextureImporterPlatformSettings platformOverriddenTips = textureImporter.GetPlatformTextureSettings(restoreBuildPlatform);
                currentHasOverride = platformOverriddenTips.overridden;
            }

            // Get the default settings.
            TextureImporterFormat abstractDefaultCurrentFormat;
            TextureFormatInfo_AbstractDefaultPlatform restoreTfi;
            {
                TextureImporterPlatformSettings defaultTips = textureImporter.GetDefaultPlatformTextureSettings();
                abstractDefaultCurrentFormat = defaultTips.format;
                restoreTfi = new TextureFormatInfo_AbstractDefaultPlatform(defaultTips.textureCompression,
                                                                    defaultTips.crunchedCompression,
                                                                    restoreBuildPlatform,
                                                                    defaultTips.format,
                                                                    defaultTips.compressionQuality,
                                                                    textureImporter.textureType == TextureImporterType.NormalMap,
                                                                    currentHasOverride);
            }
            string targetBuildPlatform = toThisFormat.platform;

            // Check if anything needs changing and if so remember that we need to reimport;
            {
                if (targetBuildPlatform != null)
                {
                    if (currentHasOverride != toThisFormat.doPlatformOverride)
                    {
                        // Disable/enable the platform override
                        TextureImporterPlatformSettings platformOverriddenTips = textureImporter.GetPlatformTextureSettings(restoreBuildPlatform);
                        platformOverriddenTips.overridden = toThisFormat.doPlatformOverride;
                        textureImporter.SetPlatformTextureSettings(platformOverriddenTips);
                        doImport = true;
                    }
                }

                if (textureImporter.textureCompression != toThisFormat.abstractDefaultTextureCompression)
                {
                    textureImporter.textureCompression = toThisFormat.abstractDefaultTextureCompression;
                    doImport = true;
                }

                if (textureImporter.crunchedCompression != toThisFormat.abstractDefaultDoCrunchCompression)
                {
                    textureImporter.crunchedCompression = toThisFormat.abstractDefaultDoCrunchCompression;
                    doImport = true;
                }

                if (_ChangeNormalMapTypeIfNecessary(textureImporter, setNormalMap))
                {
                    doImport = true;
                }

                if (abstractDefaultCurrentFormat != toThisFormat.abstractDefaultPlatformFormat)
                {
                    TextureImporterPlatformSettings defTips = textureImporter.GetDefaultPlatformTextureSettings();
                    defTips.format = toThisFormat.abstractDefaultPlatformFormat;
                    textureImporter.SetPlatformTextureSettings(defTips);
                    doImport = true;
                }
            }

            if (doImport)
            {
                string s;
                if (rememberRestoreSettings)
                {
                    s = "Setting DefaultPlatform texture compression for ";
                }
                else
                {
                    s = "Restoring DefaultPlatform texture compression for ";
                }
                s += String.Format("{0}  FROM: compression={1} isNormal{2} format={3} hadOverride={4} TO: compression={5} isNormal={6} format={7} hadOverride={8}",
                                tx, restoreTfi.abstractDefaultTextureCompression, restoreTfi.isNormalMap, restoreTfi.abstractDefaultPlatformFormat, restoreTfi.doPlatformOverride,
                                toThisFormat.abstractDefaultTextureCompression, setNormalMap, toThisFormat.abstractDefaultPlatformFormat, toThisFormat.doPlatformOverride);

                Debug.Log(s);
                if (doImport && rememberRestoreSettings && !_textureFormatMap_DefaultAbstract.ContainsKey(tx))
                {
                    RememberTextureFormatChange(tx, restoreTfi);
                }
            }

            return doImport;
        }

        private bool _SetTextureFormatUnity5(Texture2D tx, TextureFormatInfo_AbstractDefaultPlatform toThisFormat, bool addToList, bool setNormalMap, TextureImporter textureImporter)
        {
            bool doImport = false;

            TextureFormatInfo_AbstractDefaultPlatform restoreTfi = new TextureFormatInfo_AbstractDefaultPlatform(textureImporter.textureCompression,
                                                                false,
                                                                toThisFormat.platform,
                                                                TextureImporterFormat.RGBA32,
                                                                toThisFormat.platformCompressionQuality,
                                                                textureImporter.textureType == TextureImporterType.NormalMap,
                                                                false);

            string platform = toThisFormat.platform;
            if (platform != null)
            {
                TextureImporterPlatformSettings tips = textureImporter.GetPlatformTextureSettings(platform);
                if (tips.overridden)
                {
                    restoreTfi.abstractDefaultPlatformFormat = tips.format;
                    restoreTfi.platformCompressionQuality = tips.compressionQuality;
                    TextureImporterPlatformSettings tipsOverridden = new TextureImporterPlatformSettings();
                    tips.CopyTo(tipsOverridden);
                    tipsOverridden.compressionQuality = toThisFormat.platformCompressionQuality;
                    tipsOverridden.format = toThisFormat.abstractDefaultPlatformFormat;
                    textureImporter.SetPlatformTextureSettings(tipsOverridden);
                    doImport = true;
                }
            }

            if (textureImporter.textureCompression != toThisFormat.abstractDefaultTextureCompression)
            {
                textureImporter.textureCompression = toThisFormat.abstractDefaultTextureCompression;
                doImport = true;
            }

            if (doImport)
            {
                string s;
                if (addToList)
                {
                    s = "Setting texture compression for ";
                }
                else
                {
                    s = "Restoring texture compression for ";
                }
                s += String.Format("{0}  FROM: compression={1} isNormal{2} TO: compression={3} isNormal={4} ", tx, restoreTfi.abstractDefaultTextureCompression, restoreTfi.isNormalMap, toThisFormat.abstractDefaultTextureCompression, setNormalMap);
                if (toThisFormat.platform != null)
                {
                    s += String.Format(" setting platform override format for platform {0} to {1} compressionQuality {2}", toThisFormat.platform, toThisFormat.abstractDefaultPlatformFormat, toThisFormat.platformCompressionQuality);
                }
                Debug.Log(s);
            }
            if (doImport && addToList && !_textureFormatMap_DefaultAbstract.ContainsKey(tx))
            {
                RememberTextureFormatChange(tx, restoreTfi);
            }
            return doImport;
        }

        public void SetNormalMap(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                if (textureImporter.textureType != TextureImporterType.NormalMap)
                {
                    textureImporter.textureType = TextureImporterType.NormalMap;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx));
                }
            }
        }

        public bool IsCompressed(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                if (textureImporter.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    return true;
                }
            }
            return false;
        }
#else
                // 5_4 and earlier
            class TextureFormatInfo
        {
            public TextureImporterFormat format;
            public bool isNormalMap;
            public String platform;
            public TextureImporterFormat platformOverrideFormat;

            public TextureFormatInfo(TextureImporterFormat f, string p, TextureImporterFormat pf, bool isNormMap)
            {
                format = f;
                platform = p;
                platformOverrideFormat = pf;
                isNormalMap = isNormMap;
            }
        }

        public bool IsNormalMap(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                if (((TextureImporter)ai).normalmap) return true;
            }
            return false;
        }

        public void AddTextureFormat(Texture2D tx, bool isNormalMap)
        {
            //pixel values don't copy correctly from one texture to another when isNormal is set so unset it.
            _SetTextureFormat(tx,
                             new TextureFormatInfo(TextureImporterFormat.ARGB32, MBVersionEditor.GetPlatformString(), TextureImporterFormat.AutomaticTruecolor, isNormalMap),
                            true, false);
        }

        void _SetTextureFormat(Texture2D tx, TextureFormatInfo tfi, bool addToList, bool setNormalMap)
        {

            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is UnityEditor.TextureImporter)
            {
                string s;
                if (addToList)
                {
                    s = "Setting texture format for ";
                }
                else {
                    s = "Restoring texture format for ";
                }
                s += tx + " to " + tfi.format;
                if (tfi.platform != null)
                {
                    s += " setting platform override format for " + tfi.platform + " to " + tfi.platformOverrideFormat;
                }
                Debug.Log(s);
                TextureImporter textureImporter = (TextureImporter)ai;
                TextureFormatInfo restoreTfi = new TextureFormatInfo(textureImporter.textureFormat,
                                                                    tfi.platform,
                                                                    TextureImporterFormat.AutomaticTruecolor,
                                                                    textureImporter.normalmap);
                string platform = tfi.platform;
                bool doImport = false;
                if (platform != null)
                {
                    int maxSize;
                    TextureImporterFormat f;
                    textureImporter.GetPlatformTextureSettings(platform, out maxSize, out f);
                    restoreTfi.platformOverrideFormat = f;
                    if (f != 0)
                    { //f == 0 means no override or platform doesn't exist
                        textureImporter.SetPlatformTextureSettings(platform, maxSize, tfi.platformOverrideFormat);
                        doImport = true;
                    }
                }

                if (textureImporter.textureFormat != tfi.format)
                {
                    textureImporter.textureFormat = tfi.format;
                    doImport = true;
                }
                if (textureImporter.normalmap && !setNormalMap)
                {
                    textureImporter.normalmap = false;
                    doImport = true;
                }
                if (!textureImporter.normalmap && setNormalMap)
                {
                    textureImporter.normalmap = true;
                    doImport = true;
                }
                if (addToList && !_textureFormatMap.ContainsKey(tx)) _textureFormatMap.Add(tx, restoreTfi);
                if (doImport) AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx), ImportAssetOptions.ForceUpdate);
            }
        }

        public void SetNormalMap(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                if (!textureImporter.normalmap)
                {
                    textureImporter.normalmap = true;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx));
                }
            }
        }

        public bool IsCompressed(Texture2D tx)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                TextureImporterFormat tf = textureImporter.textureFormat;
                if (tf != TextureImporterFormat.ARGB32)
                {
                    return true;
                }
            }
            return false;
        }
#endif


        public void RestoreReadFlagsAndFormats(ProgressUpdateDelegate progressInfo)
        {
            {
                for (int i = 0; i < _texturesWithReadWriteFlagSet.Count; i++)
                {
                    if (progressInfo != null) progressInfo("Restoring read flag for " + _texturesWithReadWriteFlagSet[i], .9f);
                    SetReadWriteFlag(_texturesWithReadWriteFlagSet[i], false, false);
                }
                _texturesWithReadWriteFlagSet.Clear();
            }

            {
                foreach (Texture2D tex in _textureFormatMap_DefaultAbstract.Keys)
                {
                    if (progressInfo != null) progressInfo("Restoring format for " + tex, .9f);
                    _SetTextureFormat_DefaultPlatform(tex, _textureFormatMap_DefaultAbstract[tex], false, _textureFormatMap_DefaultAbstract[tex].isNormalMap);
                }
                _textureFormatMap_DefaultAbstract.Clear();
            }

            {
                foreach (Texture2D tex in _textureFormatMap_PlatformOverride.Keys)
                {
                    if (progressInfo != null) progressInfo("Restoring format for " + tex, .9f);
                    _SetTextureFormat_PlatformOverride(tex, _textureFormatMap_PlatformOverride[tex], false, _textureFormatMap_PlatformOverride[tex].isNormalMap);
                }
                _textureFormatMap_PlatformOverride.Clear();
            }

        }


        public void SetReadWriteFlag(Texture2D tx, bool isReadable, bool addToList)
        {
            AssetImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx));
            if (ai != null && ai is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter)ai;
                if (textureImporter.isReadable != isReadable)
                {
                    if (addToList) _texturesWithReadWriteFlagSet.Add(tx);
                    textureImporter.isReadable = isReadable;
                    //				Debug.LogWarning("Setting read flag for Texture asset " + AssetDatabase.GetAssetPath(tx) + " to " + isReadable);
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx));
                }
            }
        }

        private bool ConstructFilename(Material resMat, string texPropertyName, string atlasType, string formatString, int atlasNum,
            out string pth,
            out string relativePath)
        {
            string prefabPth = AssetDatabase.GetAssetPath(resMat);
            if (prefabPth == null || prefabPth.Length == 0)
            {
                Debug.LogError("Could save atlas. Could not find result material in AssetDatabase.");
                pth = "";
                relativePath = "";
                return false;
            }
            string baseName = Path.GetFileNameWithoutExtension(prefabPth);
            string folderPath = prefabPth.Substring(0, prefabPth.Length - baseName.Length - 4);
            string fullFolderPath = Application.dataPath + folderPath.Substring("Assets".Length, folderPath.Length - "Assets".Length);
            pth = fullFolderPath + baseName + "-" + texPropertyName + "-" + atlasType + "-" + atlasNum;
            relativePath = folderPath + baseName + "-" + texPropertyName + "-" + atlasType + "-" + formatString + atlasNum;
            return true;
        }

        public void SaveTextureArrayToAssetDatabase(Texture2DArray atlas, TextureFormat format, string texPropertyName, int atlasNum, Material resMat)
        {
            if (atlas == null)
            {
                if (resMat.HasProperty(texPropertyName))
                {
                    resMat.SetTexture(texPropertyName, null);
                }
            }
            else
            {
                string pth, relativePath;
                if (ConstructFilename(resMat, texPropertyName, "texarray-", format.ToString(), atlasNum, out pth, out relativePath))
                {
                    string assetFilename = relativePath + ".asset";
                    Texture2DArray existingAsset = AssetDatabase.LoadAssetAtPath<Texture2DArray>(assetFilename);
                    if (!existingAsset)
                    {
                        AssetDatabase.CreateAsset(atlas, assetFilename);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(atlas, existingAsset);
                    }

                    Debug.Log(String.Format("Wrote Texture2DArray for {0} to file:{1}", texPropertyName, pth));
                    if (resMat.HasProperty(texPropertyName))
                    {
                        Texture2DArray txx = (Texture2DArray)(AssetDatabase.LoadAssetAtPath(assetFilename, typeof(Texture2DArray)));
                        resMat.SetTexture(texPropertyName, txx);
                    }
                }
            }
        }

        /**
         pass in System.IO.File.WriteAllBytes for parameter fileSaveFunction. This is necessary because on Web Player file saving
         functions only exist for Editor classes
         */
        public void SaveAtlasToAssetDatabase(Texture2D atlas, ShaderTextureProperty texPropertyName, int atlasNum, bool doAnySrcMatsHaveProperty, Material resMat)
        {
            if (atlas == null)
            {
                if (doAnySrcMatsHaveProperty)
                { 
                    SetMaterialTextureProperty(resMat, texPropertyName, null);
                }
            }
            else
            {
                string pth, relativePath;
                if (ConstructFilename(resMat, texPropertyName.name, "atlas", "", atlasNum, out pth, out relativePath))
                {
                    //need to create a copy because sometimes the packed atlases are not in ARGB32 format
                    Texture2D newTex = MB_Utility.createTextureCopy(atlas);
                    int size = Mathf.Max(newTex.height, newTex.width);
                    if (SAVE_FORMAT == saveTextureFormat.png)
                    {
                        pth += ".png";
                        relativePath += ".png";
                        byte[] bytes = newTex.EncodeToPNG();
                        System.IO.File.WriteAllBytes(pth, bytes);
                    }
                    else
                    {
                        pth += ".tga";
                        relativePath += ".tga";
                        if (File.Exists(pth))
                        {
                            File.Delete(pth);
                        }

                        //Create the file.
                        FileStream fs = File.Create(pth);
                        MB_TGAWriter.Write(newTex.GetPixels(), newTex.width, newTex.height, fs);
                    }
                    Editor.DestroyImmediate(newTex);
                    AssetDatabase.Refresh();
                    Debug.Log(String.Format("Wrote atlas for {0} to file:{1}", texPropertyName.name, pth));
                    Texture2D txx = (Texture2D)(AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D)));
                    SetTextureSize(txx, size);
                    if (doAnySrcMatsHaveProperty)
                    {
                        SetMaterialTextureProperty(resMat, texPropertyName, relativePath);
                    }
                }
            }
        }

        public void SetMaterialTextureProperty(Material target, ShaderTextureProperty texPropName, string texturePath)
        {
            //			if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.Log(MB2_LogLevel.debug,"Assigning atlas " + texturePath + " to result material " + target + " for property " + texPropName,LOG_LEVEL);
            if (texPropName.isNormalMap)
            {
                SetNormalMap((Texture2D)(AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D))));
            }
            if (target.HasProperty(texPropName.name))
            {
                target.SetTexture(texPropName.name, (Texture2D)(AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D))));
            }
        }

        public void SetTextureSize(Texture2D tx, int size)
        {
            TextureImporter ai = AssetImporter.GetAtPath(AssetDatabase.GetAssetOrScenePath(tx)) as TextureImporter;
            if (ai == null) return;

            int maxSize = 32;
            if (size > 32) maxSize = 64;
            if (size > 64) maxSize = 128;
            if (size > 128) maxSize = 256;
            if (size > 256) maxSize = 512;
            if (size > 512) maxSize = 1024;
            if (size > 1024) maxSize = 2048;
            if (size > 2048) maxSize = 4096;

            bool isSettingsChanged = false;
            if (ai.maxTextureSize != maxSize)
            {
                ai.maxTextureSize = maxSize;
                isSettingsChanged = true;
            }

#if UNITY_5_5_OR_NEWER
            string[] platforms = { "Standalone", "Web", "iPhone", "Android", "WebGL", "Windows Store Apps", "PSP2", "PS4", "XboxOne", "Nintendo 3DS", "tvOS" };
            foreach (string platform in platforms)
            {
                TextureImporterPlatformSettings settings = ai.GetPlatformTextureSettings(platform);
                if (settings != null && settings.overridden && settings.maxTextureSize != maxSize)
                {
                    settings.maxTextureSize = maxSize;
                    ai.SetPlatformTextureSettings(settings);
                    isSettingsChanged = true;
                }
            }

            // reimport if anything changed
            if (isSettingsChanged)
            {
                ai.SaveAndReimport();
            }
#else
            if (ai.maxTextureSize != maxSize)
            {
                ai.maxTextureSize = maxSize;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetOrScenePath(tx), ImportAssetOptions.ForceUpdate);
            }
#endif
        }

        public void CommitChangesToAssets()
        {
            AssetDatabase.Refresh();
        }

        public string GetPlatformString()
        {
            return MBVersionEditor.GetPlatformString();
        }

        public void CheckBuildSettings(long estimatedArea)
        {
            if (Math.Sqrt(estimatedArea) > 1000f)
            {
                if (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone)
                {
                    Debug.LogWarning("If the current selected build target is not standalone then the generated atlases may be capped at size 1024. If build target is Standalone then atlases of 4096 can be built");
                }
            }
        }

        public bool CheckPrefabTypes(MB_ObjsToCombineTypes objToCombineType, List<GameObject> objsToMesh)
        {
            for (int i = 0; i < objsToMesh.Count; i++)
            {
                MB_PrefabType pt = MBVersionEditor.PrefabUtility_GetPrefabType(objsToMesh[i]);
                if (pt == MB_PrefabType.scenePefabInstance || pt == MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab)
                {
                    // these are scene objects
                    if (objToCombineType == MB_ObjsToCombineTypes.prefabOnly)
                    {
                        Debug.LogWarning("The list of objects to combine contains scene objects. You probably want prefabs. If using scene objects ensure position is zero, rotation is zero and scale is one. Translation, Rotation and Scale will be baked into the generated mesh." + objsToMesh[i] + " is a scene object");
                        return false;
                    }
                }
                else if (objToCombineType == MB_ObjsToCombineTypes.sceneObjOnly)
                {
                    //these are prefabs
                    if (pt == MB_PrefabType.modelPrefabAsset || pt == MB_PrefabType.prefabAsset)
                    {
                        Debug.LogError("The list of objects to combine contains prefab assets. You need scene instances." + objsToMesh[i] + "(position " + i + ") is a project prefab object. Create a scene instance of this prefab and use that in the list of objects to combine.");
                        return false;
                    }
                }
            }
            return true;
        }

        public bool ValidateSkinnedMeshes(List<GameObject> objs)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                Renderer r = MB_Utility.GetRenderer(objs[i]);
                if (r is SkinnedMeshRenderer)
                {
                    Mesh m = MB_Utility.GetMesh(objs[i]);
                    if (m != null)
                    {
                        Matrix4x4[] bindposes = m.bindposes;
                        if (bindposes.Length > 0)
                        {
                            // There should be a 1-to-1 match between bindposes and bones;
                            Transform[] bones = ((SkinnedMeshRenderer)r).bones;
                            if (bones.Length == 0)
                            {
                                Debug.LogWarning("SkinnedMesh " + i + " (" + objs[i] + ") in the list of objects to combine has no bones. Check that 'optimize game object' is not checked in the 'Rig' tab of the asset importer. Mesh Baker cannot combine optimized skinned meshes because the bones are not available.");
                            }
                            //					UnityEngine.Object parentObject = EditorUtility.GetPrefabParent(r.gameObject);
                            //					string path = AssetDatabase.GetAssetPath(parentObject);
                            //					Debug.Log (path);
                            //					AssetImporter ai = AssetImporter.GetAtPath( path );
                            //					Debug.Log ("bbb " + ai);
                            //					if (ai != null && ai is ModelImporter){
                            //						Debug.Log ("valing 2");
                            //						ModelImporter modelImporter = (ModelImporter) ai;
                            //						if(modelImporter.optimizeMesh){
                            //							Debug.LogError("SkinnedMesh " + i + " (" + objs[i] + ") in the list of objects to combine is optimized. Mesh Baker cannot combine optimized skinned meshes because the bones are not available.");
                            //						}
                            //					}
                        } else
                        {
                            // This was a skinned mesh with no bindposes (bones). This is allowed if there are blendshapes
                            if (m.blendShapeCount == 0)
                            {
                                Debug.LogWarning("SkinnedMesh " + i + " (" + objs[i] + ") in the list of objects to combine has no bindposes or blendshapes. Should this renderer be a MeshRenderer?");
                            }
                        }
                    }
                }
            }
            return true;
        }

        public void Destroy(UnityEngine.Object o)
        {
            if (Application.isPlaying)
            {
                if (!IsAnAsset(o))// This is an asset. Don't destroy it.
                {
                    MonoBehaviour.Destroy(o);
                }
            }
            else
            {
                if (!IsAnAsset(o)) // don't try to destroy assets
                {
                    MonoBehaviour.DestroyImmediate(o, false);
                }
            }
        }

        public void DestroyAsset(UnityEngine.Object o)
        {
            if (o == null) return;
            string path = AssetDatabase.GetAssetPath(o);
            if (path != null && path != "")
            {
                AssetDatabase.DeleteAsset(path);
            } else
            {
                Debug.LogError("DestroyAsset was called on an object that was not an asset: " + o);
            }
        }

        public static object[] DropZone(string title, int w, int h)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box(title, GUILayout.Width(w), GUILayout.Height(h));
            Rect dropRect = GUILayoutUtility.GetLastRect();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EventType eventType = Event.current.type;
            bool isAccepted = false;

            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                if (dropRect.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (eventType == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        isAccepted = true;
                        //Debug.Log("Consuming drop event in inspector. " + Event.current.mousePosition + " rect" + dropRect);
                        Event.current.Use();
                    }
                }
            }

            return isAccepted ? DragAndDrop.objectReferences : null;
        }

        public static void AddDroppedObjects(object[] objs, MB3_MeshBakerRoot momm)
        {
            if (objs != null)
            {
                HashSet<Renderer> renderersToAdd = new HashSet<Renderer>();
                for (int i = 0; i < objs.Length; i++)
                {
                    object obj = objs[i];
                    if (obj is GameObject)
                    {
                        Renderer[] rs = ((GameObject)obj).GetComponentsInChildren<Renderer>(true);
                        for (int j = 0; j < rs.Length; j++)
                        {
                            if (rs[j] is MeshRenderer || rs[j] is SkinnedMeshRenderer)
                            {
                                renderersToAdd.Add(rs[j]);
                            }
                        }
                    }
                }

                int numAdded = 0;
                List<GameObject> objsToCombine = momm.GetObjectsToCombine();
                bool failedToAddAssets = false;
                foreach (Renderer r in renderersToAdd)
                {
                    if (!objsToCombine.Contains(r.gameObject))
                    {
                        MB_PrefabType prefabType = MBVersionEditor.PrefabUtility_GetPrefabType(r.gameObject);
                        if (prefabType == MB_PrefabType.modelPrefabAsset || prefabType == MB_PrefabType.prefabAsset)
                        {
                            failedToAddAssets = true;
                        }
                        else
                        {
                            objsToCombine.Add(r.gameObject);
                            numAdded++;
                        }
                    }
                }

                if (failedToAddAssets)
                {
                    Debug.LogError("Did not add some object(s) because they are not scene objects");
                }
                Debug.Log("Added " + numAdded + " renderers");
            }
        }

        public bool IsAnAsset(UnityEngine.Object o)
        {
            string path = AssetDatabase.GetAssetPath(o);
            bool isAsset = !(path == null || path.Length == 0);
            return isAsset;
        }

        public Texture2D CreateTemporaryAssetCopy(ShaderTextureProperty shaderProp, Texture2D sliceTex, int w, int h, TextureFormat format, MB2_LogLevel logLevel)
        {
            bool foundMatch;
            UnityEditor.TextureImporterFormat targetImporterFormat = Map_TextureFormat_2_TextureImporterFormat(format, out foundMatch);
            if (!foundMatch)
            {
                Debug.LogError("Could not find target importer format matching " + format);
                return null;
            }

            // Can't do a pixel copy with normal maps because unity has swizzled the color channels. Turn of the TextureType:Normal first.
            if (shaderProp.isNormalMap)
            {
                if (!_textureFormatMap_DefaultAbstract.ContainsKey(sliceTex) 
                    && !_textureFormatMap_PlatformOverride.ContainsKey(sliceTex))
                {
                    ConvertTextureFormat_DefaultPlatform(sliceTex, TextureFormat.RGBA32, isNormalMap:false);
                }
            }

            string workingFolder = MB_EditorUtil.GetShortPathToWorkingDirectoryAndEnsureItExists();
            string tryPth = workingFolder + "/" + sliceTex.name + "_TEMP.png";
            string shortPath = AssetDatabase.GenerateUniqueAssetPath(tryPth);
            string fullPath = MB_Utility.ConvertAssetsRelativePathToFullSystemPath(shortPath);
            // Duplicate the source texture and save it as a truecolor temporary asset
            {
                Texture2D newTex1 = new Texture2D(sliceTex.width, sliceTex.height, TextureFormat.ARGB32, true);
                newTex1.SetPixels(sliceTex.GetPixels());
                newTex1.Apply();

                // Resize it.
                Texture2D newTex = MB_Utility.resampleTexture(newTex1, w, h);
                System.IO.File.WriteAllBytes(fullPath, newTex.EncodeToPNG());
                GameObject.DestroyImmediate(newTex);
                GameObject.DestroyImmediate(newTex1);
                AssetDatabase.Refresh();
            }

            Texture2D temporaryTex = AssetDatabase.LoadAssetAtPath<Texture2D>(shortPath);
            TextureImporter ai = (TextureImporter)AssetImporter.GetAtPath(shortPath);
            ai.isReadable = true;
            string platformString = GetPlatformString();
            TextureImporterPlatformSettings settings = ai.GetPlatformTextureSettings(platformString);

            // Note that it is not enough to set the default platform settings. Default only uses abstract formats, true formats are auto generated settings.
            // Need to use the plaftorm override to set the true setting.
            if (settings.format != targetImporterFormat)
            {
                settings.overridden = true;
                settings.format = targetImporterFormat;
                ai.SetPlatformTextureSettings(settings);
            }

            if (logLevel >= MB2_LogLevel.debug) Debug.LogFormat("Creating temporary texuture asset to resize texture: {0} w:{1} h:{2} format:{3} TO w:{4} h:{5} format:{6}",
                sliceTex, sliceTex.width, sliceTex.height, sliceTex.format, w, h, format);
            ai.SaveAndReimport();
            settings = ai.GetPlatformTextureSettings(platformString);
            Debug.Assert(settings.format == targetImporterFormat, "Format of temporary texture after import was " + settings.format + " not targetFormat: " + targetImporterFormat);
            return temporaryTex;
        }

        public static TextureImporterFormat Map_TextureFormat_2_TextureImporterFormat(TextureFormat texFormat, out bool success)
        {
            return MBVersionEditor.Map_TextureFormat_2_TextureImporterFormat(texFormat, out success); 
        }

        public bool TextureImporterFormatExistsForTextureFormat(TextureFormat texFormat)
        {
            bool success;
            Map_TextureFormat_2_TextureImporterFormat(texFormat, out success);
            return success;
        }

        public bool ConvertTexture2DArray(Texture2DArray inArray, Texture2DArray outArray, TextureFormat outFormat)
        {
            bool foundFormat;
            TextureImporterFormat outImporterFormat = Map_TextureFormat_2_TextureImporterFormat(outFormat, out foundFormat);
            if (!foundFormat)
            {
                Debug.LogError("Could not find a TextureImporterFormat matching format: " + outFormat);
                return false;
            }

            Texture2D tempTex = new Texture2D(inArray.width, inArray.height, inArray.format, true);

            // Create a temporary texture asset of the correct size. We need this for the TextureImporter
            string shortPath, fullPath;
            {
                shortPath = MB_EditorUtil.GetShortPathToWorkingDirectoryAndEnsureItExists();
                shortPath += inArray.name + "_" + outFormat.ToString() + "_TEMP.png";
                shortPath = AssetDatabase.GenerateUniqueAssetPath(shortPath);
                fullPath = MB_Utility.ConvertAssetsRelativePathToFullSystemPath(shortPath);
                Debug.Log("Saving temp tex: " + fullPath + " shortPth " + shortPath);
                byte[] bytes = tempTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.Refresh();
            }

            // This is horrible, but the only way to convert textures to compressed formats it through the asset importer. We need an asset!
            TextureImporter ai = (TextureImporter)AssetImporter.GetAtPath(shortPath);
            {
                ai.isReadable = true;
                TextureImporterPlatformSettings tips = new TextureImporterPlatformSettings();
                tips.format = outImporterFormat;
                //tips.textureCompression = TextureImporterCompression.Uncompressed;
                ai.SetPlatformTextureSettings(tips);
                ai.SaveAndReimport();
            }

            for (int sliceIdx = 0; sliceIdx < inArray.depth; sliceIdx++)
            {
                Graphics.CopyTexture(inArray, sliceIdx, 0, tempTex, sliceIdx, 0);
                byte[] bytes = tempTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.Refresh();
                Texture2D srcTexConverted = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(shortPath);
                int numMips = srcTexConverted.mipmapCount;
                for (int mipIdx = 0; mipIdx < numMips; mipIdx++)
                {
                    Graphics.CopyTexture(srcTexConverted, 0, mipIdx, outArray, sliceIdx, mipIdx);
                }
            }

            MonoBehaviour.DestroyImmediate(tempTex);
            AssetDatabase.DeleteAsset(shortPath);
            return true;
        }

        public void GetMaterialPrimaryKeysIfAddressables(MB2_TextureBakeResults textureBakeResults)
        {
            // This currently does nothing. This appoach will work if there is an Editor time Addressables
            // API that can retrieve the addressables key for each baked material. This API does not exist.
            /*
            for (int i = 0; i < textureBakeResults.materialsAndUVRects.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(textureBakeResults.materialsAndUVRects[i].material);
                if (path == null) path = "";
                textureBakeResults.materialsAndUVRects[i].matAddressablesPKey = path;
            }
            */
        }
    }
}