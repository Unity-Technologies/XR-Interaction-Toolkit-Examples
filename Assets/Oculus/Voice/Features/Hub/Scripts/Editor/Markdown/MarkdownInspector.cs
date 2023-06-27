/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using Meta.Voice.Hub.Interfaces;
using UnityEngine.Networking;

namespace Meta.Voice.Hub.Markdown
{
    [CustomEditor(typeof(MarkdownPage))]
    public class MarkdownInspector : Editor, IOverrideSize
    {
        private GUIStyle _linkStyle;
        private GUIStyle _normalTextStyle;
        private GUIStyle _imageLabelStyle;
        private Dictionary<string, Texture2D> _cachedImages = new Dictionary<string, Texture2D>();
        private Texture2D _badImageTex;

        private Vector2 scrollView;

        public float OverrideWidth { get; set; } = -1;
        public float OverrideHeight { get; set; } = -1;

        public override void OnInspectorGUI()
        {
            float padding = 55;
            var markdownPage = ((MarkdownPage)target);
            if (!markdownPage)
            {
                base.OnInspectorGUI();
                return;
            }

            var markdownFile = markdownPage.MarkdownFile;
            if (!markdownFile)
            {
                base.OnInspectorGUI();
                return;
            }

            var text = markdownFile.text;

            if (_linkStyle == null)
            {
                _linkStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (_normalTextStyle == null)
            {
                _normalTextStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    richText = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            Event currentEvent = Event.current;
            Regex urlRegex = new Regex(@"(https?://[^\s]+)");
            Regex imageRegex = new Regex(@"!\[(.*?)\]\((.*?)\)");
            Regex splitRegex = new Regex(@"(!\[.*?\]\(.*?\))|(https?://[^\s]+)");
            string[] parts = splitRegex.Split(text);

            scrollView = GUILayout.BeginScrollView(scrollView);
            var windowWidth = (OverrideWidth > 0 ? OverrideWidth : EditorGUIUtility.currentViewWidth) - padding;
            GUILayout.BeginVertical(GUILayout.Width(windowWidth));
            foreach (string part in parts)
            {
                if (imageRegex.IsMatch(part))
                {
                    Match imageMatch = imageRegex.Match(part);

                    if (imageMatch.Success)
                    {
                        string imagePath = imageMatch.Groups[2].Value;

                        if (!_cachedImages.ContainsKey(imagePath))
                        {
                            if (urlRegex.IsMatch(imagePath))
                            {
                                LoadImageFromUrl(imagePath);
                            }
                            else
                            {
                                var path = AssetDatabase.GetAssetPath(markdownPage);
                                var dir = Path.GetDirectoryName(path);
                                Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + imagePath);
                                if (!image)
                                {
                                    // Get the path of target markdown file
                                    string markdownPath = AssetDatabase.GetAssetPath(markdownFile);
                                    // Get the directory of the markdown file
                                    string markdownDir = System.IO.Path.GetDirectoryName(markdownPath);
                                    image = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(markdownDir,
                                        imagePath));
                                    if (!image) image = _badImageTex;
                                }

                                _cachedImages[imagePath] = image;
                            }
                        }

                        if (_cachedImages.TryGetValue(imagePath, out Texture2D img) && img && img != _badImageTex)
                        {
                            float aspectRatio = 1;
                            float width = img.width;
                            float height = img.height;
                            if (img.width > windowWidth - padding)
                            {
                                width = windowWidth - padding;
                                aspectRatio = img.width / (float) img.height;
                                height = width / aspectRatio;
                            }

                            if (null == _imageLabelStyle)
                            {
                                _imageLabelStyle = new GUIStyle(GUI.skin.label)
                                {
                                    alignment = TextAnchor.MiddleCenter,
                                    imagePosition = ImagePosition.ImageAbove
                                };
                            }

                            GUIContent content = new GUIContent(img);
                            Rect imageLabelRect = GUILayoutUtility.GetRect(content, _imageLabelStyle,
                                GUILayout.Height(height), GUILayout.Width(width));

                            if (GUI.Button(imageLabelRect, content, _imageLabelStyle))
                            {
                                ImageViewer.ShowWindow(img, Path.GetFileNameWithoutExtension(imagePath));
                            }
                        }
                    }
                }
                else if (urlRegex.IsMatch(part))
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Space(EditorGUI.indentLevel * 15);
                    GUILayout.Label("<color=blue>" + part + "</color>", _linkStyle, GUILayout.MaxWidth(windowWidth));

                    Rect linkRect = GUILayoutUtility.GetLastRect();
                    if (currentEvent.type == EventType.MouseDown && linkRect.Contains(currentEvent.mousePosition))
                    {
                        Application.OpenURL(part);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField(ParseMarkdown(part), _normalTextStyle, GUILayout.MaxWidth(windowWidth));
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public static string ParseMarkdown(string markdown)
        {
            // Headers
            markdown = Regex.Replace(markdown, @"^######\s(.*?)$", "<size=14><b>$1</b></size>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^#####\s(.*?)$", "<size=16><b>$1</b></size>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^####\s(.*?)$", "<size=18><b>$1</b></size>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^###\s(.*?)$", "<size=20><b>$1</b></size>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^##\s(.*?)$", "<size=22><b>$1</b></size>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^#\s(.*?)$", "<size=24><b>$1</b></size>", RegexOptions.Multiline);

            // Bold
            markdown = Regex.Replace(markdown, @"\*\*(.*?)\*\*", "<b>$1</b>", RegexOptions.Multiline);

            // Italic
            markdown = Regex.Replace(markdown, @"\*(.*?)\*", "<i>$1</i>", RegexOptions.Multiline);

            // Code blocks
            markdown = Regex.Replace(markdown, @"(?s)```(.*?)```", m =>
            {
                var codeLines = m.Groups[1].Value.Trim().Split('\n');
                string result = string.Empty;
                foreach (var line in codeLines)
                {
                    result += $"  <color=#a1b56c>{line}</color>\n";
                }

                return result;
            }, RegexOptions.Multiline);

            // Raw Urls
            markdown = Regex.Replace(markdown, @"(https?:\/\/[^\s""'<>]+)",
                "<link><color=#a1b56c><u>$1</u></color></link>", RegexOptions.Multiline);

            // Unordered lists
            markdown = Regex.Replace(markdown, @"^\s*\*\s(.*?)$", "• $1", RegexOptions.Multiline);

            // Ordered lists
            markdown = Regex.Replace(markdown, @"^(\d+)\.\s(.*?)$", "$1. $2", RegexOptions.Multiline);

            return markdown;
        }

        private void LoadImageFromUrl(string url)
        {
            _cachedImages[url] = _badImageTex;
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            request.SendWebRequest().completed += operation =>
            {
                if (request.responseCode == 200)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    _cachedImages[url] = texture;
                    Repaint();
                }
                else
                {
                    Debug.LogError($"Failed to load image from URL [Error {request.responseCode}]: {url}");
                }
            };
        }
    }
}
