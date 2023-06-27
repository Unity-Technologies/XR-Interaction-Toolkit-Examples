using UnityEngine;
using UnityEngine.UI;

namespace VRBuilder.BaseTemplate
{
    /// <summary>
    /// Utility component to style all child UI text elements.
    /// </summary>
    public class FontSetter : MonoBehaviour
    {
        [Tooltip("The font used in all child UI elements.")]
        [SerializeField]
        protected Font font;
        
        [Tooltip("Size of the font used.")]
        [SerializeField]
        protected int fontSize = 30;

        protected void Start()
        {
            SetFont();
        }
        
        private void SetFont()
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.font = font;
                text.fontSize = fontSize;
            }
        }
    }
}