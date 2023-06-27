using System;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Properties;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.Editor.XRInteraction
{
    /// <summary>
    /// Custom inspector for <see cref="TeleportationProperty"/>, adding a button to automatically configure <see cref="VRBuilder.XRInteraction.TeleportationAnchor"/>s.
    /// </summary>
    [CustomEditor(typeof(TeleportationProperty)), CanEditMultipleObjects]
    internal class TeleportationPropertyEditor : UnityEditor.Editor
    {
        private const string AnchorPrefabName = "Anchor";
        private const string ReticlePrefab = "TeleportReticle";
        private const string TeleportLayerName = "XR Teleport";
        private InteractionLayerMask teleportLayer;
        private LayerMask teleportRaycastLayer;
        private bool isSetup;

        private void OnEnable()
        {
            TeleportationProperty teleportationProperty = target as TeleportationProperty;
            TeleportationAnchor teleportAnchor = teleportationProperty.GetComponent<TeleportationAnchor>();
            
            if (teleportationProperty.transform.childCount != 0 && teleportAnchor.teleportAnchorTransform.name == AnchorPrefabName)
            {
                isSetup = true;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            ShowConfigurationButton();
        }

        private void ShowConfigurationButton()
        {
            if (isSetup == false && GUILayout.Button("Set Default Teleportation Anchor"))
            {
                foreach (UnityEngine.Object targetObject in serializedObject.targetObjects)
                {
                    if (targetObject is TeleportationProperty teleportationAnchor)
                    {
                        ConfigureDefaultTeleportationAnchor(teleportationAnchor);
                    }
                }
            }
        }
        
        private void ConfigureDefaultTeleportationAnchor(TeleportationProperty teleportationAnchor)
        {
            teleportLayer = InteractionLayerMask.NameToLayer(TeleportLayerName);
            teleportRaycastLayer = LayerMask.NameToLayer(TeleportLayerName);

            try
            {
                GameObject anchorPrefab = CreateVisualEffect(teleportationAnchor);
                ConfigureTeleportationAnchor(teleportationAnchor, anchorPrefab.transform);
                ConfigureCollider(teleportationAnchor);
                
                isSetup = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"There was an exception of type '{e.GetType()}' when trying to setup {name} as default Teleportation Anchor\n{e.Message}", teleportationAnchor.gameObject);
            }
        }

        private GameObject CreateVisualEffect(TeleportationProperty teleportationAnchor)
        {
            Transform anchorTransform = teleportationAnchor.transform;
            
            GameObject anchorPrefab = Instantiate(Resources.Load<GameObject>(AnchorPrefabName));
            anchorPrefab.name = anchorPrefab.name.Remove(AnchorPrefabName.Length);
            
            anchorPrefab.transform.SetPositionAndRotation((anchorTransform.position + (Vector3.up * 0.01f)), anchorTransform.rotation);
            anchorPrefab.transform.SetParent(anchorTransform);
            
            teleportationAnchor.gameObject.layer = teleportRaycastLayer;

            return anchorPrefab;
        }

        private void ConfigureTeleportationAnchor(TeleportationProperty teleportationAnchor, Transform prefabTransform)
        {
            TeleportationAnchor teleportAnchor = teleportationAnchor.GetComponent<TeleportationAnchor>();
            
            teleportAnchor.teleportAnchorTransform = prefabTransform;
            teleportAnchor.interactionLayers = 1 << teleportLayer;            
            teleportAnchor.customReticle = Resources.Load<GameObject>(ReticlePrefab);
            teleportAnchor.matchOrientation = MatchOrientation.TargetUpAndForward;
            teleportAnchor.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnDeactivated;
        }

        private void ConfigureCollider(TeleportationProperty teleportationAnchor)
        {
            BoxCollider propertyCollider = teleportationAnchor.GetComponent<BoxCollider>();

            propertyCollider.center = new Vector3(0f, 0.02f, 0f);
            propertyCollider.size = new Vector3(1f, 0.01f, 1f);
        }
    }
}
