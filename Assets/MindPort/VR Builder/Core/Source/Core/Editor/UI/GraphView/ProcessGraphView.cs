using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Serialization;
using VRBuilder.Editor.Configuration;
using VRBuilder.Editor.UndoRedo;
using static UnityEditor.TypeCache;

namespace VRBuilder.Editor.UI.Graphics
{
    /// <summary>
    /// Graphical representation of a process chapter.
    /// </summary>
    public class ProcessGraphView : GraphView
    {
        private Vector2 defaultViewTransform = new Vector2(400, 100);
        private IChapter currentChapter;
        private ProcessGraphNode entryNode;
        private List<IStepNodeInstantiator> instantiators = new List<IStepNodeInstantiator>();
        private Dictionary<IChapter, ViewTransform> storedViewTransforms = new Dictionary<IChapter, ViewTransform>();

        private struct ViewTransform
        {
            public Vector3 Position;
            public Vector3 Scale;

            public ViewTransform(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
        }

        public ProcessGraphView()
        {
            StyleSheet styleSheet = Resources.Load<StyleSheet>("ProcessGraph");
            if (styleSheet != null) 
            {
                styleSheets.Add(styleSheet);
            }

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            SetupInstantiators();

            graphViewChanged += OnGraphChanged;
            serializeGraphElements += OnElementsSerialized;
            unserializeAndPaste += OnElementsPasted;
        }

        private void SetupInstantiators()
        {
            TypeCollection instantiatorTypes = GetTypesDerivedFrom<IStepNodeInstantiator>();
            foreach (Type instantiatorType in instantiatorTypes)
            {
                instantiators.Add((IStepNodeInstantiator)Activator.CreateInstance(instantiatorType));
            }
        }

        /// <summary>
        /// Updates visualization of the node selected in the step inspector.
        /// </summary>
        public void RefreshSelectedNode()
        {
            ProcessGraphNode node = nodes.ToList().Where(n => n is ProcessGraphNode).Select(n => n as ProcessGraphNode).Where(n => n.EntryPoint == currentChapter.ChapterMetadata.LastSelectedStep).FirstOrDefault();

            if(node != null)
            {
                RefreshNode(node);
            }
        }

        private void RefreshNode(ProcessGraphNode node)
        {
            node.Refresh();

            LinkNode(node);

            foreach (ProcessGraphNode leadingNode in GetLeadingNodes(node))
            {
                foreach (IStep output in leadingNode.Outputs)
                {
                    if (output != node)
                    {
                        continue;
                    }

                    Port port = leadingNode.outputContainer[Array.IndexOf(leadingNode.Outputs, output)].Q<Port>();

                    leadingNode.UpdateOutputPortName(port, node);
                }
            }
        }

        /// <inheritdoc/>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }


        /// <inheritdoc/>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            foreach (IStepNodeInstantiator instantiator in instantiators.Where(i => i.IsInNodeMenu).OrderBy(i => i.Priority))
            {
                evt.menu.AppendAction($"New/{instantiator.Name}", (status) =>
                {
                    IStep step = EntityFactory.CreateStep(instantiator.Name, contentViewContainer.WorldToLocal(status.eventInfo.mousePosition), instantiator.StepType);
                    currentChapter.Data.Steps.Add(step);
                    CreateStepNodeWithUndo(step);
                    GlobalEditorHandler.CurrentStepModified(step);                    
                }, instantiator.GetContextMenuStatus(evt.target, currentChapter));
            }

            evt.menu.AppendAction("Make group", (status) =>
            {
                MakeStepGroup(selection.Where(selected => selected is StepGraphNode).Cast<StepGraphNode>(), status);
            }, selection.Any(selected => selected is StepGraphNode) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            IContextMenuActions menuActions = evt.target as IContextMenuActions;

            if(menuActions != null )
            {
                menuActions.AddContextMenuActions(evt.menu);
                evt.menu.AppendSeparator();
            }

            base.BuildContextualMenu(evt);
        }

