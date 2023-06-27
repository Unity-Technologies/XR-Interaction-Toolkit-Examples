// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Editor.Configuration;
using VRBuilder.Editor.UI.Graphics;
using VRBuilder.Editor.UndoRedo;
using VRBuilder.Editor.Utils;
using UnityEngine;

namespace VRBuilder.Editor.UI.Windows
{
    internal class ChapterRepresentation
    {
        public EditorGraphics Graphics { get; private set; }

        protected IChapter CurrentChapter { get; set; }
        private StepNode lastSelectedStepNode;

        protected WorkflowEditorGrid Grid { get; set; }
        private float gridCellSize = 10f;

        protected bool IsUpdated { get; set; }

        public Rect BoundingBox
        {
            get { return Graphics.BoundingBox; }
        }

        public ChapterRepresentation()
        {
            Graphics = new EditorGraphics(WorkflowEditorColorPalette.GetDefaultPalette());
        }

        private void SetupNode(EditorNode node, Action<Vector2> setPositionInModel)
        {
            Vector2 positionBeforeDrag = node.Position;
            Vector2 deltaOnPointerDown = Vector2.zero;

            node.GraphicalEventHandler.PointerDown += (sender, args) =>
            {
                positionBeforeDrag = node.Position;
                deltaOnPointerDown = node.Position - args.PointerPosition;
            };

            node.GraphicalEventHandler.PointerUp += (sender, args) =>
            {
                if (Mathf.Abs((positionBeforeDrag - node.Position).sqrMagnitude) < 0.001f)
                {
                    return;
                }

                Vector2 positionAfterDrag = node.Position;
                Vector2 closuredPositionBeforeDrag = positionBeforeDrag;

                RevertableChangesHandler.Do(new ProcessCommand(() =>
                {
                    setPositionInModel(positionAfterDrag);
                    MarkToRefresh();
                }, () =>
                {
                    setPositionInModel(closuredPositionBeforeDrag);
                    MarkToRefresh();
                }));
            };

            node.GraphicalEventHandler.PointerDrag += (sender, args) =>
            {
                SetNewPositionOnGrid(node, args.PointerPosition, deltaOnPointerDown);
            };
        }

        /// Clamps the position of the node on the background grid.
        private void SetNewPositionOnGrid(EditorNode node, Vector2 position, Vector2 delta)
        {
            // Add original delta pointer position onto the absolute position of the node.
            node.Position = position + delta;
            Vector2 newPos = node.RelativePosition;

            // Calculate x and y offset dependent on the size of the grid cells.
            float xOffset = newPos.x % gridCellSize;
            float addedX = xOffset < gridCellSize / 2 ? -xOffset : gridCellSize - xOffset;

            float yOffset = newPos.y % gridCellSize;
            float addedY = yOffset < gridCellSize / 2 ? -yOffset : gridCellSize - yOffset;

            // Add offsets and subtract bounding box offsets
            newPos += new Vector2(addedX, addedY);
            newPos -= new Vector2(node.LocalBoundingBox.x % gridCellSize, node.LocalBoundingBox.y % gridCellSize);

            node.RelativePosition = newPos;
        }

        private void DeleteStepWithUndo(IStep step, StepNode ownerNode)
        {
            IList<ITransition> incomingTransitions = CurrentChapter.Data.Steps.SelectMany(s => s.Data.Transitions.Data.Transitions).Where(transition => transition.Data.TargetStep == step).ToList();

            bool wasFirstStep = step == CurrentChapter.Data.FirstStep;

            RevertableChangesHandler.Do(new ProcessCommand(
                () =>
                {
                    foreach (ITransition transition in incomingTransitions)
                    {
                        transition.Data.TargetStep = null;
                    }

                    DeleteStep(step);

                    if (wasFirstStep)
                    {
                        CurrentChapter.Data.FirstStep = null;
                    }
                },
                () =>
                {
                    AddStep(step);

                    if (wasFirstStep)
                    {
                        CurrentChapter.Data.FirstStep = step;
                    }

                    foreach (ITransition transition in incomingTransitions)
                    {
                        transition.Data.TargetStep = step;
                    }

                    SelectStepNode(ownerNode);
                }
            ));
        }

