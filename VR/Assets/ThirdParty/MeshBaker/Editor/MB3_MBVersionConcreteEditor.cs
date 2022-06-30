/**
 *	\brief Hax!  DLLs cannot interpret preprocessor directives, so this class acts as a "bridge"
 */
using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class MBVersionEditorConcrete : MBVersionEditorInterface
    {

        /// <summary>
        /// Used to map the activeBuildTarget to a string argument needed by TextureImporter.GetPlatformTextureSettings
        /// The allowed values for GetPlatformTextureSettings are "Web", "Standalone", "iPhone", "Android" and "FlashPlayer".
        /// </summary>
        /// <returns></returns>
        public string GetPlatformString()
        {
#if (UNITY_4_6 || UNITY_4_7 || UNITY_4_5 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5)
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone){
                return "iPhone";	
            }
#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                return "iPhone";
            }
#endif
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WSAPlayer)
            {
                return "Windows Store Apps";
            }
#if (!UNITY_2018_3_OR_NEWER)
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PSP2)
            {
                return "PSP2";
            }
#endif
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS4)
            {
                return "PS4";
            }
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.XboxOne)
            {
                return "XboxOne";
            }
#if (UNITY_2017_3_OR_NEWER)
#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.SamsungTV)
            {
                return "Samsung TV";
            }
#endif
#if (UNITY_5_5_OR_NEWER && !UNITY_2018_1_OR_NEWER)
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.N3DS)
            {
                return "Nintendo 3DS";
            }
#endif
#if (UNITY_5_3 || UNITY_5_2 || UNITY_5_3_OR_NEWER)
#if (UNITY_2018_1_OR_NEWER)
            // wiiu support was removed in 2018.1
#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WiiU)
            {
                return "WiiU";
            }
#endif
#endif
#if (UNITY_5_3 || UNITY_5_3_OR_NEWER)
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.tvOS)
            {
                return "tvOS";
            }
#endif
#if (UNITY_2018_2_OR_NEWER)

#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Tizen)
            {
                return "Tizen";
            }
#endif
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                return "Android";
            }

            bool isLinuxStandalone = false;
#if UNITY_2019_2_OR_NEWER
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64)
            {
                isLinuxStandalone = true;
            }
#else
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux || 
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinuxUniversal)
            {
                isLinuxStandalone = true;
            }
#endif
            if (isLinuxStandalone ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 ||
#if UNITY_2017_3_OR_NEWER
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX
#else
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSXIntel ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSXIntel64 ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSXUniversal
#endif
                )
            {
                return "Standalone";
            }
#if !UNITY_5_4_OR_NEWER
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebPlayer ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebPlayerStreamed
                )
            {
                return "Web";
            }
