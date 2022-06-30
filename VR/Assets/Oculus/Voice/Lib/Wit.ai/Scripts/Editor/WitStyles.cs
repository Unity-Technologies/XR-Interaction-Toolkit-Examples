/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi
{
    public class WitStyles
    {
        public static Texture2D WitIcon;
        public static Texture2D MainHeader;
        public static Texture2D ContinueButton;

        public static Texture2D TextureWhite;
        public static Texture2D TextureWhite25P;
        public static Texture2D TextureBlack25P;
        public static Texture2D TextureFBBlue;
        public static Texture2D TextureTextField;
        public static Texture2D TextureWitDark;
        public static GUIStyle BackgroundWhite;
        public static GUIStyle BackgroundWhite25P;
        public static GUIStyle BackgroundBlack25P;
        public static GUIStyle BackgroundWitDark;

        public static GUIStyle LabelHeader;
        public static GUIStyle LabelHeader2;
        public static GUIStyle Label;
        public static GUIStyle WordwrappedLabel;
        public static GUIStyle FacebookButton;

        public static GUIStyle TextField;

        public static Color ColorFB = new Color(0.09f, 0.47f, 0.95f);
        public static GUIStyle Link;

        public static GUIContent titleContent;
        public static GUIContent welcomeTitleContent;
        public static GUIContent PasteIcon;
        public static GUIContent EditIcon;
        public static GUIContent ObjectPickerIcon;
        public static GUIStyle ImageIcon;

        public const int IconButtonWidth = 20;

        static WitStyles()
        {
            WitIcon = (Texture2D) Resources.Load("witai");
            MainHeader = (Texture2D) Resources.Load("wit-ai-title");
            ContinueButton = (Texture2D) Resources.Load("continue-with-fb");

            TextureWhite = new Texture2D(1, 1);
            TextureWhite.SetPixel(0, 0, Color.white);
            TextureWhite.Apply();

            TextureWhite25P = new Texture2D(1, 1);
            TextureWhite25P.SetPixel(0, 0, new Color(1, 1, 1, .25f));
            TextureWhite25P.Apply();

            TextureBlack25P = new Texture2D(1, 1);
            TextureBlack25P.SetPixel(0, 0, new Color(0, 0, 0, .25f));
            TextureBlack25P.Apply();

            TextureFBBlue = new Texture2D(1, 1);
            TextureFBBlue.SetPixel(0, 0, ColorFB);
            TextureFBBlue.Apply();

            TextureTextField = new Texture2D(1, 1);
            TextureTextField.SetPixel(0, 0, new Color(.85f, .85f, .95f));
            TextureTextField.Apply();

            TextureWitDark = new Texture2D(1, 1);
            TextureWitDark.SetPixel(0,0, new Color(0.267f, 0.286f, 0.31f));
            TextureWitDark.Apply();

            BackgroundWhite = new GUIStyle();
            BackgroundWhite.normal.background = TextureWhite;

            BackgroundWhite25P = new GUIStyle();
            BackgroundWhite25P.normal.background = TextureWhite25P;

            BackgroundBlack25P = new GUIStyle();
            BackgroundBlack25P.normal.background = TextureBlack25P;
            BackgroundBlack25P.normal.textColor = Color.white;

            BackgroundWitDark = new GUIStyle();
            BackgroundWitDark.normal.background = TextureWitDark;

            FacebookButton = new GUIStyle(EditorStyles.miniButton);

            Label = new GUIStyle(EditorStyles.label);
            Label.richText = true;
            Label.wordWrap = true;

            WordwrappedLabel = new GUIStyle(EditorStyles.label);
            WordwrappedLabel.wordWrap = true;

            LabelHeader = new GUIStyle(Label);
            LabelHeader.fontSize = 24;

            LabelHeader2 = new GUIStyle(Label);
            LabelHeader2.fontSize = 14;

            Link = new GUIStyle(Label);
            Link.normal.textColor = ColorFB;

            TextField = new GUIStyle(EditorStyles.textField);
            TextField.normal.background = TextureTextField;
            TextField.normal.textColor = Color.black;

            ImageIcon = new GUIStyle(EditorStyles.label);
            ImageIcon.fixedWidth = 16;
            ImageIcon.fixedHeight = 16;

            titleContent = new GUIContent("Wit.ai", WitIcon);
            welcomeTitleContent = new GUIContent("Welcome to Wit.ai", WitIcon);

            PasteIcon = EditorGUIUtility.IconContent("Clipboard");
            EditIcon = EditorGUIUtility.IconContent("editicon.sml");
            ObjectPickerIcon = EditorGUIUtility.IconContent("d_Record Off");
        }
    }
}
