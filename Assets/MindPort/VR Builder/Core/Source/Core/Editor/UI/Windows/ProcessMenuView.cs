// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using VRBuilder.Core;
using VRBuilder.Core.Validation;
using VRBuilder.Editor.Configuration;
using VRBuilder.Editor.ProcessValidation;
using VRBuilder.Editor.UndoRedo;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Behaviors;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// ProcessMenuView is shown on the left side of the <see cref="ProcessWindow"/> and takes care about overall
    /// settings for the process itself, especially chapters.
    /// </summary>
    internal class ProcessMenuView : ScriptableObject
    {
        #region Layout Constants
        public const float ExtendedMenuWidth = 330f;
        public const float MinimizedMenuWidth = ExpandButtonWidth + ChapterPaddingTop * 2f;

        public const float ExpandButtonHeight = ExpandButtonWidth;
        public const float ExpandButtonWidth = 28f;

        public const float VerticalSpace = 8f;
        public const float TabSpace = 8f;

        public const float ChapterPaddingTop = 8f;
        public const float ChapterPaddingBottom = 6f;

        public const float ButtonSize = 18f;

        private static EditorIcon deleteIcon;
        private static EditorIcon arrowUpIcon;
        private static EditorIcon arrowDownIcon;
        private static EditorIcon editIcon;
        private static EditorIcon folderIcon;
        private static EditorIcon chapterMenuExpandIcon;
        private static EditorIcon chapterMenuCollapseIcon;
        #endregion

        #region Events
        public class ChapterChangedEventArgs : EventArgs
        {
            public readonly IChapter CurrentChapter;

            public ChapterChangedEventArgs(IChapter chapter)
            {
                CurrentChapter = chapter;
            }
        }

        public class MenuExtendedEventArgs : EventArgs
        {
            public readonly bool IsExtended;

            public MenuExtendedEventArgs(bool isExtended)
            {
                IsExtended = isExtended;
            }
        }

        /// <summary>
        /// Will be called every time the selection of the chapter changes.
        /// </summary>
        [NonSerialized]
        public EventHandler<ChapterChangedEventArgs> ChapterChanged;

        /// <summary>
        /// Called when the menu is extended or collapsed.
        /// </summary>
        [NonSerialized]
        public EventHandler<MenuExtendedEventArgs> MenuExtendedChanged;

        /// <summary>
        /// Called to request refresh of GUI.
        /// </summary>
        [NonSerialized]
        public EventHandler<EventArgs> RefreshRequested;
        #endregion

        #region Public properties
        [SerializeField]
        private bool isExtended = true;

        /// <summary>
        /// Determines if the process menu window is shown or not.
        /// </summary>
        public bool IsExtended { get; private set; }

        [SerializeField]
        private int activeChapter = 0;

        /// <summary>
        /// Returns the current active chapter.
        /// </summary>
        public IChapter CurrentChapter
        {
            get
            {
                return Process.Data.Chapters[activeChapter];
            }
        }
        #endregion

        protected IProcess Process { get; private set; }

        protected EditorWindow ParentWindow { get; private set; }

        [SerializeField]
        private Vector2 scrollPosition;

        private ChangeNamePopup changeNamePopup;
        private RenameProcessPopup renameProcessPopup;
        private bool showConnectionBreakdown = false;

        /// <summary>
        /// Initialises the windows with the correct process and ProcessWindow (parent).
        /// This has to be done after every time the editor reloaded the assembly (recompile).
        /// </summary>
        public void Initialise(IProcess process, EditorWindow parent)
        {
            Process = process;
            ParentWindow = parent;

            activeChapter = 0;

            if (deleteIcon == null)
            {
                LoadIcons();
            }
        }

        private void LoadIcons()
        {
            deleteIcon = new EditorIcon("icon_delete");
            arrowUpIcon = new EditorIcon("icon_arrow_up");
            arrowDownIcon = new EditorIcon("icon_arrow_down");
            editIcon = new EditorIcon("icon_edit");
            folderIcon = new EditorIcon("icon_folder");
            chapterMenuExpandIcon = new EditorIcon("icon_expand_chapter");
            chapterMenuCollapseIcon = new EditorIcon("icon_collapse_chapter");
        }

        /// <summary>
        /// Draws the process menu.
        /// </summary>
        public void Draw()
        {
            IsExtended = isExtended;
            GUILayout.BeginArea(new Rect(0f, 0f, IsExtended ? ExtendedMenuWidth : MinimizedMenuWidth, ParentWindow.position.height));
            {
                GUILayout.BeginVertical("box");
                {
                    EditorColorUtils.ResetBackgroundColor();

                    DrawExtendToggle();

                    Vector2 deltaPosition = GUILayout.BeginScrollView(scrollPosition);
                    {
                        if (IsExtended)
                        {
                            DrawHeader();
                            DrawChapterList();
                            AddChapterButton();
                        }
                    }
                    GUILayout.EndScrollView();

                    if (changeNamePopup == null || changeNamePopup.IsClosed)
                    {
                        scrollPosition = deltaPosition;
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        #region Process Menu Draw
        private void DrawHeader()
        {
            GUILayout.Space(VerticalSpace);
            GUILayout.Space(VerticalSpace);

            GUILayout.BeginHorizontal();
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                GUIContent labelContent = new GUIContent("Process Name:");
                EditorGUILayout.LabelField(labelContent, labelStyle, GUILayout.Width(labelStyle.CalcSize(labelContent).x));

                GUIStyle nameStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
                GUIContent nameContent = new GUIContent(Process.Data.Name, Process.Data.Name);

                if (renameProcessPopup == null || renameProcessPopup.IsClosed)
                {
                    EditorGUILayout.LabelField(Process.Data.Name, nameStyle, GUILayout.Width(180f), GUILayout.Height(nameStyle.CalcHeight(nameContent, 180f))); Rect labelPosition = GUILayoutUtility.GetLastRect();
                    if (FlatIconButton(editIcon.Texture))
                    {
                        labelPosition = new Rect(labelPosition.x + ParentWindow.position.x - 2, labelPosition.height + labelPosition.y + ParentWindow.position.y + 4 + ExpandButtonHeight, labelPosition.width, labelPosition.height);
                        renameProcessPopup = RenameProcessPopup.Open(Process, labelPosition, scrollPosition, ParentWindow);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawChapterList()
        {
            GUILayout.Space(VerticalSpace);
            GUILayout.BeginHorizontal();
            {
                GUIContent content = new GUIContent("  Chapters", folderIcon.Texture);
                GUILayout.Label(content, EditorStyles.boldLabel, GUILayout.MaxHeight(24f));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(VerticalSpace);

            for (int position = 0; position < Process.Data.Chapters.Count; position++)
            {
                DrawChapter(position);
            }
        }

        private void DrawChapter(int position)
        {
            EditorColorUtils.ResetBackgroundColor();

            GUIStyle chapterBoxStyle = GUI.skin.GetStyle("Label");
            if (position == activeChapter)
            {
                chapterBoxStyle = GUI.skin.GetStyle("selectionRect");
            }

            chapterBoxStyle.margin = new RectOffset(0, 0, 4, 4);
            chapterBoxStyle.padding = new RectOffset(2, 2, 2, 2);

            GUILayout.BeginHorizontal(chapterBoxStyle);
            {
                EditorColorUtils.ResetBackgroundColor();
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(ChapterPaddingTop);
                    DrawChapterContent(position);
                    GUILayout.Space(ChapterPaddingBottom);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetLastRect();
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.GetTypeForControl(GUIUtility.GetControlID(FocusType.Passive)) == EventType.MouseDown)
                {
                    activeChapter = position;
                    EmitChapterChanged();

                    Event.current.Use();
                }
            }
        }

        private void DrawChapterContent(int position)
        {
            bool isActiveChapter = (activeChapter == position);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(TabSpace);
                if (isActiveChapter)
                {
                    GUILayout.Space(ChapterPaddingTop);
                }

                EditorColorUtils.SetTransparency(isActiveChapter ? 0.8f : 0.25f);
                GUILayout.Label(folderIcon.Texture, GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize));
                EditorColorUtils.ResetColor();

                if (EditorConfigurator.Instance.Validation.IsAllowedToValidate())
                {
                    IContext context = EditorConfigurator.Instance.Validation.ContextResolver.FindContext(Process.Data.Chapters[position].Data, Process);
                    if (EditorConfigurator.Instance.Validation.LastReport != null && EditorConfigurator.Instance.Validation.LastReport.GetEntriesFor(context).Count > 0)
                    {
                        EditorColorUtils.SetBackgroundColor(Color.white);
                        Rect rect = GUILayoutUtility.GetLastRect();
                        GUI.DrawTexture(new Rect(rect.x - 4, rect.y + 8, 16, 16), EditorGUIUtility.IconContent("Warning").image);
                        EditorColorUtils.ResetBackgroundColor();
                    }
                }

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.UpperLeft;
                GUILayout.Label(Process.Data.Chapters[position].Data.Name, style, GUILayout.Width(160f), GUILayout.Height(20f));
                Rect labelPosition = GUILayoutUtility.GetLastRect();

                GUILayout.FlexibleSpace();
                AddMoveUpButton(position);
                AddMoveDownButton(position);
                AddRemoveButton(position, Process.Data.Chapters.Count == 1);
                AddRenameButton(position, labelPosition);

                GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();

            if(isActiveChapter)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                showConnectionBreakdown = EditorGUILayout.Foldout(showConnectionBreakdown, "Connection overview");
                EditorGUILayout.EndHorizontal();

                if (showConnectionBreakdown)
                {
                    GUIStyle connectionStyle = EditorStyles.label;
                    connectionStyle.richText = true;

                    IDictionary<Guid, int> incomingConnections = GetIncomingConnections(position);
                    if (incomingConnections.Count > 0)
                    {
                        GUILayout.Label("\tIncoming:");
                        foreach (Guid connectedChapter in incomingConnections.Keys.OrderBy(key => Process.Data.Chapters.IndexOf(Process.Data.Chapters.FirstOrDefault(chapter => chapter.ChapterMetadata.Guid == key)))) 
                        {
                            string chapterName = "<i>Previous Chapter</i>";

                            IChapter chapter = Process.Data.Chapters.FirstOrDefault(chapter => chapter.ChapterMetadata.Guid == connectedChapter);
                            if (chapter != null)
                            {
                                chapterName = chapter.Data.Name;
                            }

                            GUILayout.Label($"\t• {chapterName} - {incomingConnections[connectedChapter]} connection(s)", connectionStyle);
                        }
                    }

                    IDictionary<Guid, int> outgoingConnections = GetOutgoingConnections(position);
                    if (outgoingConnections.Count > 0)
                    {
                        GUILayout.Label("\tOutgoing:");
                        foreach (Guid connectedChapter in outgoingConnections.Keys.OrderBy(key => Process.Data.Chapters.IndexOf(Process.Data.Chapters.FirstOrDefault(chapter => chapter.ChapterMetadata.Guid == key))))
                        {
                            string chapterName = "<i>Next Chapter</i>";

                            IChapter chapter = Process.Data.Chapters.FirstOrDefault(chapter => chapter.ChapterMetadata.Guid == connectedChapter);
                            if(chapter != null)
                            {
                                chapterName = chapter.Data.Name;
                            }

                            GUILayout.Label($"\t• {chapterName} - {outgoingConnections[connectedChapter]} connection(s)", connectionStyle);
                        }
                    }
                }
            }            
        }

        private void DrawExtendToggle()
        {
            Rect buttonPosition = new Rect((IsExtended ? ExtendedMenuWidth : MinimizedMenuWidth) - ExpandButtonWidth - ChapterPaddingTop, ChapterPaddingTop, ExpandButtonWidth, ExpandButtonHeight);
            GUIStyle style = new GUIStyle();
            style.imagePosition = ImagePosition.ImageOnly;
            if (GUI.Button(buttonPosition, IsExtended ? new GUIContent(chapterMenuCollapseIcon.Texture) : new GUIContent(chapterMenuExpandIcon.Texture), style))
            {
                isExtended = !isExtended;
                MenuExtendedChanged?.Invoke(this, new MenuExtendedEventArgs(isExtended));
            }
            GUILayout.Space(ExpandButtonHeight);
        }
        #endregion

        #region Button Actions
        private void AddMoveUpButton(int position)
        {
            if (FlatIconButton(arrowUpIcon.Texture))
            {
                if (position > 0)
                {
                    RevertableChangesHandler.Do(new ProcessCommand(
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            MoveChapterUp(position);
                        },
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            MoveChapterDown(position - 1);
                        }
                    ));
                }
            }
        }

        private void AddMoveDownButton(int position)
        {
            if (FlatIconButton(arrowDownIcon.Texture))
            {
                if (position + 1 < Process.Data.Chapters.Count)
                {
                    RevertableChangesHandler.Do(new ProcessCommand(
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            MoveChapterDown(position);
                        },
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            MoveChapterUp(position + 1);
                        }
                    ));
                }
            }
        }

        private void AddRenameButton(int position, Rect labelPosition)
        {
            if (FlatIconButton(editIcon.Texture))
            {
                labelPosition = new Rect(labelPosition.x + ParentWindow.position.x - 2, labelPosition.height + labelPosition.y + ParentWindow.position.y + 4 + ExpandButtonHeight, labelPosition.width, labelPosition.height);
                changeNamePopup = ChangeNamePopup.Open(Process.Data.Chapters[position].Data, labelPosition, scrollPosition, ParentWindow);
            }
        }

        private void AddRemoveButton(int position, bool isDisabled)
        {
            EditorGUI.BeginDisabledGroup(isDisabled);
            {
                if (FlatIconButton(deleteIcon.Texture))
                {
                    IChapter chapter = Process.Data.Chapters[position];
                    bool isDeleteTriggered = EditorUtility.DisplayDialog($"Delete Chapter '{chapter.Data.Name}'",
                        $"Do you really want to delete chapter '{chapter.Data.Name}'? You will lose all steps stored there.", "Delete",
                        "Cancel");

                    if (isDeleteTriggered)
                    {
                        RevertableChangesHandler.Do(new ProcessCommand(
                            // ReSharper disable once ImplicitlyCapturedClosure
                            () =>
                            {
                                RemoveChapterAt(position);
                            },
                            // ReSharper disable once ImplicitlyCapturedClosure
                            () =>
                            {
                                Process.Data.Chapters.Insert(position, chapter);
                                if (position == activeChapter)
                                {
                                    EmitChapterChanged();
                                }
                            }
                        ));
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void AddChapterButton()
        {
            GUILayout.Space(VerticalSpace);
            GUILayout.Space(VerticalSpace);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Chapter", GUILayout.Width(128), GUILayout.Height(32)))
                {
                    RevertableChangesHandler.Do(new ProcessCommand(
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            Process.Data.Chapters.Add(EntityFactory.CreateChapter($"Chapter {(Process.Data.Chapters.Count + 1)}"));
                            activeChapter = Process.Data.Chapters.Count - 1;
                            EmitChapterChanged();
                        },
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            RemoveChapterAt(Process.Data.Chapters.Count - 1);
                        }
                    ));
                }

                EditorGUI.BeginDisabledGroup(CurrentChapter == null);
                if (GUILayout.Button("Duplicate Chapter", GUILayout.Width(128), GUILayout.Height(32)))
                {
                    int addedChapter = activeChapter + 1;

                    RevertableChangesHandler.Do(new ProcessCommand(
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            IProcess serializedProcess = EntityFactory.CreateProcess("Serialized Process");
                            serializedProcess.Data.Chapters[0] = CurrentChapter.Clone();
                            byte[] bytes = EditorConfigurator.Instance.Serializer.ProcessToByteArray(serializedProcess);
                            IChapter clonedChapter = EditorConfigurator.Instance.Serializer.ProcessFromByteArray(bytes).Data.Chapters[0];
                            clonedChapter.Data.SetName(clonedChapter.Data.Name + " - Copy");
                            activeChapter = addedChapter;
                            Process.Data.Chapters.Insert(activeChapter, clonedChapter);
                            EmitChapterChanged();
                        },
                        // ReSharper disable once ImplicitlyCapturedClosure
                        () =>
                        {
                            RemoveChapterAt(addedChapter);
                        }
                    ));
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(VerticalSpace);
            GUILayout.Space(VerticalSpace);
        }
        #endregion

        #region Private helpers
        private void MoveChapterUp(int position)
        {
            IChapter chapter = Process.Data.Chapters[position];
            Process.Data.Chapters.RemoveAt(position);
            Process.Data.Chapters.Insert(position - 1, chapter);

            if (activeChapter == position)
            {
                activeChapter--;
            }
            else if (activeChapter == position - 1)
            {
                activeChapter++;
            }
        }

        private void MoveChapterDown(int position)
        {
            IChapter chapter = Process.Data.Chapters[position];
            Process.Data.Chapters.RemoveAt(position);
            Process.Data.Chapters.Insert(position + 1, chapter);

            if (activeChapter == position)
            {
                activeChapter++;
            }
            else if (activeChapter == position + 1)
            {
                activeChapter--;
            }
        }

        private static bool FlatIconButton(Texture icon)
        {
            EditorColorUtils.SetTransparency(0.25f);
            bool isTriggered = GUILayout.Button(icon, EditorStyles.label, GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize));
            // Creating a highlight effect if the mouse is currently hovering the button.
            Rect buttonRect = GUILayoutUtility.GetLastRect();
            if (buttonRect.Contains(Event.current.mousePosition))
            {
                EditorColorUtils.SetTransparency(0.5f);
                GUI.Label(buttonRect, icon);
            }

            EditorColorUtils.ResetColor();
            return isTriggered;
        }

        private void EmitChapterChanged()
        {
            if (ChapterChanged != null)
            {
                ChapterChanged.Invoke(this, new ChapterChangedEventArgs(CurrentChapter));
            }
        }

        private void RemoveChapterAt(int position)
        {
            if (position > 0)
            {
                Process.Data.Chapters.RemoveAt(position);
            }
            else if (Process.Data.Chapters.Count > 1)
            {
                Process.Data.Chapters.RemoveAt(position);
            }

            if (position < activeChapter)
            {
                activeChapter--;
            }

            if (activeChapter == position)
            {
                if (Process.Data.Chapters.Count == position)
                {
                    activeChapter--;
                    EmitChapterChanged();
                }
            }

            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private IDictionary<Guid, int> GetOutgoingConnections(int chapterIndex)
        {
            Dictionary<Guid, int> connections = new Dictionary<Guid, int>();
            IChapter chapter = Process.Data.Chapters[chapterIndex];
            if (chapter == null)
            {
                return connections;
            }
            IChapterData chapterData = chapter.Data;

            if(chapterData.FirstStep == null)
            {
                connections.Add(Guid.Empty, 1);
            }

            IEnumerable<IStep> outgoingSteps = chapterData.Steps.Where(step => step.Data.Transitions.Data.Transitions.Any(transition => transition.Data.TargetStep == null));

            foreach(IStep step in outgoingSteps)
            {
                Guid nextChapter = Guid.Empty;
                GoToChapterBehavior goToChapter = step.Data.Behaviors.Data.Behaviors.FirstOrDefault(behavior => behavior is GoToChapterBehavior) as GoToChapterBehavior;

                if(goToChapter != null)
                {
                    IChapter targetChapter = Process.Data.Chapters.FirstOrDefault(chapter => chapter.ChapterMetadata.Guid == goToChapter.Data.ChapterGuid);
                    if(targetChapter != null)
                    {
                        nextChapter = targetChapter.ChapterMetadata.Guid;
                    }
                }

                if(connections.ContainsKey(nextChapter))
                {
                    connections[nextChapter]++;
                }
                else
                {
                    connections.Add(nextChapter, 1);
                }
            }

            return connections;
        }

        private IDictionary<Guid, int> GetIncomingConnections(int chapterIndex)
        {
            Dictionary<Guid, int> connections = new Dictionary<Guid, int>();
            IChapter currentChapter = Process.Data.Chapters[chapterIndex];
            if (currentChapter == null)
            {
                return connections;
            }

            foreach(IChapter chapter in Process.Data.Chapters)
            {
                if (Process.Data.Chapters.IndexOf(chapter) == chapterIndex - 1 && chapter.Data.FirstStep == null)
                {
                    connections.Add(Guid.Empty, 1);
                }

                IEnumerable<IStep> outgoingSteps = chapter.Data.Steps.Where(step => step.Data.Transitions.Data.Transitions.Any(transition => transition.Data.TargetStep == null));

                foreach(IStep step in outgoingSteps)
                {
                    GoToChapterBehavior goToChapter = step.Data.Behaviors.Data.Behaviors.FirstOrDefault(behavior => behavior is GoToChapterBehavior) as GoToChapterBehavior;

                    if (goToChapter != null)
                    {
                        if(goToChapter.Data.ChapterGuid == currentChapter.ChapterMetadata.Guid)
                        {
                            if(connections.ContainsKey(chapter.ChapterMetadata.Guid))
                            {
                                connections[chapter.ChapterMetadata.Guid]++;
                            }
                            else
                            {
                                connections.Add(chapter.ChapterMetadata.Guid, 1);
                            }
                        }
                    }
                    else if(Process.Data.Chapters.IndexOf(chapter) == chapterIndex - 1)
                    {
                        if (connections.ContainsKey(Guid.Empty))
                        {
                            connections[Guid.Empty]++;
                        }
                        else
                        {
                            connections.Add(Guid.Empty, 1);
                        }
                    }
                }
            }

            return connections;
        }
        #endregion
    }
}
