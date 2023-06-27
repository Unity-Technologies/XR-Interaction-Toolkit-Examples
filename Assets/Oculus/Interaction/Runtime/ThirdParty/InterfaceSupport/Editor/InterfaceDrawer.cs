/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Oculus.Interaction.InterfaceSupport
{
    /// <summary>
    /// This property drawer is the meat of the interface support implementation. When
    /// the value of field with this attribute is modified, the new value is tested
    /// against the interface expected. If the component matches, the new value is
    /// accepted. Otherwise, the old value is maintained.
    /// </summary>
    [CustomPropertyDrawer(typeof(InterfaceAttribute))]
    public class InterfaceDrawer : PropertyDrawer
    {
        private int _filteredObjectPickerID;
        private static readonly Type[] _singleMonoBehaviourType = new Type[1] { typeof(MonoBehaviour) };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _filteredObjectPickerID = GUIUtility.GetControlID(FocusType.Passive);
            if (property.serializedObject.isEditingMultipleObjects) return;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "InterfaceAttribute can only " +
                    "be used with Object Reference fields.");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Type[] attTypes = GetInterfaceTypes(property);

            // Pick a specific component
            UnityEngine.Object oldObject = property.objectReferenceValue;
            GameObject temporaryGameObject = null;
            string oldObjectName = "";

            if (Event.current.type == EventType.Repaint)
            {
                string attTypesName = GetTypesName(attTypes);
                if (oldObject == null)
                {
                    temporaryGameObject = new GameObject("None" + " (" + attTypesName + ")");
                    temporaryGameObject.hideFlags = HideFlags.HideAndDontSave;
                    oldObject = temporaryGameObject;
                }
                else
                {
                    oldObjectName = oldObject.name;
                    oldObject.name = oldObjectName + " (" + attTypesName + ")";
                }
            }

            UnityEngine.Object candidateObject =
                EditorGUI.ObjectField(position, label, oldObject, typeof(UnityEngine.Object), true);

            int objectPickerID = GUIUtility.GetControlID(FocusType.Passive) - 1;
            ReplaceObjectPickerForControl(attTypes, objectPickerID);
            if (Event.current.commandName == "ObjectSelectorUpdated" &&
                EditorGUIUtility.GetObjectPickerControlID() == _filteredObjectPickerID)
            {
                candidateObject = EditorGUIUtility.GetObjectPickerObject();
            }

            if (Event.current.type == EventType.Repaint)
            {
                if (temporaryGameObject != null)
                    GameObject.DestroyImmediate(temporaryGameObject);
                else
                    oldObject.name = oldObjectName;
            }

            UnityEngine.Object matchingObject = null;

            if (candidateObject != null)
            {
                // Make sure the assigned object it is the interface we are looking for.
                if (IsAssignableFromTypes(candidateObject.GetType(), attTypes))
                {
                    matchingObject = candidateObject;
                }
                else if (candidateObject is GameObject gameObject)
                {
                    // If assigned component is a GameObject, find all matching components
                    // on it and if there are multiple, open the picker window.
                    List<MonoBehaviour> monos = new List<MonoBehaviour>();
                    monos.AddRange(gameObject.GetComponents<MonoBehaviour>().
                        Where((mono) => IsAssignableFromTypes(mono.GetType(), attTypes)));

                    if (monos.Count > 1)
                    {
                        EditorApplication.delayCall += () => InterfacePicker.Show(property, monos);
                    }
                    else
                    {
                        matchingObject = monos.Count == 1 ? monos[0] : null;
                    }
                }
            }

            if (candidateObject == null || matchingObject != null)
            {
                property.objectReferenceValue = matchingObject;
            }

            EditorGUI.EndProperty();
        }

        private bool IsAssignableFromTypes(Type source, Type[] targets)
        {
            foreach (Type t in targets)
            {
                if (!IsAssignableTo(source, t))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetTypesName(Type[] attTypes)
        {
            if (attTypes.Length == 1)
            {
                return GetTypeName(attTypes[0]);
            }

            string typesString = "";
            for (int i = 0; i < attTypes.Length; i++)
            {
                if (i > 0)
                {
                    typesString += ", ";
                }

                typesString += GetTypeName(attTypes[i]);
            }

            return typesString;
        }

        private static string GetTypeName(Type attType)
        {
            if (!attType.IsGenericType)
            {
                return attType.Name;
            }

            var genericTypeNames = attType.GenericTypeArguments.Select(GetTypeName);
            return $"{attType.Name}<{string.Join(", ", genericTypeNames)}>";
        }

        private Type[] GetInterfaceTypes(SerializedProperty property)
        {
            InterfaceAttribute att = (InterfaceAttribute)attribute;
            Type[] t = att.Types;
            if (!String.IsNullOrEmpty(att.TypeFromFieldName))
            {
                var thisType = property.serializedObject.targetObject.GetType();
                while (thisType != null)
                {
                    var referredFieldInfo = thisType.GetField(att.TypeFromFieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (referredFieldInfo != null)
                    {
                        t = new Type[1] { referredFieldInfo.FieldType };
                        break;
                    }
                    var referredPropertyInfo = thisType.GetProperty(att.TypeFromFieldName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (referredPropertyInfo != null)
                    {
                        t = new Type[1] { referredPropertyInfo.PropertyType };
                        break;
                    }

                    thisType = thisType.BaseType;
                }
            }

            return t ?? _singleMonoBehaviourType;
        }

        void ReplaceObjectPickerForControl(Type[] attTypes, int replacePickerID)
        {
            var currentObjectPickerID = EditorGUIUtility.GetObjectPickerControlID();
            if (currentObjectPickerID != replacePickerID)
            {
                return;
            }

            var derivedTypes = TypeCache.GetTypesDerivedFrom(attTypes[0]);
            HashSet<Type> validTypes = new HashSet<Type>(derivedTypes);
            for (int i = 1; i < attTypes.Length; i++)
            {
                var derivedTypesIntersect = TypeCache.GetTypesDerivedFrom(attTypes[i]);
                validTypes.IntersectWith(derivedTypesIntersect);
            }

            //start filter with a long empty area to allow for easy clicking and typing
            var filterBuilder = new System.Text.StringBuilder("                       ");
            foreach (Type type in validTypes)
            {
                if (type.IsGenericType)
                {
                    continue;
                }
                filterBuilder.Append("t:" + type.FullName + " ");
            }
            string filter = filterBuilder.ToString();
            EditorGUIUtility.ShowObjectPicker<Component>(null, true, filter, _filteredObjectPickerID);
        }

        private bool IsAssignableTo(Type fromType, Type toType)
        {
            // is open interface
            if (toType.IsGenericType && toType.IsGenericTypeDefinition)
            {
                return IsAssignableToGenericType(fromType, toType);
            }

            return toType.IsAssignableFrom(fromType);
        }

        private bool IsAssignableToGenericType(Type fromType, Type toType)
        {
            var interfaceTypes = fromType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == toType)
                {
                    return true;
                }
            }

            if (fromType.IsGenericType && fromType.GetGenericTypeDefinition() == toType)
            {
                return true;
            }

            Type baseType = fromType.BaseType;
            if (baseType == null)
            {
                return false;
            }

            return IsAssignableToGenericType(baseType, toType);
        }
    }
}