        private void MakeStepGroup(IEnumerable<StepGraphNode> stepNodes, DropdownMenuAction status)
        {
            IEnumerable<IStep> groupedSteps = stepNodes.Select(node => node.EntryPoint);
            IStepNodeInstantiator instantiator = instantiators.First(instantiator => instantiator is StepGroupNodeInstantiator);

            IStep stepGroup = EntityFactory.CreateStep(instantiator.Name, contentViewContainer.WorldToLocal(status.eventInfo.mousePosition), instantiator.StepType);
            ExecuteChapterBehavior behavior = stepGroup.Data.Behaviors.Data.Behaviors.First(behavior => behavior is ExecuteChapterBehavior) as ExecuteChapterBehavior;

            List<ITransition> leadingTransitions = currentChapter.Data.Steps
                       .Where(step => groupedSteps.Contains(step) == false)
                       .SelectMany(step => step.Data.Transitions.Data.Transitions).ToList();

            List<IStep> storedTargetSteps = new List<IStep>(leadingTransitions.Select(transition => transition.Data.TargetStep));
            Dictionary<IStep, List<IStep>> storedTransitions = new Dictionary<IStep, List<IStep>>();

            foreach (IStep step in groupedSteps)
            {
                storedTransitions.Add(step, new List<IStep>(step.Data.Transitions.Data.Transitions.Select(transition => transition.Data.TargetStep)));
            }

            RevertableChangesHandler.Do(new ProcessCommand(() =>
            {               
                foreach (IStep groupedStep in groupedSteps)
                {
                    if (currentChapter.Data.Steps.Remove(groupedStep))
                    {
                        List<ITransition> transitionsToStep = leadingTransitions.Where(transition => transition.Data.TargetStep == groupedStep).ToList();

                        if (currentChapter.Data.FirstStep == groupedStep)
                        {
                            currentChapter.Data.FirstStep = stepGroup;
                            behavior.Data.Chapter.Data.FirstStep = groupedStep;
                        }

                        foreach(ITransition transition in groupedStep.Data.Transitions.Data.Transitions)
                        {
                            if(groupedSteps.Contains(transition.Data.TargetStep) == false)
                            {
                                if (stepGroup.Data.Transitions.Data.Transitions[0].Data.TargetStep == null)
                                {
                                    stepGroup.Data.Transitions.Data.Transitions[0].Data.TargetStep = transition.Data.TargetStep;
                                }

                                transition.Data.TargetStep = null;
                            }
                        }

                        behavior.Data.Chapter.Data.Steps.Add(groupedStep);

                        if (behavior.Data.Chapter.Data.FirstStep == null && leadingTransitions.Count() > 0)
                        {
                            behavior.Data.Chapter.Data.FirstStep = groupedStep;
                        }

                        foreach (ITransition transition in transitionsToStep)
                        {
                            if (transition.Data.TargetStep == behavior.Data.Chapter.Data.FirstStep)
                            {
                                transition.Data.TargetStep = stepGroup;
                            }
                            else
                            {
                                transition.Data.TargetStep = null;
                            }
                        }
                    }
                }

                currentChapter.Data.Steps.Add(stepGroup);
                CreateStepNode(stepGroup);
                SetChapter(currentChapter);
            },
            () =>
            {
                foreach (IStep addedStep in behavior.Data.Chapter.Data.Steps)
                {
                    currentChapter.Data.Steps.Add(addedStep);
                }

                for(int i = 0; i < leadingTransitions.Count(); i++)
                {
                    leadingTransitions[i].Data.TargetStep = storedTargetSteps[i];
                }

                foreach(IStep step in storedTransitions.Keys)
                {
                    for(int i = 0; i < storedTransitions[step].Count(); i++)
                    {
                        step.Data.Transitions.Data.Transitions[i].Data.TargetStep = storedTransitions[step][i];
                    }
                }

                if (currentChapter.Data.FirstStep == stepGroup)
                {
                    currentChapter.Data.FirstStep = behavior.Data.Chapter.Data.FirstStep;
                }

                currentChapter.Data.Steps.Remove(stepGroup);
                SetChapter(currentChapter);
            }
            ));
        }