#endif
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                return "WebGL";
            }
            return null;
        }

        public void RegisterUndo(UnityEngine.Object o, string s)
        {
#if (UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5)
            Undo.RegisterUndo(o, s);
#else
            Undo.RecordObject(o, s);
#endif
        }

        public void SetInspectorLabelWidth(float width)
        {
#if (UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5)
            EditorGUIUtility.LookLikeControls(width);
#else
            EditorGUIUtility.labelWidth = width;
#endif
        }

        public void UpdateIfDirtyOrScript(SerializedObject so)
        {
#if UNITY_5_6_OR_NEWER
            so.UpdateIfRequiredOrScript();
#else
            so.UpdateIfDirtyOrScript();
#endif
        }

        public UnityEngine.Object PrefabUtility_GetCorrespondingObjectFromSource(GameObject go)
        {
#if UNITY_2018_2_OR_NEWER
            return PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
            return PrefabUtility.GetPrefabParent(go);
#endif
        }

        public bool IsAutoPVRTC(TextureImporterFormat platformFormat, TextureImporterFormat platformDefaultFormat)
        {
            if ((
#if UNITY_2017_1_OR_NEWER
                    platformFormat == TextureImporterFormat.Automatic
#elif UNITY_5_5_OR_NEWER
                    platformFormat == TextureImporterFormat.Automatic ||
                    platformFormat == TextureImporterFormat.Automatic16bit ||
                    platformFormat == TextureImporterFormat.AutomaticCompressed ||
                    platformFormat == TextureImporterFormat.AutomaticCompressedHDR ||
                    platformFormat == TextureImporterFormat.AutomaticCrunched ||
                    platformFormat == TextureImporterFormat.AutomaticHDR
#else
                    platformFormat == TextureImporterFormat.Automatic16bit ||
                    platformFormat == TextureImporterFormat.AutomaticCompressed ||
                    platformFormat == TextureImporterFormat.AutomaticCrunched
#endif
                ) && (
                    platformDefaultFormat == TextureImporterFormat.PVRTC_RGB2 ||
                    platformDefaultFormat == TextureImporterFormat.PVRTC_RGB4 ||
                    platformDefaultFormat == TextureImporterFormat.PVRTC_RGBA2 ||
                    platformDefaultFormat == TextureImporterFormat.PVRTC_RGBA4
                ))
            {
                return true;
            }
            return false;
        }

        public MB_PrefabType PrefabUtility_GetPrefabType(UnityEngine.Object obj)
        {
#if UNITY_2018_3_OR_NEWER
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(obj))
            {
                return MB_PrefabType.scenePefabInstance;
            }

            if (!PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                return MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab;
            }

            PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(obj);
            if (assetType == PrefabAssetType.NotAPrefab)
            {
                if (PrefabUtility.GetPrefabInstanceStatus(obj) != PrefabInstanceStatus.NotAPrefab)
                {
                    return MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab;
                }
                else
                {
                    return MB_PrefabType.scenePefabInstance;
                }
            }
            else if (assetType == PrefabAssetType.Model)
            {
                return MB_PrefabType.modelPrefabAsset;
            }
            else if (assetType == PrefabAssetType.Regular ||
                     assetType == PrefabAssetType.Variant ||
                     assetType == PrefabAssetType.MissingAsset)
            {
                return MB_PrefabType.prefabAsset;
            }
            else
            {
                Debug.Assert(false, "Should never get here. Unknown prefab asset type.");
                return MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab;
            }
#else
            PrefabType prefabType = PrefabUtility.GetPrefabType(obj);
            if (prefabType == PrefabType.ModelPrefab)
            {
                return MB_PrefabType.modelPrefabAsset;
            } else if (prefabType == PrefabType.Prefab)
            {
                return MB_PrefabType.prefabAsset;
            } else if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.ModelPrefabInstance)
            {
                return MB_PrefabType.scenePefabInstance;
            } else
            {
                return MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab;
            }
#endif
        }

        public void PrefabUtility_UnpackPrefabInstance(UnityEngine.GameObject go, ref SerializedObject so)
        {
#if UNITY_2018_3_OR_NEWER
            UnityEngine.Object targetObj = null;
            if (so != null) targetObj = so.targetObject;
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            // This is a workaround for a nasty Unity bug. The call to UnpackPrefabInstance
            // corrupts the serialized object, Recreate a clean reference here.
            if (so != null) so = new SerializedObject(targetObj);
#else
            // Do nothing.
#endif
        }

        public void PrefabUtility_ReplacePrefab(GameObject gameObject, string assetPath, MB_ReplacePrefabOption replacePrefabOptions)
        {
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, assetPath, InteractionMode.AutomatedAction);
#else
            GameObject obj = (GameObject) AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            PrefabUtility.ReplacePrefab(gameObject, obj, (ReplacePrefabOptions) replacePrefabOptions);
            obj = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            if (replacePrefabOptions == MB_ReplacePrefabOption.nameBased)
            {
                 PrefabUtility.ConnectGameObjectToPrefab(gameObject, obj);
            }
#endif
        }

        public GameObject PrefabUtility_GetPrefabInstanceRoot(GameObject sceneInstance)
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetOutermostPrefabInstanceRoot(sceneInstance);
#else
            return PrefabUtility.FindRootGameObjectWithSameParentPrefab(sceneInstance);
