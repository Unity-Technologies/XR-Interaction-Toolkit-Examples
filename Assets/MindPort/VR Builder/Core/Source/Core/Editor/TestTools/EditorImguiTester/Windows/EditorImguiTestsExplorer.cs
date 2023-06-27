// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Utils;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Editor window which allows user to manage editor IMGUI tests.
    /// </summary>
    internal sealed class EditorImguiTestsExplorer : EditorWindow
    {
        private class TestMetadata
        {
            public TestState State { get; set; }
            public bool FoldedOut { get; set; }
        }

        private static bool IsUiDisabled
        {
            get
            {
                return EditorWindowTestRecorder.IsRecording;
            }
        }

        private static ReadOnlyCollection<KeyValuePair<string, IEnumerable<IEditorImguiTest>>> Tests
        {
            get
            {
                if (tests == null)
                {
                    Setup();
                }

                return new ReadOnlyCollection<KeyValuePair<string, IEnumerable<IEditorImguiTest>>>(tests);
            }
        }

        private static EditorWindowTestRecorder recorder;
        private IEditorImguiTest recordedTest;
        private static Dictionary<IEditorImguiTest, TestMetadata> testMetadatas = new Dictionary<IEditorImguiTest, TestMetadata>();
        private static IList<KeyValuePair<string, IEnumerable<IEditorImguiTest>>> tests;
        private static Dictionary<TestState, Texture2D> iconsForStates;

        private Vector2 scrollPosition;

        private IEditorImguiTest executedTest;
        private IEnumerator currentTestExecutionEnumerator;

        private IEnumerator<IEditorImguiTest> pendingTestsEnumerator;

        private static void Setup()
        {
            tests = ReflectionUtils
                .GetConcreteImplementationsOf<IEditorImguiTest>()
                .Select(type => Activator.CreateInstance(type, new object[0]))
                .Cast<IEditorImguiTest>()
                .GroupBy(test => test.GetType().Namespace)
                .ToDictionary(group => group.Key, group => (IEnumerable<IEditorImguiTest>)group)
                .ToList();

            testMetadatas = tests.SelectMany(group => group.Value).ToDictionary(test => test, test => new TestMetadata
            {
                State = TestState.Normal,
                FoldedOut = false
            });

            iconsForStates = new Dictionary<TestState, Texture2D>
            {
                {TestState.Normal, EditorGUIUtility.IconContent("TestNormal").image as Texture2D},
                {TestState.Pending, EditorGUIUtility.IconContent("TestStopwatch").image as Texture2D},
                {TestState.Failed, EditorGUIUtility.IconContent("TestFailed").image as Texture2D},
                {TestState.Passed, EditorGUIUtility.IconContent("TestPassed").image as Texture2D}
            };
        }

        [MenuItem("Tools/VR Builder/Developer/Editor IMGUI Tests Explorer", false, 81)]
        private static void ShowFromMenu()
        {
            GetWindow<EditorImguiTestsExplorer>();
        }

        private void UpdateCurrentTest()
        {
            testMetadatas[executedTest].State = TestState.Pending;
            bool isTestRunning;
            try
            {
                isTestRunning = currentTestExecutionEnumerator.MoveNext();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Test {0} failed: {1}", executedTest.GetType().GetNameWithNesting(), e.Message);
                testMetadatas[executedTest].State = TestState.Failed;
                isTestRunning = false;
            }

            if (isTestRunning)
            {
                return;
            }

            executedTest.Teardown();
            executedTest = null;
            currentTestExecutionEnumerator = null;
        }

        private void OnGUI()
        {
            if (tests == null)
            {
                Setup();
            }

            SetupTab();

            GUILayout.Space(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f);
            Rect pos = new Rect(EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.standardVerticalSpacing, 58f, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            if (GUI.Button(pos, "Run All"))
            {
                pendingTestsEnumerator = Tests.SelectMany(group => group.Value).GetEnumerator();
            }

            HandleTestExecution();
            DrawAllTestsData(pos);
            DrawTestsList();
            Repaint();
        }

        private void SetupTab()
        {
            TestState worstState;

            if (testMetadatas.Any(test => test.Value.State == TestState.Pending))
            {
                worstState = TestState.Pending;
            }
            else if (testMetadatas.Any(test => test.Value.State == TestState.Failed))
            {
                worstState = TestState.Failed;
            }
            else if (testMetadatas.Any(test => test.Value.State == TestState.Normal))
            {
                worstState = TestState.Normal;
            }
            else if (testMetadatas.Any(test => test.Value.State == TestState.Passed))
            {
                worstState = TestState.Passed;
            }
            else
            {
                worstState = TestState.Normal;
            }

            titleContent = new GUIContent("Editor Tests", iconsForStates[worstState]);
        }

        private void DrawTestsList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                foreach (KeyValuePair<string, IEnumerable<IEditorImguiTest>> group in Tests.OrderBy(group => group.Key))
                {
                    EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    {
                        foreach (IEditorImguiTest test in group.Value.OrderBy(test => test.GetType().Name))
                        {
                            DrawTestView(test);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAllTestsData(Rect pos)
        {
            if (executedTest == null)
            {
                int passed = testMetadatas.Values.Count(metadata => metadata.State == TestState.Passed);
                int failed = testMetadatas.Values.Count(metadata => metadata.State == TestState.Failed);
                int normal = testMetadatas.Values.Count(metadata => metadata.State == TestState.Normal);

                pos.x += pos.width;
                pos.width = pos.height;
                DrawTestStateIndicator(pos.position, TestState.Passed);

                pos.x += pos.width;
                pos.width = EditorStyles.label.CalcSize(new GUIContent(passed.ToString())).x;
                EditorGUI.LabelField(pos, passed.ToString());

                pos.x += pos.width;
                pos.width = pos.height;
                DrawTestStateIndicator(pos.position, TestState.Failed);

                pos.x += pos.width;
                pos.width = EditorStyles.label.CalcSize(new GUIContent(failed.ToString())).x;
                EditorGUI.LabelField(pos, failed.ToString());

                pos.x += pos.width;
                pos.width = pos.height;
                DrawTestStateIndicator(pos.position, TestState.Normal);

                pos.x += pos.width;
                pos.width = EditorStyles.label.CalcSize(new GUIContent(normal.ToString())).x;
                EditorGUI.LabelField(pos, normal.ToString());
            }
            else
            {
                pos.x += pos.width;
                pos.width = pos.height;
                DrawTestStateIndicator(pos.position, TestState.Pending);
            }
        }

        private void DrawTestView(IEditorImguiTest test)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // GUILayout does not support indentation so draw one in front of GUILayoutButton manually.
                GUILayout.Space(20f);

                DrawRunTestButton(test);

                EditorGUILayout.BeginVertical();
                {
                    // Draw foldout.
                    GUIStyle foldout = new GUIStyle(EditorStyles.foldout);
                    if (testMetadatas[test].FoldedOut)
                    {
                        foldout.fontStyle = FontStyle.Bold;
                    }

                    testMetadatas[test].FoldedOut = EditorGUILayout.Foldout(testMetadatas[test].FoldedOut, test.GetType().Name, foldout);

                    // Draw a test state indicator to the left of the foldout.
                    Rect foldoutRect = GUILayoutUtility.GetLastRect();
                    DrawTestStateIndicator(new Vector2(61f, foldoutRect.y), testMetadatas[test].State);

                    // Draw foldout contents, if necessary.
                    if (testMetadatas[test].FoldedOut)
                    {
                        DrawTestDescription(test);
                        if (DrawRecordTestButton(test))
                        {
                            // Workaround for EndHorizontal.
                            return;
                        }
                        GUILayoutUtility.GetRect(3f, 3f);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTestStateIndicator(Vector2 position, TestState state)
        {
            GUI.DrawTexture(new Rect(position, new Vector2(EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight)), iconsForStates[state]);
        }

        private void DrawRunTestButton(IEditorImguiTest test)
        {
            EditorGUI.BeginDisabledGroup(IsUiDisabled);
            {
                if (GUILayout.Button("Run", GUILayout.Width(40f)))
                {
                    StartTest(test);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void StartTest(IEditorImguiTest test)
        {
            executedTest = test;
            executedTest.Finished += OnCurrentTestFinished;
            currentTestExecutionEnumerator = test.Test();
        }

        private bool DrawRecordTestButton(IEditorImguiTest test)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (EditorWindowTestRecorder.IsRecording && recordedTest == test)
                {
                    if (GUILayout.Button("Save the record"))
                    {
                        recorder.SendEvent(EditorGUIUtility.CommandEvent("SaveAndTerminate"));
                        return true;
                    }

                    if (GUILayout.Button("Abort", GUILayout.Width(60f)))
                    {
                        recorder.SendEvent(EditorGUIUtility.CommandEvent("Abort"));
                        return true;
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(IsUiDisabled);
                    {
                        if (GUILayout.Button("Record"))
                        {
                            testMetadatas[test].State = TestState.Normal;
                            StartRecording(test);
                            return true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndHorizontal();
            return false;
        }

        private Rect startRecordPosition;

        private void StartRecording(IEditorImguiTest test)
        {
            foreach (EditorWindowTestRecorder w in Resources.FindObjectsOfTypeAll<EditorWindowTestRecorder>())
            {
                w.Close();
            }

            recordedTest = test;

            recorder = CreateInstance<EditorWindowTestRecorder>();
            recorder.ShowUtility();

            recorder.StartRecording(test);
        }

        private void DrawTestDescription(IEditorImguiTest test)
        {
            GUIStyle multilineLabel = new GUIStyle(EditorStyles.label) {wordWrap = true};
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Given", EditorStyles.label, GUILayout.Width(60f));
                EditorGUILayout.LabelField(test.GivenDescription, multilineLabel);
            }
            EditorGUILayout.EndHorizontal();

            GUILayoutUtility.GetRect(3f, 3f);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("When", EditorStyles.label, GUILayout.Width(60f));
                EditorGUILayout.LabelField(test.WhenDescription, multilineLabel);
            }
            EditorGUILayout.EndHorizontal();

            GUILayoutUtility.GetRect(3f, 3f);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Then", EditorStyles.label, GUILayout.Width(60f));
                EditorGUILayout.LabelField(test.ThenDescription, multilineLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnCurrentTestFinished(object sender, EditorImguiTestFinishedEventArgs args)
        {
            testMetadatas[executedTest].State = args.Result;
            executedTest.Finished -= OnCurrentTestFinished;
        }

        private void HandleTestExecution()
        {
            if (executedTest == null)
            {
                if (pendingTestsEnumerator != null)
                {
                    if (pendingTestsEnumerator.MoveNext())
                    {
                        StartTest(pendingTestsEnumerator.Current);
                    }
                    else
                    {
                        pendingTestsEnumerator = null;
                    }
                }
            }
            else
            {
                UpdateCurrentTest();
            }
        }
    }
}