        /// <summary>
        /// Displays the specified chapter.
        /// </summary>        
        public void SetChapter(IChapter chapter)
        {
            IChapter previousChapter = GlobalEditorHandler.GetCurrentChapter();

            if (chapter != previousChapter)
            {
                if (previousChapter != null)
                {
                    if (storedViewTransforms.ContainsKey(previousChapter))
                    {
                        storedViewTransforms[previousChapter] = new ViewTransform(viewTransform.position, viewTransform.scale);
                    }
                    else
                    {
                        storedViewTransforms.Add(previousChapter, new ViewTransform(viewTransform.position, viewTransform.scale));
                    }
                }

                GlobalEditorHandler.SetCurrentChapter(chapter);

                if(storedViewTransforms.ContainsKey(chapter))
                {
                    viewTransform.position = storedViewTransforms[chapter].Position;
                    viewTransform.scale = storedViewTransforms[chapter].Scale;
                }
                else
                {
                    viewTransform.scale = Vector3.one;

                    if (contentRect.height > 0)
                    {
                        viewTransform.position = new Vector2(defaultViewTransform.x, (int)(contentRect.height / 2)) - chapter.ChapterMetadata.EntryNodePosition;
                    }
                    else
                    {
                        viewTransform.position = defaultViewTransform - chapter.ChapterMetadata.EntryNodePosition;
                    }
                }
            }

            currentChapter = chapter;

            nodes.ForEach(RemoveElement);
            edges.ForEach(RemoveElement);

            entryNode = new EntryPointNode();
            AddElement(entryNode);

            GenerateNodes(currentChapter);

            foreach (ProcessGraphNode node in nodes.ToList().Where(n => n is ProcessGraphNode).Select(n => n as ProcessGraphNode))
            {
                RefreshNode(node);
            }
        }

        private void OnElementsPasted(string operationName, string data)
        {
            IProcessSerializer serializer = EditorConfigurator.Instance.Serializer;
            IProcess clipboardProcess = null;

            try
            {
                clipboardProcess = serializer.ProcessFromByteArray(Encoding.UTF8.GetBytes(data));
            }
#pragma warning disable 168
            catch (JsonReaderException exception)
#pragma warning restore
            {
                EditorUtility.DisplayDialog("Excessive serialization depth", "It was not possible to paste the clipboard data as it contains too many nested entities.", "Ok");
                return;
            }

            IChapter storedChapter = currentChapter;
            Vector2 pasteOrigin = new Vector2
                (
                    clipboardProcess.Data.FirstChapter.Data.Steps.Select(step => step.StepMetadata.Position).Min(position => position.x),
                    clipboardProcess.Data.FirstChapter.Data.Steps.Select(step => step.StepMetadata.Position).Min(position => position.y)
                );

            RevertableChangesHandler.Do(new ProcessCommand(
            () =>
            {
                ClearSelection();

                foreach (IStep step in clipboardProcess.Data.FirstChapter.Data.Steps)
                {
                    foreach(ITransition transition in step.Data.Transitions.Data.Transitions.Where(transition => clipboardProcess.Data.FirstChapter.Data.Steps.Contains(transition.Data.TargetStep) == false))
                    {
                        transition.Data.TargetStep = null;
                    }

#if ENABLE_INPUT_SYSTEM
                    step.StepMetadata.Position += contentViewContainer.WorldToLocal(UnityEngine.InputSystem.Mouse.current.position.ReadValue() / EditorGUIUtility.pixelsPerPoint) - pasteOrigin;
#else
                    step.StepMetadata.Position += new Vector2(20, 20);
#endif
                    currentChapter.Data.Steps.Add(step);
                }

                IEnumerable<ProcessGraphNode> steps = GenerateNodes(clipboardProcess.Data.FirstChapter);

                foreach (ProcessGraphNode step in steps)
                {
                    AddToSelection(step);
                    RefreshNode(step);
                }
            },
            () =>
            {
                foreach (IStep step in clipboardProcess.Data.FirstChapter.Data.Steps)
                {
                    SetChapter(storedChapter);
                    DeleteStep(step);
                    SetChapter(currentChapter);
                }
            }
            ));
        }