#endif
        }

        public TextureImporterFormat Map_TextureFormat_2_TextureImporterFormat(TextureFormat texFormat, out bool success)
        {
            TextureImporterFormat texImporterFormat;
            success = true;
            switch (texFormat)
            {
                case TextureFormat.ARGB32:
                    texImporterFormat = TextureImporterFormat.ARGB32;
                    break;
                case TextureFormat.RGBA32:
                    texImporterFormat = TextureImporterFormat.RGBA32;
                    break;
                case TextureFormat.RGB24:
                    texImporterFormat = TextureImporterFormat.RGB24;
                    break;
                case TextureFormat.Alpha8:
                    texImporterFormat = TextureImporterFormat.Alpha8;
                    break;

#if UNITY_2020_1_OR_NEWER
                case TextureFormat.ASTC_10x10:
                    texImporterFormat = TextureImporterFormat.ASTC_10x10;
                    break;
                case TextureFormat.ASTC_12x12:
                    texImporterFormat = TextureImporterFormat.ASTC_12x12;
                    break;
                case TextureFormat.ASTC_4x4:
                    texImporterFormat = TextureImporterFormat.ASTC_4x4;
                    break;
                case TextureFormat.ASTC_5x5:
                    texImporterFormat = TextureImporterFormat.ASTC_5x5;
                    break;
                case TextureFormat.ASTC_6x6:
                    texImporterFormat = TextureImporterFormat.ASTC_6x6;
                    break;
                case TextureFormat.ASTC_8x8:
                    texImporterFormat = TextureImporterFormat.ASTC_8x8;
                    break;
#else
                case TextureFormat.ASTC_RGBA_10x10:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_10x10;
                    break;
                case TextureFormat.ASTC_RGBA_12x12:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_12x12;
                    break;
                case TextureFormat.ASTC_RGBA_4x4:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_4x4;
                    break;
                case TextureFormat.ASTC_RGBA_5x5:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_5x5;
                    break;
                case TextureFormat.ASTC_RGBA_6x6:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_6x6;
                    break;
                case TextureFormat.ASTC_RGBA_8x8:
                    texImporterFormat = TextureImporterFormat.ASTC_RGBA_8x8;
                    break;

                case TextureFormat.ASTC_RGB_10x10:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_10x10;
                    break;
                case TextureFormat.ASTC_RGB_12x12:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_12x12;
                    break;
                case TextureFormat.ASTC_RGB_4x4:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_4x4;
                    break;
                case TextureFormat.ASTC_RGB_5x5:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_5x5;
                    break;
                case TextureFormat.ASTC_RGB_6x6:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_6x6;
                    break;
                case TextureFormat.ASTC_RGB_8x8:
                    texImporterFormat = TextureImporterFormat.ASTC_RGB_8x8;
                    break;
#endif
                case TextureFormat.BC4:
                    texImporterFormat = TextureImporterFormat.BC4;
                    break;
                case TextureFormat.BC5:
                    texImporterFormat = TextureImporterFormat.BC5;
                    break;
                case TextureFormat.BC6H:
                    texImporterFormat = TextureImporterFormat.BC6H;
                    break;
                case TextureFormat.BC7:
                    texImporterFormat = TextureImporterFormat.BC7;
                    break;

                case TextureFormat.DXT1:
                    texImporterFormat = TextureImporterFormat.DXT1;
                    break;
                case TextureFormat.DXT1Crunched:
                    texImporterFormat = TextureImporterFormat.DXT1Crunched;
                    break;
                case TextureFormat.DXT5:
                    texImporterFormat = TextureImporterFormat.DXT5;
                    break;
                case TextureFormat.DXT5Crunched:
                    texImporterFormat = TextureImporterFormat.DXT5Crunched;
                    break;

                case TextureFormat.EAC_R:
                    texImporterFormat = TextureImporterFormat.EAC_R;
                    break;
                case TextureFormat.EAC_RG:
                    texImporterFormat = TextureImporterFormat.EAC_RG;
                    break;
                case TextureFormat.EAC_RG_SIGNED:
                    texImporterFormat = TextureImporterFormat.EAC_RG_SIGNED;
                    break;
                case TextureFormat.EAC_R_SIGNED:
                    texImporterFormat = TextureImporterFormat.EAC_R_SIGNED;
                    break;

                case TextureFormat.ETC_RGB4:
                    texImporterFormat = TextureImporterFormat.ETC_RGB4;
                    break;
#if UNITY_2017_3_OR_NEWER
                case TextureFormat.ETC_RGB4Crunched:
                    texImporterFormat = TextureImporterFormat.ETC_RGB4Crunched;
                    break;
#endif
                case TextureFormat.ETC2_RGB:
                    texImporterFormat = TextureImporterFormat.ETC2_RGB4;
                    break;
                case TextureFormat.ETC2_RGBA8:
                    texImporterFormat = TextureImporterFormat.ETC2_RGBA8;
                    break;
#if UNITY_2017_3_OR_NEWER
                case TextureFormat.ETC2_RGBA8Crunched:
                    texImporterFormat = TextureImporterFormat.ETC2_RGBA8Crunched;
                    break;
#endif
                case TextureFormat.PVRTC_RGB2:
                    texImporterFormat = TextureImporterFormat.PVRTC_RGB2;
                    break;
                case TextureFormat.PVRTC_RGB4:
                    texImporterFormat = TextureImporterFormat.PVRTC_RGB4;
                    break;
                case TextureFormat.PVRTC_RGBA2:
                    texImporterFormat = TextureImporterFormat.PVRTC_RGBA2;
                    break;
                case TextureFormat.PVRTC_RGBA4:
                    texImporterFormat = TextureImporterFormat.PVRTC_RGBA4;
                    break;
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.R16:
                    texImporterFormat = TextureImporterFormat.R16;
                    break;
#endif
#if UNITY_2018_2_OR_NEWER
                case TextureFormat.R8:
                    texImporterFormat = TextureImporterFormat.R8;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RFloat:
                    texImporterFormat = TextureImporterFormat.RFloat;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RG16:
                    texImporterFormat = TextureImporterFormat.RG16;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RGB9e5Float:
                    texImporterFormat = TextureImporterFormat.RGB9E5;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RGHalf:
                    texImporterFormat = TextureImporterFormat.RGHalf;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RGFloat:
                    texImporterFormat = TextureImporterFormat.RGFloat;
                    break;
#endif
#if UNITY_2018_3_OR_NEWER
                case TextureFormat.RHalf:
                    texImporterFormat = TextureImporterFormat.RHalf;
                    break;
#endif
                default:
                    texImporterFormat = TextureImporterFormat.ARGB32;
                    success = false;
                    Debug.LogError("No mapping for TextureFormat: " + texFormat + " to a TextureImporterFormat. ");
                    break;
            }

            return texImporterFormat;
        }

        public void PrefabUtility_SavePrefabAsset(GameObject prefabAsset)
        {
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SavePrefabAsset(prefabAsset);
#endif
        }

        public void PrefabUtility_ApplyPrefabInstance(GameObject instance)
        {
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
#else
            PropertyModification[] mods = PrefabUtility.GetPropertyModifications(instance);
            PrefabUtility.SetPropertyModifications(instance, mods);
#endif
        }

        public GameObject PrefabUtility_CreatePrefab(string path, GameObject go)
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.SaveAsPrefabAsset(go, path);
#else
            return PrefabUtility.CreatePrefab(path, go);
#endif
        }

        public GameObject PrefabUtility_FindPrefabRoot(GameObject sourceObj)
        {
#if UNITY_2018_3_OR_NEWER
            if (sourceObj == null) return null;
            if (MB_Utility.IsSceneInstance(sourceObj))
            {
                return PrefabUtility.GetOutermostPrefabInstanceRoot(sourceObj);
            } else
            {
                return sourceObj.transform.root.gameObject;
            }
#else
            return PrefabUtility.FindPrefabRoot(sourceObj);
#endif
        }

        public GameObject GetCorrespondingObjectFromSource(GameObject sourceObj)
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetCorrespondingObjectFromSource(sourceObj);
#else
            return (GameObject) PrefabUtility.GetPrefabParent(sourceObj);
#endif
        }
    }
}