using System;
using VRBuilder.TextToSpeech;
using VRBuilder.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.TextToSpeech.UI.ProjectSettings
{
    /// <summary>
    /// Provides text to speech settings.
    /// </summary>
    public class TextToSpeechSectionProvider : IProjectSettingsSection
    {
        /// <inheritdoc/>
        public string Title { get; } = "Text to Speech";
        
        /// <inheritdoc/>
        public Type TargetPageProvider { get; } = typeof(LanguageSettingsProvider);
        
        /// <inheritdoc/>
        public int Priority { get; } = 0;
        
        /// <inheritdoc/>
        public void OnGUI(string searchContext)
        {
            GUILayout.Label("Configuration for your Text to Speech provider.", BuilderEditorStyles.ApplyPadding(BuilderEditorStyles.Label, 0));
        
            GUILayout.Space(8);
        
            TextToSpeechConfiguration config = TextToSpeechConfiguration.Instance;
            UnityEditor.Editor.CreateEditor(config, typeof(VRBuilder.Editor.TextToSpeech.UI.TextToSpeechConfigurationEditor)).OnInspectorGUI();

            GUILayout.Space(8);
	    
	        BuilderGUILayout.DrawLink("Need Help? Visit our documentation", "https://www.mindport.co/vr-builder-tutorials/text-to-speech-audio", 0);
        
        }
        
        ~TextToSpeechSectionProvider()
        {
            if (EditorUtility.IsDirty(TextToSpeechConfiguration.Instance))
            {
                TextToSpeechConfiguration.Instance.Save();
            }
        }
    }
}