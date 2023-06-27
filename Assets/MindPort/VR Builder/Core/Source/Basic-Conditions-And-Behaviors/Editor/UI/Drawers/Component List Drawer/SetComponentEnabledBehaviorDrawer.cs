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
    [DefaultProcessDrawer(typeof(SetComponentEnabledBehavior.EntityData))]
    public class SetComponentEnabledBehaviorDrawer : NameableDrawer
    {
        private const string noComponentSelected = "<none>";

        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect = base.Draw(rect, currentValue, changeValueCallback, label);

            float height = DrawLabel(rect, currentValue, changeValueCallback, label);

            height += EditorDrawingHelper.VerticalSpacing;

            Rect nextPosition = new Rect(rect.x, rect.y + height, rect.width, rect.height);

            SetComponentEnabledBehavior.EntityData data = currentValue as SetComponentEnabledBehavior.EntityData;            

            nextPosition = DrawerLocator.GetDrawerForValue(data.Target, typeof(SceneObjectReference)).Draw(nextPosition, data.Target, (value) => UpdateTargetObject(value, data, changeValueCallback), "Object");
            height += nextPosition.height;
            height += EditorDrawingHelper.VerticalSpacing;
            nextPosition.y = rect.y + height;

            if (RuntimeConfigurator.Configuration.SceneObjectRegistry.ContainsName(data.Target.UniqueName) && data.Target.Value != null)
            {
                List<Component> components = data.Target.Value.GameObject.GetComponents<Component>()
                    .Where(CanBeDisabled)
                    .Where(c => c is ISceneObject == false && c is ISceneObjectProperty == false) // Make it impossible to use this behavior to disable VR Builder components
                    .ToList();

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

                if(newComponent != currentComponent)
                {
                    currentComponent = newComponent;

                    if(currentComponent == 0)
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
            }

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

        private void UpdateTargetObject(object value, SetComponentEnabledBehavior.EntityData data, Action<object> changeValueCallback)
        {
            SceneObjectReference newTarget = (SceneObjectReference)value;
            SceneObjectReference oldTarget = data.Target;

            if (newTarget != oldTarget)
            {
                data.Target = newTarget;
                changeValueCallback(data);
                RevertableChangesHandler.Do(
                    new ProcessCommand(
                        () =>
                        {
                            data.Target = newTarget;
                            changeValueCallback(data);
                        },
                        () =>
                        {
                            data.Target = oldTarget;
                            changeValueCallback(data);
                        }));
            }
        }

        private void ChangeComponentType(string newValue, SetComponentEnabledBehavior.EntityData data, Action<object> changeValueCallback)
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

        private void UpdateRevertOnDeactivate(object value, SetComponentEnabledBehavior.EntityData data, Action<object> changeValueCallback)
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