        private StepNode CreateNewStepNode(IStep step)
        {
            StepNode node = new StepNode(Graphics, CurrentChapter, step);

            node.GraphicalEventHandler.ContextClick += (sender, args) =>
            {
                TestableEditorElements.DisplayContextMenu(new List<TestableEditorElements.MenuOption>
                {
                    new TestableEditorElements.MenuItem(new GUIContent("Copy"), false, () =>
                    {
                        CopyStep(step);
                    }),
                    new TestableEditorElements.MenuItem(new GUIContent("Cut"), false, () =>
                    {
                        CutStep(step, node);
                    }),
                    new TestableEditorElements.MenuItem(new GUIContent("Delete"), false, () =>
                    {
                        DeleteStepWithUndo(step, node);
                    })
                });
            };

            node.GraphicalEventHandler.PointerDown += (sender, args) =>
            {
                UserSelectStepNode(node);
            };

            node.RelativePositionChanged += (sender, args) =>
            {
                node.Step.StepMetadata.Position = node.Position;
            };

            node.GraphicalEventHandler.PointerUp += (sender, args) =>
            {
                Graphics.CalculateBoundingBox();
            };

            // ReSharper disable once ImplicitlyCapturedClosure
            node.GraphicalEventHandler.PointerDown += (sender, args) => UserSelectStepNode(node);

            node.CreateTransitionButton.GraphicalEventHandler.PointerClick += (sender, args) =>
            {
                ITransition transition = EntityFactory.CreateTransition();

                RevertableChangesHandler.Do(new ProcessCommand(
                    () =>
                    {
                        step.Data.Transitions.Data.Transitions.Add(transition);
                        MarkToRefresh();
                    },
                    () =>
                    {
                        step.Data.Transitions.Data.Transitions.Remove(transition);
                        MarkToRefresh();
                    }
                ));
            };

            if (CurrentChapter.ChapterMetadata.LastSelectedStep == step)
            {
                SelectStepNode(node);
            }

            SetupNode(node, position => node.Step.StepMetadata.Position = position);

            return node;
        }

        private void SelectStepNode(StepNode stepNode)
        {
            IStep step = stepNode == null ? null : stepNode.Step;

            if (lastSelectedStepNode != null)
            {
                lastSelectedStepNode.IsLastSelectedStep = false;
            }

            lastSelectedStepNode = stepNode;
            CurrentChapter.ChapterMetadata.LastSelectedStep = step;

            if (stepNode != null)
            {
                stepNode.IsLastSelectedStep = true;
            }

            GlobalEditorHandler.ChangeCurrentStep(step);
        }

        private void UserSelectStepNode(StepNode stepNode)
        {
            SelectStepNode(stepNode);
            Graphics.BringToTop(stepNode);
            GlobalEditorHandler.StartEditingStep();
        }

        private void MarkToRefresh()
        {
            IsUpdated = false;
        }

