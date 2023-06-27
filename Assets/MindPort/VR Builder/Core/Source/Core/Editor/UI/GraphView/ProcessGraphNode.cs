using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Process node in a graph view editor.
    /// </summary>
    public abstract class ProcessGraphNode : Node
    {
        private static EditorIcon deleteIcon;
        private static string emptyOutputPortText = "To Next Chapter";

        private Label label;
        protected Vector2 defaultNodeSize = new Vector2(200, 300);

        /// <summary>
        /// True if this is the "Start" node.
        /// </summary>
        public bool IsEntryPoint { get; set; }

        /// <summary>
        /// Name of the node.
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Steps this node leads to.
        /// </summary>
        public abstract IStep[] Outputs { get; }

        /// <summary>
        /// Step other nodes connect to.
        /// </summary>
        public abstract IStep EntryPoint { get; }

        /// <summary>
        /// Position in the graph.
        /// </summary>
        public abstract Vector2 Position { get; set; }

        /// <summary>
        /// Sets an output to the specified step.
        /// </summary>        
        public abstract void SetOutput(int index, IStep output);

        /// <summary>
        /// Adds node to specified chapter.
        /// </summary>        
        public abstract void AddToChapter(IChapter chapter);

        /// <summary>
        /// Removes node from specified chapter.
        /// </summary>        
        public abstract void RemoveFromChapter(IChapter chapter);

        /// <summary>
        /// Remove port with undo.
        /// </summary>        
        protected abstract void RemovePortWithUndo(Port port);

        public ProcessGraphNode() : base()
        {
            StyleSheet styleSheet = Resources.Load<StyleSheet>("ProcessGraphNode");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            label = titleContainer.Q<Label>();
            label.RegisterCallback<MouseDownEvent>(e => OnMouseDownEvent(e));            
        }

        /// <summary>
        /// Refreshes the node's graphics.
        /// </summary>
        public virtual void Refresh()
        {
            List<Edge> connectedEdges = new List<Edge>();

            foreach (VisualElement element in outputContainer.Children())
            {
                Port port = element as Port;

                if (port == null)
                {
                    continue;
                }

                port.connections.ToList().ForEach(e => e.RemoveFromHierarchy());
            }

            outputContainer.Clear();

            Outputs.ToList().ForEach(o => AddTransitionPort());
        }

        /// <summary>
        /// Updates the name of the output port depending on the node it is connected with.
        /// </summary>
        public void UpdateOutputPortName(Port outputPort, Node input)
        {
            if (input == null)
            {
                outputPort.portName = emptyOutputPortText;
            }
            else
            {
                outputPort.portName = $"To {input.title}";
            }
        }

        /// <summary>
        /// Adds a potentially deletable transition port to this node.
        /// </summary>
        /// <param name="isDeletablePort">If true, a delete button is created which allows to delete the transition.</param>
        /// <param name="index">Index where to insert the port, if blank it will be added at the end of the list.</param>
        /// <returns>The created port.</returns>
        public virtual Port AddTransitionPort(bool isDeletablePort = true, int index = -1)
        {
            Port port = CreatePort(Direction.Output);

            if (isDeletablePort)
            {
                Button deleteButton = new Button(() => RemovePortWithUndo(port));

                Image icon = CreateDeleteTransitionIcon();
                deleteButton.Add(icon);
                icon.StretchToParentSize();

                deleteButton.style.alignSelf = Align.Stretch;

                port.contentContainer.Insert(1, deleteButton);
            }

            UpdateOutputPortName(port, null);

            if (index < 0)
            {
                outputContainer.Add(port);
            }
            else
            {
                outputContainer.Insert(index, port);
            }

            RefreshExpandedState();
            RefreshPorts();

            return port;
        }

        protected Port CreatePort(Direction direction, Port.Capacity capacity = Port.Capacity.Single)
        {
            return InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(ProcessExec));
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
                e.StopImmediatePropagation();
            }
        }

        protected Image CreateDeleteTransitionIcon()
        {
            if (deleteIcon == null)
            {
                deleteIcon = new EditorIcon("icon_delete");
            }

            Image icon = new Image();
            icon.image = deleteIcon.Texture;
            icon.style.paddingBottom = 2;
            icon.style.paddingLeft = 2;
            icon.style.paddingRight = 2;
            icon.style.paddingTop = 2;

            return icon;
        }

        protected virtual void OpenTextEditor()
        {
            label.text = "";
            TextField textField = new TextField();
            textField.value = Name;

            textField.RegisterCallback<FocusOutEvent>(e => OnEditTextFinished(textField));
            label.Add(textField);

            textField.Focus();
            textField.SelectAll();
        }

        protected virtual void OnEditTextFinished(TextField textField)
        {
            Name = textField.value;
            label.text = textField.value;
            label.Remove(textField);            
        }      
    }
}