        private string OnElementsSerialized(IEnumerable<GraphElement> elements)
        {
            IProcess clipboardProcess = EntityFactory.CreateProcess("Clipboard Process");

            clipboardProcess.Data.FirstChapter.Data.Steps = elements.Where(node => node is ProcessGraphNode)
                .Select(node => ((ProcessGraphNode)node).EntryPoint)
                .Where(entryPoint => entryPoint != null)
                .ToList();

            byte[] bytes = EditorConfigurator.Instance.Serializer.ProcessToByteArray(clipboardProcess.Clone());

            return Encoding.UTF8.GetString(bytes);
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {            
            if (change.elementsToRemove != null)
            {
                IEnumerable<Edge> removedEdges = change.elementsToRemove.Where(e => e is Edge).Select(e => e as Edge);
                IEnumerable<ProcessGraphNode> removedNodes = change.elementsToRemove.Where(e => e is ProcessGraphNode).Select(e => e as ProcessGraphNode);
                Dictionary<Edge, List<Port>> storedEdgeIO = new Dictionary<Edge, List<Port>>();
                Dictionary<ProcessGraphNode, List<ITransition>> incomingTransitions = new Dictionary<ProcessGraphNode, List<ITransition>>();
                IChapter storedChapter = currentChapter;

                foreach(ProcessGraphNode node in removedNodes)
                {
                    if (node.EntryPoint != null)
                    {
                        incomingTransitions.Add(node, currentChapter.Data.Steps.SelectMany(s => s.Data.Transitions.Data.Transitions).Where(transition => transition.Data.TargetStep == node.EntryPoint).ToList());
                    }
                }

                foreach(Edge edge in removedEdges)
                {
                    List<Port> nodes = new List<Port>() { edge.output, edge.input };
                    storedEdgeIO.Add(edge, nodes);                   
                }

                RevertableChangesHandler.Do(new ProcessCommand(
                    () =>
                    {
                        foreach (Edge edge in removedEdges)
                        {
                            ProcessGraphNode node = edge.output.node as ProcessGraphNode;

                            if (node == null)
                            {
                                continue;
                            }

                            if (node.IsEntryPoint)
                            {
                                currentChapter.Data.FirstStep = null;
                            }
                            else
                            {
                                node.SetOutput(node.outputContainer.IndexOf(edge.output), null);                                
                            }

                            node.UpdateOutputPortName(edge.output, null);
                        }

                        foreach (ProcessGraphNode node in removedNodes)
                        {
                            foreach (ITransition transition in incomingTransitions[node])
                            {
                                transition.Data.TargetStep = null;
                            }

                            node.RemoveFromChapter(currentChapter);                            
                            SetChapter(currentChapter);
                        }
                    },
                    () =>
                    {
                        SetChapter(storedChapter);

                        foreach (ProcessGraphNode node in removedNodes)
                        {
                            node.AddToChapter(storedChapter);
                            AddElement(node);

                            foreach (ITransition transition in incomingTransitions[node])
                            {
                                transition.Data.TargetStep = node.EntryPoint;
                            }
                        }

                        foreach (Edge edge in removedEdges)
                        {
                            Port outputPort = storedEdgeIO[edge][0];
                            ProcessGraphNode output = outputPort.node as ProcessGraphNode;
                            ProcessGraphNode input = storedEdgeIO[edge][1].node as ProcessGraphNode;

                            if (output == null || input == null)
                            {
                                continue;
                            }

                            if (output.IsEntryPoint)
                            {
                                storedChapter.Data.FirstStep = input.EntryPoint;
                            }
                            else
                            {
                                IStep targetStep = output.Outputs[output.outputContainer.IndexOf(outputPort)];

                                if (targetStep == null)
                                {
                                    output.SetOutput(output.outputContainer.IndexOf(outputPort), input.EntryPoint);
                                }
                            }

                            ((ProcessGraphNode)outputPort.node).UpdateOutputPortName(outputPort, input);
                            SetChapter(currentChapter);
                        }
                    }
                    ));
            }

            if(change.movedElements != null)
            {
                IEnumerable<ProcessGraphNode> movedNodes = change.movedElements.Where(e => e is ProcessGraphNode).Select(e => e as ProcessGraphNode);
                Dictionary<ProcessGraphNode, Vector2> storedPositions = new Dictionary<ProcessGraphNode, Vector2>();
                IChapter storedChapter = currentChapter;

                foreach (ProcessGraphNode node in movedNodes)
                {
                    storedPositions.Add(node, node.Position);
                }

                RevertableChangesHandler.Do(new ProcessCommand(
                    () =>
                    {
                        foreach(ProcessGraphNode node in movedNodes)
                        {
                            if (node.IsEntryPoint)
                            {
                                currentChapter.ChapterMetadata.EntryNodePosition = (node).GetPosition().position;
                            }
                            else
                            {
                                node.Position = node.GetPosition().position;
                            }
                        }
                    },
                    () =>
                    {
                        foreach (ProcessGraphNode node in storedPositions.Keys)
                        {
                            node.SetPosition(new Rect(storedPositions[node], node.contentRect.size));

                            if (node.IsEntryPoint)
                            {
                                storedChapter.ChapterMetadata.EntryNodePosition = storedPositions[node];
                            }
                            else
                            {
                                node.Position = storedPositions[node];
                            }                          
                        }

                        if (storedChapter != currentChapter)
                        {
                            SetChapter(storedChapter);
                        }
                    }
                    ));
            }

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    CreateEdgeWithUndo(edge);
                }
            }

