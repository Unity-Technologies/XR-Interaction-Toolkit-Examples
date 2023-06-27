using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;
using VRBuilder.Editor.UI.Windows;
using VRBuilder.Editor.UndoRedo;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Editor windows that displays the process using a graphview.
    /// </summary>
    public class ProcessGraphViewWindow : ProcessEditorWindow
    {
        private EditorIcon titleIcon;

        internal EditorIcon TitleIcon
        {
            get
            {
                if (titleIcon == null)
                {
                    titleIcon = new EditorIcon("icon_process_editor");
                }

                return titleIcon;
            }
        }

        private ProcessGraphView graphView;
        private Box chapterHierarchy;

        [SerializeField]
        private ProcessMenuView chapterMenu;

        private IMGUIContainer chapterViewContainer;
        private IProcess currentProcess;
        private IChapter currentChapter;

        private void CreateGUI()
        {
            wantsMouseMove = true;
            if (chapterMenu == null)
            {
                chapterMenu = CreateInstance<ProcessMenuView>();
            }

            chapterMenu.MenuExtendedChanged += (sender, args) => { chapterViewContainer.style.width = args.IsExtended ? ProcessMenuView.ExtendedMenuWidth : ProcessMenuView.MinimizedMenuWidth; };
            chapterMenu.RefreshRequested += (sender, args) => { chapterViewContainer.MarkDirtyLayout(); };

            chapterViewContainer = new IMGUIContainer();
            rootVisualElement.Add(chapterViewContainer);
            chapterViewContainer.StretchToParentSize();
            chapterViewContainer.style.width = ProcessMenuView.ExtendedMenuWidth;
            chapterViewContainer.style.backgroundColor = new StyleColor(new Color32(51, 51, 51, 192));

            graphView = ConstructGraphView();
            chapterHierarchy = ConstructChapterHierarchy();

            GlobalEditorHandler.ProcessWindowOpened(this);
        }

        private void OnGUI()
        {
            SetTabName();
        }

        private void OnDisable()
        {
            GlobalEditorHandler.ProcessWindowClosed(this);
        }

        private void SetTabName()
        {
            titleContent = new GUIContent("Process Editor", TitleIcon.Texture);
        }

        private ProcessGraphView ConstructGraphView()
        {
            ProcessGraphView graphView = new ProcessGraphView()
            {
                name = "Process Graph"
            };

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
            graphView.SendToBack();

            return graphView;
        }

        /// <inheritdoc/>
        internal override void SetChapter(IChapter chapter)
        {
            SetupChapterHierarchy(chapter);

            currentChapter = chapter;

            if (graphView == null)
            {
                graphView = ConstructGraphView();
            }

            graphView.SetChapter(currentChapter);
        }

        /// <inheritdoc/>
        internal override void SetProcess(IProcess process)
        {
            RevertableChangesHandler.FlushStack();

            currentProcess = process;

            if (currentProcess == null)
            {
                return;
            }

            chapterMenu.Initialise(currentProcess, this);
            chapterViewContainer.onGUIHandler = () => chapterMenu.Draw();

            chapterMenu.ChapterChanged += (sender, args) =>
            {
                SetChapter(args.CurrentChapter);
            };

            SetChapter(currentProcess.Data.FirstChapter);
        }

        /// <inheritdoc/>
        internal override IChapter GetChapter()
        {
            return currentChapter;
        }

        /// <inheritdoc/>
        internal override void RefreshChapterRepresentation()
        {
            if (currentProcess != null)
            {
                graphView.RefreshSelectedNode();
            }
        }

        private Box ConstructChapterHierarchy()
        {
            Box box = new Box();

            box.style.alignSelf = Align.FlexStart;
            box.style.left = ProcessMenuView.ExtendedMenuWidth;
            box.contentContainer.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(box);

            chapterMenu.MenuExtendedChanged += (sender, args) => { box.style.left = args.IsExtended ? ProcessMenuView.ExtendedMenuWidth : ProcessMenuView.MinimizedMenuWidth; };

            return box;
        }

        private void SetupChapterHierarchy(IChapter chapter)
        {
            bool isRoot = GlobalEditorHandler.GetCurrentProcess().Data.Chapters.Contains(chapter);
            if (GlobalEditorHandler.GetCurrentProcess().Data.Chapters.Contains(chapter))
            {
                chapterHierarchy.contentContainer.Clear();
            }

            chapterHierarchy.visible = !isRoot;

            List<ChapterHierarchyElement> elements = chapterHierarchy.contentContainer.Children().Select(child => child as ChapterHierarchyElement).ToList();

            int index = elements.IndexOf(elements.FirstOrDefault(container => container.Chapter == chapter));

            if (index < 0)
            {
                elements.ForEach(element => element.SetInteractable(true));

                ChapterHierarchyElement element = new ChapterHierarchyElement(chapter, elements.Count() == 0);
                
                chapterHierarchy.Add(element);
            }
            else
            {
                while(chapterHierarchy.contentContainer.childCount > index + 1)
                {
                    chapterHierarchy.contentContainer.RemoveAt(index + 1);
                }

                elements[index].SetInteractable(false);
            }
        }

        private class ChapterHierarchyElement : VisualElement
        {
            private IChapter chapter;
            public IChapter Chapter => chapter;

            private Label chapterLabel;
            private Button chapterButton;

            public ChapterHierarchyElement(IChapter chapter, bool isFirstElement, bool isInteractable = false)
            {
                this.chapter = chapter;

                contentContainer.style.flexDirection = FlexDirection.Row;

                if (isFirstElement == false)
                {
                    Label separator = new Label(">");
                    separator.style.alignSelf = Align.Center;
                    Add(separator);
                }

                chapterButton = new Button(() => GlobalEditorHandler.RequestNewChapter(Chapter));
                chapterButton.text = Chapter.Data.Name;

                chapterLabel = new Label(Chapter.Data.Name);
                chapterLabel.style.alignSelf = Align.Center;

                SetInteractable(isInteractable);
            }

            public void SetInteractable(bool isInteractable)
            {
                if(isInteractable)
                {
                    if (contentContainer.Children().Contains(chapterLabel))
                    {
                        Remove(chapterLabel);
                    }
                    Add(chapterButton);                    
                }
                else
                {
                    if (contentContainer.Children().Contains(chapterButton))
                    {
                        Remove(chapterButton);
                    }
                    Add(chapterLabel);                    
                }
            }
        }
    }
}
