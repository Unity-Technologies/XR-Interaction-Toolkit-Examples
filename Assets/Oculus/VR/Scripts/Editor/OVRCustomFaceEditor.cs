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

using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using Component = UnityEngine.Component;

/// <summary>
/// Custom Editor for <see cref="OVRCustomFace">
/// </summary>
/// <remarks>
/// Custom Editor for <see cref="OVRCustomFace"> that supports:
/// - attempting to find an <see cref="OVRFaceExpressions"/>in the parent hierarchy to match to if none is chosen
/// - supporting string matching to attempt to automatically match <see cref="OVRFaceExpressions.FaceExpression"/> to blend shapes on the shared mesh
/// The string matching algorithm will tokenize the blend shape names and the <see cref="OVRFaceExpressions.FaceExpression"/> names and for each
/// blend shape find the <see cref="OVRFaceExpressions.FaceExpression"/> with the most total characters in matching tokens.
/// To match tokens it currently uses case invariant matching.
/// The tokenization is based on some common string seperation characters and seperation by camel case.
/// </remarks>
[CustomEditor(typeof(OVRCustomFace))]
public class OVRCustomFaceEditor : Editor
{
    private SerializedProperty _expressionsProp;
    private SerializedProperty _mappings;
    private SerializedProperty _strengthMultiplier;
    private SerializedProperty _allowDuplicateMapping;
    private bool _showBlendshapes = true;

    protected virtual void OnEnable()
    {
        _expressionsProp = serializedObject.FindProperty(nameof(OVRCustomFace._faceExpressions));
        _mappings = serializedObject.FindProperty(nameof(OVRCustomFace._mappings));
        _strengthMultiplier = serializedObject.FindProperty(nameof(OVRCustomFace._blendShapeStrengthMultiplier));
        _allowDuplicateMapping = serializedObject.FindProperty(nameof(OVRCustomFace._allowDuplicateMapping));
    }

