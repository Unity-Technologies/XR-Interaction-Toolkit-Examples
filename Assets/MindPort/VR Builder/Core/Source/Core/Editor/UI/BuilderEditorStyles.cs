// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace VRBuilder.Editor.UI
{
    public static class BuilderEditorStyles
    {
        public const int BaseIndent = 2;

        public const int Indent = 12;
        public const int IndentLarge = Indent * 3;

        public static Color HighlightTextColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        private static GUIStyle title;
        public static GUIStyle Title
        {
            get
            {
                if (title == null)
                {
                    title = new GUIStyle(EditorStyles.largeLabel);
                    title.fontSize = 22;
                    title.fontStyle = FontStyle.Bold;
                    title.normal.textColor = HighlightTextColor;
                    title.padding = new RectOffset(BaseIndent, BaseIndent, Indent, Indent);
                }

                return title;
            }
        }

        private static GUIStyle titleNoPadding;
        public static GUIStyle TitleNoPadding
        {
            get
            {
                if (titleNoPadding == null)
                {
                    titleNoPadding = new GUIStyle(EditorStyles.largeLabel);
                    titleNoPadding.fontSize = 22;
                    titleNoPadding.fontStyle = FontStyle.Bold;
                    titleNoPadding.normal.textColor = HighlightTextColor;
                    titleNoPadding.padding = new RectOffset(BaseIndent, BaseIndent, BaseIndent, BaseIndent);
                }

                return titleNoPadding;
            }
        }

        private static GUIStyle header;
        public static GUIStyle Header
        {
            get
            {
                if (header == null)
                {
                    header = new GUIStyle(EditorStyles.largeLabel);
                    header.fontSize = 15;
                    header.fontStyle = FontStyle.Bold;
                    header.alignment = TextAnchor.UpperLeft;
                    header.normal.textColor = HighlightTextColor;
                    header.padding = new RectOffset(BaseIndent, BaseIndent, BaseIndent, BaseIndent);
                }

                return header;
            }
        }

        private static GUIStyle paragraph;
        public static GUIStyle Paragraph
        {
            get
            {
                if (paragraph == null)
                {
                    paragraph = new GUIStyle(GUI.skin.label);
                    paragraph.alignment = TextAnchor.UpperLeft;
                    paragraph.fontSize = 13;
                    paragraph.richText = true;
                    paragraph.clipping = TextClipping.Clip;
                    paragraph.wordWrap = true;
                    paragraph.padding = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent);
                }

                return paragraph;
            }
        }

        private static GUIStyle paragraphNoPadding;
        public static GUIStyle ParagraphNoPadding
        {
            get
            {
                if (paragraphNoPadding == null)
                {
                    paragraphNoPadding = new GUIStyle(GUI.skin.label);
                    paragraphNoPadding.alignment = TextAnchor.UpperLeft;
                    paragraphNoPadding.fontSize = 13;
                    paragraphNoPadding.richText = true;
                    paragraphNoPadding.clipping = TextClipping.Clip;
                    paragraphNoPadding.wordWrap = true;
                    paragraphNoPadding.padding = new RectOffset(BaseIndent, BaseIndent, BaseIndent, BaseIndent);
                }

                return paragraphNoPadding;
            }
        }

        private static GUIStyle textField;
        public static GUIStyle TextField
        {
            get
            {
                if (textField == null)
                {
                    textField = new GUIStyle(EditorStyles.textField);
                    textField.padding = new RectOffset(BaseIndent, BaseIndent, BaseIndent, BaseIndent);
                    textField.margin = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent);
                }

                return textField;
            }
        }

        private static GUIStyle toggle;
        public static GUIStyle Toggle
        {
            get
            {
                if (toggle == null)
                {
                    toggle = new GUIStyle(EditorStyles.toggle);
                    toggle.fontSize = Paragraph.fontSize;
                    toggle.padding = new RectOffset(Indent + Indent / 2, BaseIndent, BaseIndent, BaseIndent + 1); // this only affects the text
                    toggle.margin = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent); // this affects the position
                }

                return toggle;
            }
        }

        private static GUIStyle radioButton;
        public static GUIStyle RadioButton
        {
            get
            {
                if (radioButton == null)
                {
                    radioButton = new GUIStyle(EditorStyles.radioButton);
                    radioButton.fontSize = Paragraph.fontSize;
                    radioButton.padding = new RectOffset((int)(Indent + Indent * 0.75f), BaseIndent, 0, 0); // this only affects the text
                    radioButton.margin = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent); // this affects the position
                }

                return radioButton;
            }
        }

        private static GUIStyle subText;
        public static GUIStyle SubText
        {
            get
            {
                if (subText == null)
                {
                    subText = new GUIStyle(EditorStyles.miniLabel);
                    subText.normal.textColor = HighlightTextColor;
                    subText.margin = new RectOffset(2 * Indent, 0, 0, 0);
                }

                return subText;
            }
        }

        private static GUIStyle label;
        public static GUIStyle Label
        {
            get
            {
                if (label == null)
                {
                    label = new GUIStyle(GUI.skin.label);
                    label.alignment = TextAnchor.MiddleLeft;
                    label.fontSize = 13;
                    label.richText = true;
                    label.clipping = TextClipping.Clip;
                    label.padding = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent);
                }

                return label;
            }
        }


        private static GUIStyle link;
        public static GUIStyle Link
        {
            get
            {
                if (link == null)
                {
                    link = new GUIStyle(EditorStyles.linkLabel);
                    link.alignment = TextAnchor.MiddleLeft;
                    link.fontSize = 13;
                    link.richText = true;
                    link.clipping = TextClipping.Clip;
                    link.padding = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent);
                }

                return link;
            }
        }

        private static GUIStyle popup;
        public static GUIStyle Popup
        {
            get
            {
                if (popup == null)
                {
                    popup = new GUIStyle(EditorStyles.popup);
                    popup.fontSize = 13;
                    popup.margin = new RectOffset((int)(Indent * 1.25), (int)(Indent * 1.25), BaseIndent, BaseIndent);
                }

                return popup;
            }
        }

        private static GUIStyle helpBox;
        public static GUIStyle HelpBox
        {
            get
            {
                if (helpBox == null)
                {
                    helpBox = new GUIStyle(EditorStyles.helpBox);
                    helpBox.fontSize = 10;
                    helpBox.margin = new RectOffset((int)(Indent * 1.25), (int)(Indent * 1.25), BaseIndent, BaseIndent);
                }

                return helpBox;
            }
        }

        private static GUIStyle button;
        public static GUIStyle Button
        {
            get
            {
                if (button == null)
                {
                    button = new GUIStyle(GUI.skin.button);
                    button.margin = new RectOffset(Indent, BaseIndent, BaseIndent, BaseIndent);
                }

                return button;
            }
        }

        public static GUIStyle ApplyPadding(GUIStyle style, int ident = Indent)
        {
            return new GUIStyle(style) { padding = new RectOffset(ident, style.margin.right, style.margin.top, style.margin.bottom) };
        }

        public static GUIStyle ApplyPadding(GUIStyle style, RectOffset indent)
        {
            return new GUIStyle(style) { padding = indent };
        }

        public static GUIStyle ApplyMargin(GUIStyle style, int ident = Indent)
        {
            return new GUIStyle(style) { margin = new RectOffset(ident, style.margin.right, style.margin.top, style.margin.bottom) };
        }

        public static GUIStyle ApplyMargin(GUIStyle style, RectOffset indent)
        {
            return new GUIStyle(style) { margin = indent };
        }
    }
}