        private EntryNode CreateEntryNode(IChapter chapter)
        {
            EntryNode entryNode = new EntryNode(Graphics);

            entryNode.IsDragging = false;

            ExitJoint joint = new ExitJoint(Graphics, entryNode)
            {
                RelativePosition = new Vector2(entryNode.LocalBoundingBox.xMax, entryNode.LocalBoundingBox.center.y),
            };

            entryNode.ExitJoints.Add(joint);

            entryNode.Position = chapter.ChapterMetadata.EntryNodePosition;

            entryNode.RelativePositionChanged += (sender, args) =>
            {
                chapter.ChapterMetadata.EntryNodePosition = entryNode.Position;
            };

            entryNode.GraphicalEventHandler.PointerUp += (sender, args) =>
            {
                entryNode.IsDragging = false;
                Graphics.CalculateBoundingBox();
            };

            entryNode.GraphicalEventHandler.PointerDown += (sender, args) =>
            {
                entryNode.IsDragging = true;
                Graphics.CalculateBoundingBox();
            };

            entryNode.GraphicalEventHandler.ContextClick += (sender, args) =>
            {
                if (chapter.Data.FirstStep == null)
                {
                    return;
                }

                TestableEditorElements.DisplayContextMenu(new List<TestableEditorElements.MenuOption>
                {
                    new TestableEditorElements.MenuItem(new GUIContent("Delete transition"), false, () =>
                    {
                        IStep firstStep = chapter.Data.FirstStep;

                        RevertableChangesHandler.Do(new ProcessCommand(() =>
                            {
                                chapter.Data.FirstStep = null;
                                MarkToRefresh();
                            },
                            () =>
                            {
                                chapter.Data.FirstStep = firstStep;
                                MarkToRefresh();
                            }
                        ));
                    })
                });
            };

            joint.GraphicalEventHandler.PointerDrag += (sender, args) =>
            {
                joint.DragDelta = args.PointerPosition - joint.Position;
            };

            joint.GraphicalEventHandler.PointerUp += (sender, args) =>
            {
                joint.DragDelta = Vector2.zero;
                IStep oldStep = chapter.Data.FirstStep;

                if (TryGetStepForTransitionDrag(args.PointerPosition, out IStep target) == false)
                {
                    DisplayContextMenu(args.PointerPosition, joint);
                    return;
                }

                RevertableChangesHandler.Do(new ProcessCommand(() =>
                    {
                        chapter.Data.FirstStep = target;
                        MarkToRefresh();
                    },
                    () =>
                    {
                        chapter.Data.FirstStep = oldStep;
                        MarkToRefresh();
                    }
                ));

                joint.DragDelta = Vector2.zero;
            };

            SetupNode(entryNode, position => chapter.ChapterMetadata.EntryNodePosition = position);

            return entryNode;
        }

        private void SetupTransitions(IChapter chapter, EntryNode entryNode, IDictionary<IStep, StepNode> stepNodes)
        {
            if (chapter.Data.FirstStep != null)
            {
                CreateNewTransition(entryNode.ExitJoints.First(), stepNodes[chapter.Data.FirstStep].EntryJoints.First());
            }

            foreach (IStep step in stepNodes.Keys)
            {
                foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
                {
                    ExitJoint joint = stepNodes[step].AddExitJoint();
                    if (transition.Data.TargetStep != null)
                    {
                        StepNode target = stepNodes[transition.Data.TargetStep];
                        CreateNewTransition(joint, target.EntryJoints.First());
                    }

                    IStep closuredStep = step;
                    ITransition closuredTransition = transition;
                    int transitionIndex = step.Data.Transitions.Data.Transitions.IndexOf(closuredTransition);

                    joint.GraphicalEventHandler.PointerDrag += (sender, args) =>
                    {
                        joint.DragDelta = args.PointerPosition - joint.Position;
                    };

                    joint.GraphicalEventHandler.PointerUp += (sender, args) =>
                    {
                        joint.DragDelta = Vector2.zero;
                        IStep oldStep = closuredTransition.Data.TargetStep;

                        if (TryGetStepForTransitionDrag(args.PointerPosition, out IStep targetStep) == false)
                        {
                            DisplayContextMenu(args.PointerPosition, joint);
                            return;
                        }

                        RevertableChangesHandler.Do(new ProcessCommand(() =>
                            {
                                closuredTransition.Data.TargetStep = targetStep;
                                SelectStepNode(stepNodes[closuredStep]);
                                MarkToRefresh();
                            },
                            () =>
                            {
                                closuredTransition.Data.TargetStep = oldStep;
                                SelectStepNode(stepNodes[closuredStep]);
                                MarkToRefresh();
                            }
                        ));
                    };

                    joint.GraphicalEventHandler.ContextClick += (sender, args) =>
                    {
                        TestableEditorElements.DisplayContextMenu(new List<TestableEditorElements.MenuOption>
                        {
                            new TestableEditorElements.MenuItem(new GUIContent("Delete transition"), false, () =>
                            {
                                bool isLast = closuredStep.Data.Transitions.Data.Transitions.Count == 1;
                                RevertableChangesHandler.Do(new ProcessCommand(() =>
                                    {
                                        closuredStep.Data.Transitions.Data.Transitions.Remove(closuredTransition);
                                        if (isLast)
                                        {
                                            closuredStep.Data.Transitions.Data.Transitions.Add(EntityFactory.CreateTransition());
                                        }

                                        MarkToRefresh();
                                    },
                                    () =>
                                    {
                                        if (isLast)
                                        {
                                            closuredStep.Data.Transitions.Data.Transitions.RemoveAt(0);
                                        }

                                        closuredStep.Data.Transitions.Data.Transitions.Insert(transitionIndex, closuredTransition);
                                        MarkToRefresh();
                                    }
                                ));
                            })
                        });
                    };
                }
            }
        }

