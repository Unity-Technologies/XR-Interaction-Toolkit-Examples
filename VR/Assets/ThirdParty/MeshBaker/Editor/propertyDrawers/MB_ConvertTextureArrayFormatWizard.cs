using UnityEditor;
using UnityEngine;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    public class MB_ConvertTextureArrayFormatWizard : ScriptableWizard
    {
        public Texture2DArray textureArray;
        public TextureFormat format = TextureFormat.ARGB32;

        [MenuItem("Window/Mesh Baker/TextureArray Format Converter")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<MB_ConvertTextureArrayFormatWizard>("Convert Texture Array Format", "Close", "Convert");
        }

        void OnWizardCreate()
        {

        }

        void OnWizardUpdate()
        {
            helpString = "Please assign a texture array";
        }

        void OnWizardOtherButton()
        {
            helpString = "";
            if (textureArray == null)
            {
                helpString = "Please assign a texture array";
                return;
            }

            MB3_EditorMethods editorMethods = new MB3_EditorMethods();
            if (!editorMethods.TextureImporterFormatExistsForTextureFormat(format))
            {
                helpString = "No ImporterFormat exists for the selected format. Please select a different format.";
                return;
            }

            if (textureArray.format != TextureFormat.ARGB32 &&
                textureArray.format != TextureFormat.RGB24)
            {
                helpString = "Source TextureArray must be in format ARGB32 or RGB24. This will probably be changed in" +
                    "a future version of Mesh Baker.";
                return;
            }

            Texture2DArray outArray = new Texture2DArray(textureArray.width, textureArray.height, textureArray.depth, format, true);
            if (editorMethods.ConvertTexture2DArray(textureArray, outArray, format))
            {
                string pth = UnityEditor.AssetDatabase.GetAssetPath(textureArray);
                if (pth == null) pth = "Assets/TextureArray.asset";
                pth = pth.Replace(".asset", "");
                pth += format.ToString() + ".asset";
                UnityEditor.AssetDatabase.CreateAsset(outArray, pth);
                Debug.Log("Convert success saved asset: " + pth);
            }
        }
    }
}

