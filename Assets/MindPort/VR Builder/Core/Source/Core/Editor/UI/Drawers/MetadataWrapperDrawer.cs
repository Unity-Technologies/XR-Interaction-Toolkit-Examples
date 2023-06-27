// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRBuilder.Core;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.UI.Drawers.Metadata;
using VRBuilder.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// This drawer receives a data structure which contains an actual object to draw and additional drawing information.
    /// It takes metadata entries one by one and recursively calls its Draw method, until no unprocessed metadata left.
    /// After that, an actual object is drawn.
    /// </summary>
    [DefaultProcessDrawer(typeof(MetadataWrapper))]
    internal class MetadataWrapperDrawer : AbstractDrawer
    {
        private readonly string reorderableName = "ReorderableElement";
        private readonly string separatedName = typeof(SeparatedAttribute).FullName;
        private readonly string deletableName = typeof(DeletableAttribute).FullName;
        private readonly string foldableName = typeof(FoldableAttribute).FullName;
        private readonly string drawIsBlockingToggleName = typeof(DrawIsBlockingToggleAttribute).FullName;
        private readonly string extendableListName = typeof(ExtendableListAttribute).FullName;
        private readonly string keepPopulatedName = typeof(KeepPopulatedAttribute).FullName;
        private readonly string reorderableListOfName = typeof(ReorderableListOfAttribute).FullName;
        private readonly string listOfName = typeof(ListOfAttribute).FullName;
        private readonly string showHelpName = typeof(HelpAttribute).FullName;
        private static readonly EditorIcon deleteIcon = new EditorIcon("icon_delete");
        private static readonly EditorIcon arrowUpIcon = new EditorIcon("icon_arrow_up");
        private static readonly EditorIcon arrowDownIcon = new EditorIcon("icon_arrow_down");
        private static readonly EditorIcon helpIcon = new EditorIcon("icon_help");

        
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            MetadataWrapper wrapper = (MetadataWrapper)currentValue;
            // If the drawn object is a ITransition, IBehavior or ICondition the list object will be part of a header.
            bool isPartOfHeader = wrapper.ValueDeclaredType == typeof(ITransition) || wrapper.ValueDeclaredType == typeof(IBehavior) || wrapper.ValueDeclaredType == typeof(ICondition);

            if (wrapper.Metadata.ContainsKey(showHelpName))
            {
                return DrawHelp(rect, wrapper, changeValueCallback, label, isPartOfHeader);
            }
            if (wrapper.Metadata.ContainsKey(reorderableName))
            {
                return DrawReorderable(rect, wrapper, changeValueCallback, label, isPartOfHeader);
            }

            if (wrapper.Metadata.ContainsKey(separatedName))
            {
                return DrawSeparated(rect, wrapper, changeValueCallback, label);
            }

            if (wrapper.Metadata.ContainsKey(deletableName))
            {
                return DrawDeletable(rect, wrapper, changeValueCallback, label, isPartOfHeader);
            }

            if (wrapper.Metadata.ContainsKey(foldableName))
            {
                return DrawFoldable(rect, wrapper, changeValueCallback, label, isPartOfHeader);
            }

            if (wrapper.Metadata.ContainsKey(drawIsBlockingToggleName))
            {
                return DrawIsBlockingToggle(rect, wrapper, changeValueCallback, label);
            }

            if (wrapper.Metadata.ContainsKey(extendableListName))
            {
                return DrawExtendableList(rect, wrapper, changeValueCallback, label);
            }

            if (wrapper.Metadata.ContainsKey(keepPopulatedName))
            {
                return HandleKeepPopulated(rect, wrapper, changeValueCallback, label);
            }

            if (wrapper.Metadata.ContainsKey(reorderableListOfName))
            {
                return DrawReorderableListOf(rect, wrapper, changeValueCallback, label);
            }

            if (wrapper.Metadata.ContainsKey(listOfName))
            {
                return DrawListOf(rect, wrapper, changeValueCallback, label);
            }

            throw new NotImplementedException("Wrapper drawer for this kind of metadata is not implemented.");
        }

        /// <inheritdoc />
        public override GUIContent GetLabel(MemberInfo memberInfo, object memberOwner)
        {
            return GetLabel(ReflectionUtils.GetValueFromPropertyOrField(memberOwner, memberInfo), ReflectionUtils.GetDeclaredTypeOfPropertyOrField(memberInfo));
        }

        /// <inheritdoc />
        public override GUIContent GetLabel(object value, Type declaredType)
        {
            // Assert that value is never null, as we always call MetadataWrapper on freshly created instance.
            MetadataWrapper wrapper = value as MetadataWrapper;
            IProcessDrawer valueDrawer = DrawerLocator.GetDrawerForValue(wrapper.Value, wrapper.ValueDeclaredType);

            return valueDrawer.GetLabel(wrapper.Value, wrapper.ValueDeclaredType);
        }

        private GUIStyle GetStyle(bool isPartOfHeader = false)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };

            if (isPartOfHeader)
            {
                Texture2D normal = new Texture2D(1, 1);
                normal.SetPixels(new Color[]{ new Color(1, 1, 1, 0)  });
                normal.Apply();

                Texture2D active = new Texture2D(1, 1);
                active.SetPixels(new Color[]{ new Color(1, 1, 1, 0.05f)  });
                active.Apply();

                style.normal.background = normal;
                style.hover.background = active;
                style.active.background = active;
            }

            return style;
        }

        private Rect DrawHelp(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label, bool isPartOfHeader)
        {
            rect = DrawRecursively(rect, wrapper, showHelpName, changeValueCallback, label);
            Vector2 buttonSize = new Vector2(EditorGUIUtility.singleLineHeight + 3f, EditorDrawingHelper.SingleLineHeight);
            GUIStyle style = GetStyle(isPartOfHeader);
            if(wrapper.Value != null && wrapper.Value.GetType() != null)
            {
                HelpLinkAttribute helpLinkAttribute = wrapper.Value.GetType().GetCustomAttribute(typeof(HelpLinkAttribute)) as HelpLinkAttribute;
                if (helpLinkAttribute != null)
                {
                    if (GUI.Button(new Rect(rect.x + rect.width - buttonSize.x * 4 - 0.1f, rect.y + 1, buttonSize.x, buttonSize.y), helpIcon.Texture, style))
                    {
                        Application.OpenURL(helpLinkAttribute.HelpLink);
                    }
                }
            }
            return rect;
        }

        private Rect DrawReorderable(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label, bool isPartOfHeader)
        {
            rect = DrawRecursively(rect, wrapper, reorderableName, changeValueCallback, label);

            Vector2 buttonSize = new Vector2(EditorGUIUtility.singleLineHeight + 3f, EditorDrawingHelper.SingleLineHeight);
            
            GUIStyle style = GetStyle(isPartOfHeader);
           
            GUI.enabled = ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).IsLast == false;
            if (GUI.Button(new Rect(rect.x + rect.width - buttonSize.x * 2, rect.y + 1, buttonSize.x, buttonSize.y), arrowDownIcon.Texture, style))
            {
                object oldValue = wrapper.Value;
                ChangeValue(() =>
                    {
                        ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).MoveDown = true;
                        return wrapper;
                    },
                    () =>
                    {
                        ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).MoveDown = false;
                        wrapper.Value = oldValue;
                        return wrapper;
                    },
                    changeValueCallback);
            }

            GUI.enabled = ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).IsFirst == false;
            
            if (GUI.Button(new Rect(rect.x + rect.width - buttonSize.x * 3, rect.y + 1, buttonSize.x, buttonSize.y), arrowUpIcon.Texture, style))
            {
                object oldValue = wrapper.Value;
                ChangeValue(() =>
                    {
                        ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).MoveUp = true;
                        return wrapper;
                    },
                    () =>
                    {
                        ((ReorderableElementMetadata)wrapper.Metadata[reorderableName]).MoveUp = false;
                        wrapper.Value = oldValue;
                        return wrapper;
                    },
                    changeValueCallback);
            }

            GUI.enabled = true;
            return rect;
        }

        private Rect DrawSeparated(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            EditorDrawingHelper.DrawRect(new Rect(0f, rect.y - 1f, rect.x + rect.width, 1f), Color.grey);

            Rect wrappedRect = rect;
            wrappedRect.y += EditorDrawingHelper.VerticalSpacing;

            wrappedRect = DrawRecursively(wrappedRect, wrapper, separatedName, changeValueCallback, label);

            wrappedRect.height += EditorDrawingHelper.VerticalSpacing;

            EditorDrawingHelper.DrawRect(new Rect(0f, wrappedRect.y + wrappedRect.height - 1f, wrappedRect.x + wrappedRect.width, 1f), Color.grey);

            rect.height = wrappedRect.height;
            return rect;
        }

        private Rect DrawDeletable(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label, bool isPartOfHeader)
        {
            rect = DrawRecursively(rect, wrapper, deletableName, changeValueCallback, label);

            Vector2 buttonSize = new Vector2(EditorGUIUtility.singleLineHeight + 3, EditorDrawingHelper.SingleLineHeight);

            GUIStyle style = GetStyle(isPartOfHeader);

            if (GUI.Button(new Rect(rect.x + rect.width - buttonSize.x, rect.y + 1, buttonSize.x, buttonSize.y), deleteIcon.Texture, style))
            {
                object oldValue = wrapper.Value;
                ChangeValue(() =>
                    {
                        wrapper.Value = null;
                        return wrapper;
                    },
                    () =>
                    {
                        wrapper.Value = oldValue;
                        return wrapper;
                    },
                    changeValueCallback);
            }

            return rect;
        }

        private Rect DrawFoldable(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label, bool isPartOfHeader)
        {
            if (wrapper.Metadata[foldableName] == null)
            {
                wrapper.Metadata[foldableName] = true;
                changeValueCallback(wrapper);
            }

            bool oldIsFoldedOutValue = (bool)wrapper.Metadata[foldableName];

            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, EditorDrawingHelper.HeaderLineHeight);

            if (isPartOfHeader)
            {
                EditorGUI.DrawRect(new Rect(0, foldoutRect.y, foldoutRect.width + foldoutRect.x + 8, foldoutRect.height), new Color(62f / 256f, 62f / 256f, 62f / 256f));
                EditorGUI.DrawRect(new Rect(0, foldoutRect.y, foldoutRect.width + foldoutRect.x + 8, 1), new Color(26f / 256f, 26f / 256f, 26f / 256f));
                EditorGUI.DrawRect(new Rect(0, foldoutRect.y + foldoutRect.height, foldoutRect.width + foldoutRect.x + 8, 1), new Color(48f / 256f, 48f / 256f, 48f / 256f));
            }

            bool newIsFoldedOutValue = EditorDrawingHelper.DrawFoldoutWithReducedFocusArea(foldoutRect, oldIsFoldedOutValue, oldIsFoldedOutValue ? new GUIContent() : label, foldoutStyle, labelStyle);

            if (newIsFoldedOutValue != oldIsFoldedOutValue)
            {
                wrapper.Metadata[foldableName] = newIsFoldedOutValue;
                changeValueCallback(wrapper);
            }

            // Collapsed
            if (newIsFoldedOutValue == false)
            {
                rect.height = EditorDrawingHelper.HeaderLineHeight;
                return rect;
            }

            rect.height = 0f;

            Rect wrappedRect = rect;
            wrappedRect.x += EditorDrawingHelper.IndentationWidth;
            wrappedRect.width -= EditorDrawingHelper.IndentationWidth;

            return DrawRecursively(wrappedRect, wrapper, foldableName, (newWrapper) =>
            {
                // We want the user to be aware that value has changed even if the foldable was collapsed (for example, undo/redo).
                wrapper.Metadata[foldableName] = true;
                changeValueCallback(wrapper);
            }, label);
        }

        private Rect DrawIsBlockingToggle(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            IDataOwner dataOwner = wrapper.Value as IDataOwner;

            rect = DrawRecursively(rect, wrapper, drawIsBlockingToggleName, changeValueCallback, label);

            if (dataOwner == null)
            {
                Debug.LogError("The target property of the DrawIsBlockingToggleAttribute has to implement IDataOwner.");
                return rect;
            }

            IBackgroundBehaviorData backgroundBehaviorData = dataOwner.Data as IBackgroundBehaviorData;

            if (backgroundBehaviorData == null)
            {
                return rect;
            }

            IProcessDrawer boolDrawer = DrawerLocator.GetDrawerForValue(backgroundBehaviorData.IsBlocking, typeof(bool));
            rect.height += boolDrawer.Draw(new Rect(rect.x, rect.y + rect.height, rect.width, 0), backgroundBehaviorData.IsBlocking, (newValue) =>
            {
                backgroundBehaviorData.IsBlocking = (bool)newValue;
                changeValueCallback(wrapper);
            }, "Wait for completion").height;

            return rect;
        }

        private Rect DrawExtendableList(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            if (wrapper.Value == null || wrapper.Value is IList == false)
            {
                if (wrapper.Value != null)
                {
                    Debug.LogWarning("ExtendableListAttribute can be used only with IList members.");
                }

                return rect;
            }

            Type elementType = (wrapper.Metadata[extendableListName] as ExtendableListAttribute.SerializedTypeWrapper)?.Type;
            IList list = (IList)wrapper.Value;
            float currentY = 0;

            currentY += DrawRecursively(rect, wrapper, extendableListName, changeValueCallback, label).height;

            IProcessDrawer addThingsDrawer = DrawerLocator.GetInstantiatorDrawer(elementType);

            if (addThingsDrawer != null)
            {
                currentY += addThingsDrawer.Draw(new Rect(rect.x, rect.y + currentY, rect.width, 0), null, (newValue) =>
                {
                    if (newValue == null)
                    {
                        ReflectionUtils.RemoveFromList(ref list, list.Count - 1);
                    }
                    else
                    {
                        ReflectionUtils.InsertIntoList(ref list, list.Count, newValue);
                    }

                    if (wrapper.Metadata.ContainsKey(listOfName))
                    {
                        ListOfAttribute.Metadata temp = (ListOfAttribute.Metadata)wrapper.Metadata[listOfName];
                        temp.ChildMetadata.Add(temp.ChildAttributes.ToDictionary(attribute => attribute.Name, attribute => attribute.GetDefaultMetadata(null)));
                        wrapper.Metadata[listOfName] = temp;
                    }

                    wrapper.Value = list;
                    changeValueCallback(wrapper);
                }, "").height;
            }

            rect.height = currentY;
            return rect;
        }

        private Rect HandleKeepPopulated(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            if (wrapper.Value == null || (wrapper.Value is IList == false))
            {
                if (wrapper.Value != null)
                {
                    Debug.LogWarning("KeepPopulated can be used only with IList members.");
                }

                return rect;
            }

            IList list = (IList)wrapper.Value;

            if (list.Count == 0)
            {
                Type entryType = (Type)wrapper.Metadata[keepPopulatedName];
                if (entryType != null)
                {
                    Type listType = ReflectionUtils.GetEntryType(list);
                    if (listType.IsAssignableFrom(entryType))
                    {
                        ReflectionUtils.InsertIntoList(ref list, 0, ReflectionUtils.CreateInstanceOfType(entryType));
                    }
                    else
                    {
                        Debug.LogErrorFormat("Trying to add an keep populuated entry with type {0} to list filled {1}", entryType.Name, listType.Name);
                    }
                }
                else
                {
                    Debug.LogError("No Type found to create default instance with");
                }
            }

            return DrawRecursively(rect, wrapper, keepPopulatedName, changeValueCallback, label);
        }

        private IList<MetadataWrapper> ConvertListOfMetadataToList(MetadataWrapper wrapper)
        {
            if (CheckListOfMetadata(wrapper) == false)
            {
                return new List<MetadataWrapper>();
            }

            if (wrapper.Metadata.Count > 1)
            {
                throw new NotImplementedException($"ListOfAttribute attribute should have the lowest priority. Check MetadataWrapperDrawer.Draw method.");
            }

            ListOfAttribute.Metadata wrapperMetadata = (wrapper.Metadata[listOfName] as ListOfAttribute.Metadata);
            List<Dictionary<string, object>> listOfMetadata = wrapperMetadata.ChildMetadata;

            IList list = (IList)wrapper.Value;

            if (listOfMetadata == null)
            {
                listOfMetadata = new List<Dictionary<string, object>>(list.Count);
            }

            if (listOfMetadata.Count != list.Count)
            {
                listOfMetadata.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    listOfMetadata.Add(wrapperMetadata.ChildAttributes.ToDictionary(attribute => attribute.Name, attribute => attribute.GetDefaultMetadata(null)));
                }
            }

            return GetListOfWrappers(wrapper, listOfMetadata);
        }

        private IList<MetadataWrapper> ConvertReorderableListOfMetadataToList(MetadataWrapper wrapper)
        {
            if (CheckListOfMetadata(wrapper) == false)
            {
                return new List<MetadataWrapper>();
            }

            if (wrapper.Metadata.Count > 1)
            {
                throw new NotImplementedException($"ReorderableListOfAttribute attribute should have the lowest priority. Check MetadataWrapperDrawer.Draw method.");
            }

            ListOfAttribute.Metadata wrapperMetadata = (wrapper.Metadata[reorderableListOfName] as ListOfAttribute.Metadata);
            List<Dictionary<string, object>> listOfMetadata = wrapperMetadata.ChildMetadata;

            int wrapperCount = ((IList)wrapper.Value).Count;

            if (listOfMetadata == null)
            {
                listOfMetadata = new List<Dictionary<string, object>>(wrapperCount);
            }

            if (listOfMetadata.Count != wrapperCount)
            {
                listOfMetadata.Clear();
                for (int i = 0; i < wrapperCount; i++)
                {
                    listOfMetadata.Add(wrapperMetadata.ChildAttributes.ToDictionary(attribute => attribute.Name, attribute => attribute.GetDefaultMetadata(null)));
                    listOfMetadata[i].Add(reorderableName, new ReorderableElementMetadata());
                }
            }

            return GetListOfWrappers(wrapper, listOfMetadata);
        }

        private bool CheckListOfMetadata(MetadataWrapper wrapper)
        {
            if (wrapper.Value == null || (wrapper.Value is IList == false))
            {
                if (wrapper.Value != null)
                {
                    Debug.LogWarning($"ListOfAttribute can be used only with IList members.");
                }

                return false;
            }

            return true;
        }

        private IList<MetadataWrapper> GetListOfWrappers(MetadataWrapper wrapper, List<Dictionary<string, object>> listOfMetadata)
        {
            Type entryType = ReflectionUtils.GetEntryType(wrapper.Value);
            IList wrapperValueList = (IList)wrapper.Value;

            List<MetadataWrapper> listOfWrappers = new List<MetadataWrapper>();
            for (int i = 0; i < wrapperValueList.Count; i++)
            {
                listOfWrappers.Add(new MetadataWrapper()
                {
                    Metadata = listOfMetadata[i],
                    ValueDeclaredType = entryType,
                    Value = wrapperValueList[i],
                });
            }

            return listOfWrappers;
        }

        private Rect DrawListOf(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            IList<MetadataWrapper> listOfWrappers = ConvertListOfMetadataToList(wrapper);

            IProcessDrawer valueDrawer = DrawerLocator.GetDrawerForValue(wrapper.Value, wrapper.ValueDeclaredType);
            IList list = (IList)wrapper.Value;

            return valueDrawer.Draw(rect, listOfWrappers, (newValue) =>
            {
                List<MetadataWrapper> newListOfWrappers = ((List<MetadataWrapper>)newValue).ToList();

                ReflectionUtils.ReplaceList(ref list, newListOfWrappers.Select(childWrapper => childWrapper.Value));
                wrapper.Value = list;

                ((ListOfAttribute.Metadata)wrapper.Metadata[listOfName]).ChildMetadata = newListOfWrappers.Select(childWrapper => childWrapper.Metadata).ToList();
                changeValueCallback(wrapper);
            }, label);
        }

        private Rect DrawReorderableListOf(Rect rect, MetadataWrapper wrapper, Action<object> changeValueCallback, GUIContent label)
        {
            IList<MetadataWrapper> listOfWrappers = ConvertReorderableListOfMetadataToList(wrapper);

            IProcessDrawer valueDrawer = DrawerLocator.GetDrawerForValue(wrapper.Value, wrapper.ValueDeclaredType);
            IList list = (IList)wrapper.Value;

            for (int i = 0; i < listOfWrappers.Count; i++)
            {
                ReorderableElementMetadata metadata = (ReorderableElementMetadata)listOfWrappers[i].Metadata[reorderableName];
                metadata.IsFirst = i == 0;
                metadata.IsLast = i == listOfWrappers.Count - 1;
            }

            return valueDrawer.Draw(rect, listOfWrappers, (newValue) =>
            {
                List<MetadataWrapper> newListOfWrappers = ((List<MetadataWrapper>)newValue).ToList();

                for (int i = 0; i < newListOfWrappers.Count; i++)
                {
                    ReorderableElementMetadata metadata = (ReorderableElementMetadata)newListOfWrappers[i].Metadata[reorderableName];

                    if (metadata.MoveDown && metadata.MoveUp == false)
                    {
                        metadata.MoveDown = false;
                        if (i < newListOfWrappers.Count - 1)
                        {
                            MetadataWrapper oldElement = newListOfWrappers[i];
                            newListOfWrappers[i] = newListOfWrappers[i + 1];
                            newListOfWrappers[i + 1] = oldElement;
                        }

                        // Repeat at same index because unprocessed element switched position to i.
                        i--;
                    }
                    else if (metadata.MoveDown == false && metadata.MoveUp)
                    {
                        metadata.MoveUp = false;
                        if (i > 0)
                        {
                            MetadataWrapper oldElement = newListOfWrappers[i];
                            newListOfWrappers[i] = newListOfWrappers[i - 1];
                            newListOfWrappers[i - 1] = oldElement;
                        }
                    }
                    else
                    {
                        // Reset, if both actions are true
                        metadata.MoveDown = false;
                        metadata.MoveUp = false;
                    }
                }

                ReflectionUtils.ReplaceList(ref list, newListOfWrappers.Select(childWrapper => childWrapper.Value));
                wrapper.Value = list;

                ((ListOfAttribute.Metadata)wrapper.Metadata[reorderableListOfName]).ChildMetadata = newListOfWrappers.Select(childWrapper => childWrapper.Metadata).ToList();
                changeValueCallback(wrapper);
            }, label);
        }

        private Rect DrawRecursively(Rect rect, MetadataWrapper wrapper, string currentDrawerName, Action<object> changeValueCallback, GUIContent label)
        {
            // There are more metadata information to handle, pass it to the next iteration.
            if (wrapper.Metadata.Count > 1)
            {
                rect = DrawWrapperRecursively(rect, wrapper, changeValueCallback, currentDrawerName, label);
            }
            else
            {
                // Draw an actual object.
                IProcessDrawer valueDrawer = DrawerLocator.GetDrawerForValue(wrapper.Value, wrapper.ValueDeclaredType);

                Action<object> valueChanged = (newValue) =>
                {
                    wrapper.Value = newValue;
                    changeValueCallback(wrapper);
                };

                rect = valueDrawer.Draw(rect, wrapper.Value, valueChanged, label);
            }

            return rect;
        }

        private Rect DrawWrapperRecursively(Rect rect, MetadataWrapper parentWrapper, Action<object> changeValueCallback, string removedMetadataName, GUIContent label)
        {
            MetadataWrapper wrappedWrapper = new MetadataWrapper()
            {
                Value = parentWrapper.Value,
                ValueDeclaredType = parentWrapper.ValueDeclaredType,
                Metadata = parentWrapper.Metadata.Where(kvp => kvp.Key != removedMetadataName).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
            Action<object> wrappedWrapperChanged = (newValue) =>
            {
                MetadataWrapper newWrapper = (MetadataWrapper)newValue;

                foreach (string key in newWrapper.Metadata.Keys)
                {
                    parentWrapper.Metadata[key] = wrappedWrapper.Metadata[key];
                }

                parentWrapper.Value = newWrapper.Value;

                changeValueCallback(parentWrapper);
            };
            rect.height = Draw(rect, wrappedWrapper, wrappedWrapperChanged, label).height;
            return rect;
        }
    }
}
