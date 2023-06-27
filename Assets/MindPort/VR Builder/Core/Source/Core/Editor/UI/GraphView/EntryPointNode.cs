using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Entry point node in a graph view editor.
    /// </summary>
    public class EntryPointNode : ProcessGraphNode
    {
        public EntryPointNode() : base()
        {
            title = "Start";
            IsEntryPoint = true;

            capabilities = Capabilities.Selectable;
            capabilities |= Capabilities.Movable;

#if UNITY_2020_1_OR_NEWER
            capabilities |= Capabilities.Snappable;
#endif

            titleButtonContainer.Clear();

            AddTransitionPort(false);

            SetPosition(new Rect(GlobalEditorHandler.GetCurrentChapter().ChapterMetadata.EntryNodePosition, defaultNodeSize));
        }

        /// <inheritdoc/>
        public override IStep[] Outputs => new[] { GlobalEditorHandler.GetCurrentChapter().Data.FirstStep };

        /// <inheritdoc/>
        public override IStep EntryPoint => null;

        /// <inheritdoc/>
        public override Vector2 Position { get => GlobalEditorHandler.GetCurrentChapter().ChapterMetadata.EntryNodePosition; set => GlobalEditorHandler.GetCurrentChapter().ChapterMetadata.EntryNodePosition = value; }

        /// <inheritdoc/>
        public override string Name { get => title; set => title = value; }

        public override void OnSelected()
        {
            base.OnSelected();

            GlobalEditorHandler.ChangeCurrentStep(null);
        }

        /// <inheritdoc/>
        public override void AddToChapter(IChapter chapter)
        {
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
        }

        /// <inheritdoc/>
        public override void RemoveFromChapter(IChapter chapter)
        {
        }

        /// <inheritdoc/>
        public override void SetOutput(int index, IStep output)
        {
            GlobalEditorHandler.GetCurrentChapter().Data.FirstStep = output;
        }

        /// <inheritdoc/>
        protected override void RemovePortWithUndo(Port port)
        {
        }
    }
}