        private bool TryGetStepForTransitionDrag(Vector2 pointerPosition, out IStep step)
        {
            step = null;

            GraphicalElement elementUnderCursor = Graphics.GetGraphicalElementWithHandlerAtPoint(pointerPosition).FirstOrDefault();

            if (elementUnderCursor is EntryJoint endJoint)
            {
                if (endJoint.Parent is StepNode stepNode)
                {
                    step = stepNode.Step;
                }

                return true;
            }
            else if (elementUnderCursor is StepNode stepNode)
            {
                step = stepNode.Step;
                return true;
            }
            else
            {
                return elementUnderCursor != null;
            }
        }

        private IDictionary<IStep, StepNode> SetupSteps(IChapter chapter)
        {
            return chapter.Data.Steps.OrderBy(step => step == chapter.ChapterMetadata.LastSelectedStep).ToDictionary(step => step, CreateNewStepNode);
        }

        private void DeleteStep(IStep step)
        {
            if (CurrentChapter.ChapterMetadata.LastSelectedStep == step)
            {
                CurrentChapter.ChapterMetadata.LastSelectedStep = null;
                GlobalEditorHandler.ChangeCurrentStep(null);
            }

            CurrentChapter.Data.Steps.Remove(step);
            MarkToRefresh();
        }

        private void AddStep(IStep step)
        {
            CurrentChapter.Data.Steps.Add(step);

            MarkToRefresh();
        }

        private void CreateNewTransition(ExitJoint from, EntryJoint to)
        {
            TransitionElement transitionElement = new TransitionElement(Graphics, from, to);
            transitionElement.RelativePosition = Vector2.zero;
        }

        private void AddStepWithUndo(IStep step)
        {
            RevertableChangesHandler.Do(new ProcessCommand(() =>
                {
                    AddStep(step);
                    CurrentChapter.ChapterMetadata.LastSelectedStep = step;
                },
                () =>
                {
                    DeleteStep(step);
                }
            ));
        }

        private void HandleCanvasContextClick(object sender, PointerGraphicalElementEventArgs e)
        {
            DisplayContextMenu(e.PointerPosition);
        }

        private void DisplayContextMenu(Vector2 pointerPosition, ExitJoint joint = null)
        {
            IList<TestableEditorElements.MenuOption> options = new List<TestableEditorElements.MenuOption>();

            options.Add(new TestableEditorElements.MenuItem(new GUIContent("Add step"), false, () =>
            {
                IStep step = EntityFactory.CreateStep("New Step");
                step.StepMetadata.Position = pointerPosition;
                AddStepWithUndo(step);
                HandleTransition(joint, step);
            }));

            if (SystemClipboard.IsStepInClipboard())
            {
                options.Add(new TestableEditorElements.MenuItem(new GUIContent("Paste step"), false, () =>
                {
                    Paste(pointerPosition, joint);
                }));
            }
            else
            {
                options.Add(new TestableEditorElements.DisabledMenuItem(new GUIContent("Paste step")));
            }

            TestableEditorElements.DisplayContextMenu(options);
        }