    public override void OnInspectorGUI()
    {
        var face = (OVRCustomFace)target;

        serializedObject.Update();

        if (_expressionsProp.objectReferenceValue == null)
        {
            _expressionsProp.objectReferenceValue = face.SearchFaceExpressions();
        }

        if (!IsFaceExpressionsConfigured(face))
        {
            if (OVREditorUIElements.RenderWarningWithButton(
                    "OVRFaceExpressions is required.", "Configure OVRFaceExpressions"))
            {
                FixFaceExpressions(face);
            }
        }

        EditorGUILayout.PropertyField(_expressionsProp, new GUIContent(nameof(OVRFaceExpressions)));

        EditorGUILayout.PropertyField(_strengthMultiplier, new GUIContent("Blend Shape Strength Multiplier"));


        //need to pass out some property to find the component from
        SkinnedMeshRenderer renderer = GetSkinnedMeshRenderer(_expressionsProp);

        if (renderer == null || renderer.sharedMesh == null)
        {
            if (_mappings.arraySize > 0)
            {
                _mappings.ClearArray();
            }

            serializedObject.ApplyModifiedProperties();
            return;
        }

        if (_mappings.arraySize != renderer.sharedMesh.blendShapeCount)
        {
            _mappings.ClearArray();
            _mappings.arraySize = renderer.sharedMesh.blendShapeCount;
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
            {
                _mappings.GetArrayElementAtIndex(i).intValue = (int)OVRFaceExpressions.FaceExpression.Invalid;
            }
        }

        EditorGUILayout.Space();

        var enumValues = Enum.GetNames(typeof(OVRCustomFace.RetargetingType));
        face.retargetingType = (OVRCustomFace.RetargetingType)
            EditorGUILayout.Popup("Custom face structure", (int)face.retargetingType, enumValues);

        if (face.retargetingType == OVRCustomFace.RetargetingType.OculusFace
            || face.retargetingType == OVRCustomFace.RetargetingType.Custom
           )
        {
            _showBlendshapes = EditorGUILayout.BeginFoldoutHeaderGroup(_showBlendshapes, "Blendshapes");
            if (_showBlendshapes)
            {
                if (GUILayout.Button("Auto Generate Mapping"))
                {
                    face.AutoMapBlendshapes();
                    Refresh(face);
                }

                if (GUILayout.Button("Clear Mapping"))
                {
                    face.ClearBlendshapes();
                    Refresh(face);
                }

                EditorGUILayout.Space();

                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
                {
                    EditorGUILayout.PropertyField(_mappings.GetArrayElementAtIndex(i),
                        new GUIContent(renderer.sharedMesh.GetBlendShapeName(i)));
                }
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.PropertyField(_allowDuplicateMapping,
            new GUIContent("Allow duplicate mapping"));

        serializedObject.ApplyModifiedProperties();

        static void Refresh(OVRCustomFace face)
        {
            EditorUtility.SetDirty(face);
            EditorSceneManager.MarkSceneDirty(face.gameObject.scene);
        }
    }

    internal static bool IsFaceExpressionsConfigured(OVRFace face)
    {
        return face._faceExpressions != null;
    }

    internal static void FixFaceExpressions(OVRFace face)
    {
        Undo.IncrementCurrentGroup();
        var gameObject = face.gameObject;

        var faceExpressions = face.SearchFaceExpressions();
        if (!faceExpressions)
        {
            faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
            Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
        }

        Undo.RecordObject(face, "Linked OVRFaceExpression");
        face._faceExpressions = faceExpressions;

        EditorUtility.SetDirty(face);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Undo.SetCurrentGroupName("Configure OVRFaceExpressions for OVRCustomFace");
    }

    private static SkinnedMeshRenderer GetSkinnedMeshRenderer(SerializedProperty property)
    {
        GameObject targetObject = GetGameObject(property);

        if (!targetObject)
            return null;

        return targetObject.GetComponent<SkinnedMeshRenderer>();
    }

    private static GameObject GetGameObject(SerializedProperty property)
    {
        Component targetComponent = property.serializedObject.targetObject as Component;

        if (targetComponent && targetComponent.gameObject)
        {
            return targetComponent.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Find the best matching blend shape for each facial expression based on their names
    /// </summary>
    /// <remarks>
    /// Auto generation idea is to tokenize expression enum strings and blend shape name strings and find matching tokens
    /// We quantify the quality of the match by the total number of characters in the matching tokens
    /// We require at least a total of more than 2 characters to match, to avoid matching just L/R LB/RB etc.
    /// A better technique might be to use Levenshtein distance to match the tokens to allow some typos while still being loose on order of tokens
    /// </remarks>
    /// <param name="skinnedMesh">The mesh to find a mapping for.</param>
    /// <param name="blendShapeNames">Array of blend shape names</param>
    /// <param name="faceExpressions">Array of FaceExpression id for mapping to them</param>
    /// <param name="allowDuplicateMapping">Whether to allow duplicate mapping or not</param>
    /// <returns>Returns an array of <see cref="OVRFaceExpressions.FaceExpression"/> of the same length as the number of blendshapes on the <paramref name="skinnedMesh"/> with each element identifying the closest found match</returns>
    internal static OVRFaceExpressions.FaceExpression[] AutoGenerateMapping(
        Mesh skinnedMesh,
        string[] blendShapeNames,
        OVRFaceExpressions.FaceExpression[] faceExpressions,
        bool allowDuplicateMapping)
    {
        Assert.AreEqual(blendShapeNames.Length, faceExpressions.Length);
        var result = new OVRFaceExpressions.FaceExpression[skinnedMesh.blendShapeCount];
        var expressionTokens = new HashSet<string>[blendShapeNames.Length];
        for (int i = 0; i < blendShapeNames.Length; ++i)
        {
            expressionTokens[i] = TokenizeString(blendShapeNames[i]);
        }

        var usedBlendshapes = new HashSet<OVRFaceExpressions.FaceExpression>();
        for (int i = 0; i < skinnedMesh.blendShapeCount; ++i)
        {
            var blendShapeName = skinnedMesh.GetBlendShapeName(i);
            var bestMatchFound = FindBestMatch(
                expressionTokens,
                blendShapeName,
                faceExpressions,
                OVRFaceExpressions.FaceExpression.Invalid);
            // If not allowing duplicates, make an exception for liptowards.
            if (!allowDuplicateMapping &&
                (usedBlendshapes.Contains(bestMatchFound) &&
                 !IsLipsToward(blendShapeName)))
            {
                result[i] = OVRFaceExpressions.FaceExpression.Invalid;
            }
            else
            {
                result[i] = bestMatchFound;
                usedBlendshapes.Add(bestMatchFound);
            }
        }

        return result;
    }

    private static OVRFaceExpressions.FaceExpression FindBestMatch(HashSet<string>[] tokenizedOptions,
        string searchString, OVRFaceExpressions.FaceExpression[] expressions,
        OVRFaceExpressions.FaceExpression fallback)
    {
        searchString = searchString.Substring(searchString.LastIndexOf('.') + 1); //remove model name prefix if present
        HashSet<string> blendShapeTokens = TokenizeString(searchString);

        OVRFaceExpressions.FaceExpression bestMatch = fallback;

        // require more than two characters to match in an expression, to avoid just matching L/ LB/ R/RB
        int bestMatchCount = 2;

        for (int j = 0; j < tokenizedOptions.Length; ++j)
        {
            int thisMatchCount = 0;
            HashSet<string> thisSet = tokenizedOptions[j];
            // Currently we only allow exact matches, using Levenshtein distance for fuzzy matches
            // would allow for handling of common typos and other slight mismatches
            foreach (string matchingToken in blendShapeTokens.Intersect(thisSet))
            {
                thisMatchCount += matchingToken.Length;
            }

            if (thisMatchCount > bestMatchCount)
            {
                bestMatchCount = thisMatchCount;
                bestMatch = expressions[j];
            }
        }

        return bestMatch;
    }

    private static bool IsLipsToward(string blendshapeName)
    {
        blendshapeName = blendshapeName.Substring(blendshapeName.IndexOf('.') + 1);
        return blendshapeName == "lipsToward_LB" ||
               blendshapeName == "lipsToward_RB" ||
               blendshapeName == "lipsToward_LT" ||
               blendshapeName == "lipsToward_RT";
    }

    internal static HashSet<string> TokenizeString(string s)
    {
        var separators = new char[] { ' ', '_', '-', ',', '.', ';' };
        // add both the camel case and non-camel case split versions since the
        // camel case split doesn't handle all caps
        //(it's fundamentally ambigous without natural language comprehension)
        // duplicates don't matter as we later will hash them and they should match
        var splitTokens = SplitCamelCase(s).Split(separators).Concat(s.Split(separators));

        var hashCodes = new HashSet<string>();
        foreach (string token in splitTokens)
        {
            string lowerCaseToken = token.ToLowerInvariant();
            // give a chance for synonyms to mach with low weight
            if (lowerCaseToken == "left" || lowerCaseToken == "l")
            {
                hashCodes.Add("L");
            }

            if (lowerCaseToken == "right" || lowerCaseToken == "r")
            {
                hashCodes.Add("R");
            }

            hashCodes.Add(lowerCaseToken);
        }

        return hashCodes;
    }

    private static string SplitCamelCase(string input) => System.Text.RegularExpressions.Regex
        .Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
}

public static class OVRCustomFaceEditorExtensions
{

    public static void AutoMapBlendshapes(this OVRCustomFace customFace)
    {
        var type = customFace.retargetingType;
        var renderer = customFace.GetComponent<SkinnedMeshRenderer>();

        try
        {
            OVRFaceExpressions.FaceExpression[] generatedMapping;
            switch (type)
            {
                case OVRCustomFace.RetargetingType.OculusFace:
                    generatedMapping = OculusFaceAutoGenerateMapping(renderer.sharedMesh,
                        customFace._allowDuplicateMapping);
                    break;
                case OVRCustomFace.RetargetingType.Custom:
                    generatedMapping = CustomAutoGeneratedMapping(customFace,
                        renderer.sharedMesh,
                        customFace._allowDuplicateMapping);
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Invalid {nameof(OVRCustomFace.RetargetingType)}");
            }

            if (generatedMapping != null)
            {
                Assert.AreEqual(generatedMapping.Length, renderer.sharedMesh.blendShapeCount);
                if (customFace._mappings == null || customFace._mappings.Length != renderer.sharedMesh.blendShapeCount)
                {
                    customFace._mappings =
                        new OVRFaceExpressions.FaceExpression[renderer.sharedMesh.blendShapeCount];
                }

                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
                {
                    customFace._mappings[i] = generatedMapping[i];
                }
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog($"Auto Map Face Error", e.Message, "Ok");
        }
    }

    public static void ClearBlendshapes(this OVRCustomFace customFace)
    {
        var renderer = customFace.GetComponent<SkinnedMeshRenderer>();
        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; ++i)
        {
            customFace._mappings[i] = OVRFaceExpressions.FaceExpression.Invalid;
        }
    }

    internal static OVRFaceExpressions.FaceExpression[] OculusFaceAutoGenerateMapping(Mesh sharedMesh,
        bool allowDuplicateMapping)
    {
        string[] oculusBlendShapeNames = Enum.GetNames(typeof(OVRFaceExpressions.FaceExpression));
        OVRFaceExpressions.FaceExpression[] oculusFaceExpressions =
            (OVRFaceExpressions.FaceExpression[])Enum.GetValues(typeof(OVRFaceExpressions.FaceExpression));
        return OVRCustomFaceEditor.AutoGenerateMapping(sharedMesh,
            oculusBlendShapeNames, oculusFaceExpressions, allowDuplicateMapping);
    }

    internal static OVRFaceExpressions.FaceExpression[] CustomAutoGeneratedMapping(OVRCustomFace customFace,
        Mesh sharedMesh,
        bool allowDuplicateMapping)
    {
        string[] customBlendShapeNames;
        OVRFaceExpressions.FaceExpression[] customFaceExpressions;
        (customBlendShapeNames, customFaceExpressions) = customFace.GetCustomBlendShapeNameAndExpressionPairs();
        return OVRCustomFaceEditor.AutoGenerateMapping(sharedMesh,
            customBlendShapeNames, customFaceExpressions, allowDuplicateMapping);
    }

}
