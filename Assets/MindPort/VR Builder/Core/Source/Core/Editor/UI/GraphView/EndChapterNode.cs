using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using VRBuilder.Editor.UndoRedo;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Node representation for the End Chapter node.
    /// </summary>
    public class EndChapterNode : StepGraphNode
    {
        protected const float DefaultPadding = 8f;
        protected const float DropDownPadding = 4f;
        protected const float ElementWidth = 128f;

        private GoToChapterBehavior behavior;
        protected GoToChapterBehavior Behavior
        {
            get
            {
                if(behavior == null)
                {
                    behavior = (GoToChapterBehavior)step.Data.Behaviors.Data.Behaviors.FirstOrDefault(behavior => behavior is GoToChapterBehavior);
                }

                return behavior;
            }
        }

        public EndChapterNode(IStep step) : base(step)
        {
            extensionContainer.style.backgroundColor = new Color(.2f, .2f, .2f, .8f);
            DrawChapterSelectionField();

            titleButtonContainer.Clear();
            inputContainer.style.minWidth = ElementWidth / 2;
        }

        public override void OnSelected()
        {
            base.OnSelected();

            GlobalEditorHandler.ChangeCurrentStep(null);
        }

        public override Port AddTransitionPort(bool isDeletablePort = true, int index = -1)
        {
            Port port = base.AddTransitionPort(isDeletablePort, index);
            port.portName = "";
            port.visible = false;
            return port;
        }

        private void DrawChapterSelectionField()
        {
            Label label = new Label("Next chapter:");
            label.style.paddingTop = DefaultPadding;
            label.style.paddingLeft = DefaultPadding;           
            extensionContainer.Add(label);

            List<IChapter> chapters = GlobalEditorHandler.GetCurrentProcess().Data.Chapters.ToList();
            List<Guid> popupList = chapters.Select(chapter => chapter.ChapterMetadata.Guid).ToList();
            popupList.Insert(0, Guid.Empty);

            Guid selectedGuid = popupList.FirstOrDefault(guid => guid == Behavior.Data.ChapterGuid);

            int selectedIndex = 0;
            if(selectedGuid != null && popupList.Contains(selectedGuid))
            {
                selectedIndex = popupList.IndexOf(selectedGuid);
            }

            PopupField<Guid> chapterSelector = new PopupField<Guid>("", popupList, selectedIndex, (guid) => FormatSelectedValue(guid, chapters), (guid) => FormatSelectedValue(guid, chapters));            
            chapterSelector.RegisterValueChangedCallback((value) => OnChapterSelected(value));
            chapterSelector.style.minWidth = ElementWidth;
            chapterSelector.style.maxWidth = ElementWidth;
            chapterSelector.style.paddingBottom = DefaultPadding;
            chapterSelector.style.paddingLeft = DropDownPadding;    

            extensionContainer.Add(chapterSelector);

            RefreshExpandedState();
        }

        private string FormatSelectedValue(Guid guid, IEnumerable<IChapter> chapters)
        {
            if(guid == Guid.Empty)
            {
                return "<Next Chapter>";
            }

            return chapters.First(chapter => chapter.ChapterMetadata.Guid == guid).Data.Name;
        }

        private void OnChapterSelected(ChangeEvent<Guid> value)
        {
            PopupField<Guid> chapterSelector = value.target as PopupField<Guid>;
            Guid newValue = value.newValue;
            Guid oldValue = value.previousValue;

            RevertableChangesHandler.Do(new ProcessCommand(
                () =>
                {
                    Behavior.Data.ChapterGuid = newValue;
                },
                () =>
                {
                    Behavior.Data.ChapterGuid = oldValue;
                    chapterSelector.SetValueWithoutNotify(oldValue);
                }
                ));            
        }
    }
}
