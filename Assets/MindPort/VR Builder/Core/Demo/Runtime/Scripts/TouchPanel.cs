using UnityEngine;

namespace VRBuilder.DemoScene
{
    public class TouchPanel : MonoBehaviour
    {
        [SerializeField]
        private Color32 defaultColor, touchingColor;

        [SerializeField]
        private int materialIndex= 0;

        [SerializeField]
        private string materialColorProperty = "_EmissionColor";


        private Material materialInstance;

        void Start()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                materialInstance = meshRenderer.materials[materialIndex];
            }
            else
            {
                Debug.LogError($"No mesh renderer found on game object {gameObject.name}.");
            }

#if VR_BUILDER_XR_INTERACTION
            XRInteraction.Properties.TouchableProperty touchableProperty = GetComponent<XRInteraction.Properties.TouchableProperty>();

            if(touchableProperty != null)
            {
                touchableProperty.TouchStarted.AddListener((args) => SetMaterialColor(touchingColor));
                touchableProperty.TouchEnded.AddListener((args) => SetMaterialColor(defaultColor));

                SetMaterialColor(touchableProperty.IsBeingTouched ? touchingColor : defaultColor);
            }
            else
            {
                Debug.LogError($"No touchable property found on game object {gameObject.name}.");
            }
#endif
        }

        private void SetMaterialColor(Color32 color)
        {
            materialInstance.SetColor(materialColorProperty, color);
        }
    }
}