            return change;
        }

        private void CreateEdgeWithUndo(Edge edge)
        {
            ProcessGraphNode targetNode = edge.input.node as ProcessGraphNode;

            if (targetNode == null)
            {
                Debug.LogError("Connected non-step node");
                return;
            }

            ProcessGraphNode startNode = edge.output.node as ProcessGraphNode;

            if (startNode == null)
            {
                Debug.LogError("Connected non-step node");
                return;
            }

            IChapter storedChapter = currentChapter;

            RevertableChangesHandler.Do(new ProcessCommand(
                () =>
                {
                    if (startNode.IsEntryPoint)
                    {
                        currentChapter.Data.FirstStep = targetNode.EntryPoint;
                        ((ProcessGraphNode)edge.output.node).UpdateOutputPortName(edge.output, targetNode);
                    }
                    else
                    {
                        startNode.SetOutput(startNode.outputContainer.IndexOf(edge.output), targetNode.EntryPoint);
                        ((ProcessGraphNode)edge.output.node).UpdateOutputPortName(edge.output, targetNode);
                    }
                },
                () =>
                {
                    if (startNode.IsEntryPoint)
                    {
                        storedChapter.Data.FirstStep = null;
                        ((ProcessGraphNode)edge.output.node).UpdateOutputPortName(edge.output, null);
                    }
                    else
                    {
                        startNode.SetOutput(startNode.outputContainer.IndexOf(edge.output), null);
                        ((ProcessGraphNode)edge.output.node).UpdateOutputPortName(edge.output, null);
                    }

                    RemoveElement(edge);

                    if(currentChapter != storedChapter)
                    {
                        SetChapter(storedChapter);
                    }
                }
                ));
        }

