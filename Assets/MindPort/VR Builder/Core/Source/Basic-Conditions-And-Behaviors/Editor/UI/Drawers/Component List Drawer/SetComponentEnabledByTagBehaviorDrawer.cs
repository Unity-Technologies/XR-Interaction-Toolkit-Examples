using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Editor.UI;
using VRBuilder.Editor.UI.Drawers;
using VRBuilder.Editor.UndoRedo;

namespace VRBuilder.Editor.Core.UI.Drawers
{
    [DefaultProcessDrawer(typeof(SetComponentEnabledByTagBehavior.EntityData))]
    public class SetComponentEnabledByTagBehaviorDrawer : NameableDrawer
    {
        private const string noComponentSelected = "<none>";

        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect = base.Draw(rect, currentValue, changeValueCallback, label);
            float height = DrawLabel(rect, currentValue, changeValueCallback, label);
            height += EditorDrawingHelper.VerticalSpacing;
            Rect nextPosition = new Rect(rect.x, rect.y + height, rect.width, rect.height);

            SetComponentEnabledByTagBehavior.EntityData data = currentValue as SetComponentEnabledByTagBehavior.EntityData;

            nextPosition = DrawerLocator.GetDrawerForValue(data.TargetTag, typeof(SceneObjectTagBase)).Draw(nextPosition, data.TargetTag, changeValueCallback, "Tag");            

            height += nextPosition.height;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            List<Component> components = new List<Component>();

            if (data.TargetTag != null && data.TargetTag.Guid != Guid.Empty)
            {
                components = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetByTag(data.TargetTag.Guid)
                    .SelectMany(sceneObject => sceneObject.GameObject.GetComponents<Component>())
                    .Where(CanBeDisabled)
                    .Where(component => component is ISceneObject == false && component is ISceneObjectProperty == false) // Make it impossible to use this behavior to disable VR Builder components
                    .ToList();
            }

            int currentComponent = 0;

            List<string> componentLabels = components.Select(c => c.GetType().Name).ToList();
            componentLabels.Insert(0, noComponentSelected);

            if (string.IsNullOrEmpty(data.ComponentType) == false)
            {
                if (componentLabels.Contains(data.ComponentType))
                {
                    currentComponent = componentLabels.IndexOf(componentLabels.First(l => l == data.ComponentType));
                }
                else
                {
                    currentComponent = 0;
                    ChangeComponentType("", data, changeValueCallback);
                }
            }

            int newComponent = EditorGUI.Popup(nextPosition, "Component type", currentComponent, componentLabels.ToArray());

            if (newComponent != currentComponent)
            {
                currentComponent = newComponent;

                if (currentComponent == 0)
                {
                    ChangeComponentType("", data, changeValueCallback);
                }
                else
                {
                    ChangeComponentType(componentLabels[currentComponent], data, changeValueCallback);                    
                }

                changeValueCallback(data);
            }

            height += EditorDrawingHelper.SingleLineHeight;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            string revertState = data.SetEnabled ? "Disable" : "Enable";
            nextPosition = DrawerLocator.GetDrawerForValue(data.RevertOnDeactivation, typeof(bool)).Draw(nextPosition, data.RevertOnDeactivation, (value) => UpdateRevertOnDeactivate(value, data, changeValueCallback), $"{revertState} at end of step");            

            height += EditorDrawingHelper.SingleLineHeight;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            rect.height = height;
            return rect;
        }

        private bool CanBeDisabled(Component component)
        {
            return component.GetType().GetProperty("enabled") != null;
        }

        private void ChangeComponentType(string newValue, SetComponentEnabledByTagBehavior.EntityData data, Action<object> changeValueCallback)
        {
            string oldValue = data.ComponentType;

            if (newValue != oldValue)
            {
                RevertableChangesHandler.Do(
                    new ProcessCommand(
                        () =>
                        {
                            data.ComponentType = newValue;
                            changeValueCallback(data);
                        },
                        () =>
                        {
                            data.ComponentType = oldValue;
                            changeValueCallback(data);
                        }));
            }
        }

        private void UpdateRevertOnDeactivate(object value, SetComponentEnabledByTagBehavior.EntityData data, Action<object> changeValueCallback)
        {
            bool newValue = (bool)value;
            bool oldValue = data.RevertOnDeactivation;

            if (newValue != oldValue)
            {
                RevertableChangesHandler.Do(
                    new ProcessCommand(
                        () =>
                        {
                            data.RevertOnDeactivation = newValue;
                            changeValueCallback(data);
                        },
                        () =>
                        {
                            data.RevertOnDeactivation = oldValue;
                            changeValueCallback(data);
                        }));
            }
        }
    }
}