        private void HandleTransition(ExitJoint exitJoint, IStep targetStep)
        {
            if (exitJoint != null)
            {
                if (exitJoint.Parent is EntryNode)
                {
                    IStep oldStep = CurrentChapter.Data.FirstStep;

                    RevertableChangesHandler.Do(new ProcessCommand(() =>
                    {
                        CurrentChapter.Data.FirstStep = targetStep;
                        MarkToRefresh();
                    },
                    () =>
                    {
                        CurrentChapter.Data.FirstStep = oldStep;
                        MarkToRefresh();
                    }
                    ));
                }
                else if (exitJoint.Parent is StepNode)
                {
                    StepNode stepNode = exitJoint.Parent as StepNode;
                    int index = stepNode.ExitJoints.IndexOf(exitJoint);
                    ITransition transition = stepNode.Step.Data.Transitions.Data.Transitions[index];
                    IStep oldStep = transition.Data.TargetStep;

                    RevertableChangesHandler.Do(new ProcessCommand(() =>
                    {
                        transition.Data.TargetStep = targetStep;
                        SelectStepNode(stepNode);
                        MarkToRefresh();
                    },
                    () =>
                    {
                        transition.Data.TargetStep = oldStep;
                        SelectStepNode(stepNode);
                        MarkToRefresh();
                    }
                ));
                }
            }
        }

        public void SetChapter(IChapter chapter)
        {
            if (chapter != GlobalEditorHandler.GetCurrentChapter())
            {
                GlobalEditorHandler.SetCurrentChapter(chapter);
            }

            CurrentChapter = chapter;

            Graphics.Reset();

            Grid = new WorkflowEditorGrid(Graphics, gridCellSize);

            Graphics.Canvas.ContextClick += HandleCanvasContextClick;

            EntryNode entryNode = CreateEntryNode(chapter);
            IDictionary<IStep, StepNode> stepNodes = SetupSteps(chapter);
            SetupTransitions(chapter, entryNode, stepNodes);

            Graphics.CalculateBoundingBox();

            if (EditorConfigurator.Instance.Validation.IsAllowedToValidate())
            {
                EditorConfigurator.Instance.Validation.Validate(CurrentChapter.Data, GlobalEditorHandler.GetCurrentProcess(), null);
            }
        }

        public virtual void HandleEvent(Event current, Rect windowRect)
        {
            if (IsUpdated == false)
            {
                SetChapter(CurrentChapter);
                IsUpdated = true;
            }

            Grid.SetSize(windowRect);

            Graphics.HandleEvent(current, windowRect);
        }

        private bool CopyStep(IStep step)
        {
            if (step == null)
            {
                return false;
            }

            try
            {
                SystemClipboard.CopyStep(step);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Copies the selected step into the system's copy buffer.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool CopySelected()
        {
            IStep step = CurrentChapter.ChapterMetadata.LastSelectedStep;
            return CopyStep(step);
        }

        private bool CutStep(IStep step, StepNode owner)
        {
            if (CopyStep(step))
            {
                DeleteStepWithUndo(step, owner);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cuts the selected step into the system's copy buffer from the chapter.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool CutSelected()
        {
            IStep step = CurrentChapter.ChapterMetadata.LastSelectedStep;
            return CutStep(step, lastSelectedStepNode);
        }

        /// <summary>
        /// Pastes the step from the system's copy buffer into the chapter at given <paramref name="position"/>.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool Paste(Vector2 position, ExitJoint connection = null)
        {
            IStep step;
            try
            {
                step = SystemClipboard.PasteStep();

                if (step == null)
                {
                    return false;
                }

                step.Data.SetName("Copy of " + step.Data.Name);

                step.StepMetadata.Position = position - new Vector2(0f, step.Data.Transitions.Data.Transitions.Count * 20f / 2f);
            }
            catch
            {
                return false;
            }

            AddStepWithUndo(step);
            if(connection != null)
            {
                HandleTransition(connection, step);
            }

            return true;
        }

        /// <summary>
        /// Deletes the selected step from the chapter.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool DeleteSelected()
        {
            IStep step = CurrentChapter.ChapterMetadata.LastSelectedStep;
            if (step == null)
            {
                return false;
            }

            DeleteStepWithUndo(step, lastSelectedStepNode);
            return true;
        }
    }
}