        private void DeleteStep(IStep step)
        {
            if (currentChapter.ChapterMetadata.LastSelectedStep == step)
            {
                currentChapter.ChapterMetadata.LastSelectedStep = null;
                GlobalEditorHandler.ChangeCurrentStep(null);
            }

            currentChapter.Data.Steps.Remove(step);
        }

        private void LinkNodes(Port output, Port input)
        {
            Edge edge = new Edge
            {
                output = output,
                input = input,
            };

            AddElement(edge);
            edge.input.Connect(edge);
            edge.output.Connect(edge);

            ((ProcessGraphNode)output.node).UpdateOutputPortName(output, input.node);
        }

        private IEnumerable<ProcessGraphNode> GenerateNodes(IChapter chapter)
        {
            return chapter.Data.Steps.Select(CreateStepNode).ToList();
        }

        private ProcessGraphNode FindStepNode(IStep step)
        {
            if(step == null)
            {
                return null;
            }

            return nodes.ToList().FirstOrDefault(n => n is ProcessGraphNode && ((ProcessGraphNode)n).EntryPoint == step) as ProcessGraphNode;
        }

        private void LinkNode(ProcessGraphNode node)
        {
            if(node.EntryPoint != null)
            {
                LinkStepNode(node.EntryPoint);
            }
            else if(node is EntryPointNode)
            {
                ProcessGraphNode firstNode = FindStepNode(currentChapter.Data.FirstStep);

                if (firstNode != null)
                {
                    LinkNodes(node.outputContainer[0].Query<Port>(), firstNode.inputContainer[0].Query<Port>());
                }
            }
        }

        private void LinkStepNode(IStep step)
        {
            foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
            {
                Port outputPort = FindStepNode(step).outputContainer[step.Data.Transitions.Data.Transitions.IndexOf(transition)] as Port;

                if (transition.Data.TargetStep != null && outputPort != null)
                {
                    ProcessGraphNode target = FindStepNode(transition.Data.TargetStep);
                    LinkNodes(outputPort, target.inputContainer[0].Query<Port>());
                }
            }
        }
      
        private void CreateStepNodeWithUndo(IStep step)
        {
            IChapter storedChapter = currentChapter;

            RevertableChangesHandler.Do(new ProcessCommand(
            () =>
            {
                CreateStepNode(step);                
            },
            () =>
            {
                DeleteStep(step);
                SetChapter(storedChapter);
            }
            ));
        }

        private ProcessGraphNode CreateStepNode(IStep step)
        {
            if(string.IsNullOrEmpty(step.StepMetadata.StepType))
            {
                step.StepMetadata.StepType = "default";
            }

            IStepNodeInstantiator instantiator = instantiators.FirstOrDefault(i => i.StepType == step.StepMetadata.StepType);

            if(instantiator == null)
            {
                Debug.LogError($"Impossible to find correct visualization for type '{step.StepMetadata.StepType}' used in step '{step.Data.Name}'. Things might not look as expected.");
                instantiator = instantiators.First(i => i.StepType == "default");
            }

            ProcessGraphNode node = instantiator.InstantiateNode(step);
            AddElement(node);
            return node;
        }

        private IEnumerable<ProcessGraphNode> GetLeadingNodes(ProcessGraphNode targetNode)
        {
            List<ProcessGraphNode> leadingNodes = new List<ProcessGraphNode>();

            if(targetNode.EntryPoint == null)
            {
                return leadingNodes;
            }

            foreach(Node node in nodes.ToList())
            {
                ProcessGraphNode processGraphNode = node as ProcessGraphNode;

                if(processGraphNode != null && processGraphNode.Outputs.Contains(targetNode.EntryPoint))
                {
                    leadingNodes.Add(processGraphNode);
                }
            }

            return leadingNodes;
        }
    }
}
