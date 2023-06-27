/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.Voice.Hub.UIComponents;
using Meta.Voice.Hub.Utilities;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub
{
    public class MetaHub : EditorWindow
    {
        [SerializeField] private Texture2D _icon;
        [SerializeField] private Texture2D _logoImage;

        private int _leftPanelWidth = 200;
        
        private List<string> _contextFilter = new List<string>();
        
        private List<MetaHubContext> _contexts = new List<MetaHubContext>();
        private Dictionary<string, MetaHubContext> _contextMap = new Dictionary<string, MetaHubContext>();

        private List<PageGroup> _pageGroups = new List<PageGroup>();
        private Dictionary<MetaHubContext, PageGroup> _pageGroupMap = new Dictionary<MetaHubContext, PageGroup>();

        public MetaHubContext PrimaryContext
        {
            get
            {
                if (ContextFilter.Count > 0)
                {
                    var filter = ContextFilter.First();
                    if (_contextMap.TryGetValue(filter, out var context))
                    {
                        return context;
                    } 
                }
                

                return _contexts[0];
            }
        }
        
        public GUIContent TitleContent => new GUIContent(PrimaryContext.Title, PrimaryContext.Icon);
        public Texture2D LogoImage => PrimaryContext.LogoImage ? PrimaryContext.LogoImage : _logoImage;

        public const string DEFAULT_CONTEXT = "";
        
        public virtual List<string> ContextFilter => _contextFilter;
        public string SelectedPage { get; set; } = "";

        private PageGroup _rootPageGroup;

        private class PageGroup
        {
            private MetaHubContext _context;
            private List<PageReference> _pages = new List<PageReference>();
            private HashSet<string> _addedPages = new HashSet<string>();
            private FoldoutHierarchy<PageReference> _foldoutHierarchy = new FoldoutHierarchy<PageReference>();
            private readonly Action<PageReference> _onDrawPage;

            public MetaHubContext Context => _context;
            public IEnumerable<PageReference> Pages => _pages;
            public int PageCount => _pages.Count;
            public FoldoutHierarchy<PageReference> Hierarchy => _foldoutHierarchy;

            public PageGroup(MetaHubContext context, Action<PageReference> onDrawPage)
            {
                _context = context;
                _onDrawPage = onDrawPage;
            }

            public void AddPage(PageReference page)
            {
                var pageId = page.PageId;
                if (!_addedPages.Contains(pageId))
                {
                    _addedPages.Add(pageId);
                    _pages.Add(page);
                    var prefix = page.info.Prefix?.Trim(new char[] { '/' });
                    if (prefix.Length > 0)
                    {
                        prefix += "/";
                    }
                    var path = "/" + prefix + page.info.Name;
                    _foldoutHierarchy.Add(path, new FoldoutHierarchyItem<PageReference> {
                        path = path, 
                        item = page,
                        onDraw = _onDrawPage
                    });
                }
            }

            public void Sort()
            {
                Sort(_pages);
            }

            public void Sort(List<PageReference> pages)
            {
                pages.Sort((a, b) =>
                {
                    int compare = a.info.Priority.CompareTo(b.info.Priority);
                    if (compare == 0) compare = string.Compare(a.info.Name, b.info.Name);
                    return compare;
                });
                _foldoutHierarchy = new FoldoutHierarchy<PageReference>();
                foreach (var page in _pages)
                {
                    var path = "/" + page.info.Prefix + page.info.Name;
                    _foldoutHierarchy.Add(path, new FoldoutHierarchyItem<PageReference> {
                        path = path, 
                        item = page,
                        onDraw = _onDrawPage
                    });
                }
            }
        }

        private struct PageReference
        {
            public IMetaHubPage page;
            public IPageInfo info;
            public string PageId => info.Context + "::" + info.Name;
        }

        private string _searchString = "";
        private IMetaHubPage _selectedPage;
        private Vector2 _scroll;
        private Vector2 _leftScroll;

        private void OnEnable()
        {
            UpdateContextFilter();

            minSize = new Vector2(400, 400);
        }

        public void UpdateContextFilter()
        {
            
            if(null == _rootPageGroup) _rootPageGroup = new PageGroup(null, DrawPageEntry);
            _contexts = ContextFinder.FindAllContextAssets<MetaHubContext>();
            _contexts.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            foreach (var context in _contexts)
            {
                _contextMap[context.Name] = context;
                var pageGroup = new PageGroup(context, DrawPageEntry);
                if (!_pageGroupMap.ContainsKey(context))
                {
                    _pageGroups.Add(pageGroup);
                    _pageGroupMap[context] = pageGroup;

                    foreach (var soPage in context.ScriptableObjectReflectionPages)
                    {
                        var pages = PageFinder.FindPages(soPage.scriptableObjectType);
                        foreach (var so in pages)
                        {
                            var page = new ScriptableObjectPage(so, context.Name, prefix: soPage.namePrefix, priority: soPage.priorityModifier);
                            AddPage(new PageReference
                            {
                                page = page,
                                info = page
                            });
                        }
                    }
                }
            }

            foreach (var page in ContextFinder.FindAllContextAssets<MetaHubPage>())
            {
                AddPage(new PageReference
                {
                    page = page,
                    info = page
                });
            }

            foreach (var pageType in PageFinder.FindPages())
            {
                var pageInfo = PageFinder.GetPageInfo(pageType);

                if (pageInfo is MetaHubPageScriptableObjectAttribute)
                {
                    var pages = PageFinder.FindPages(pageType);
                    foreach (var page in pages)
                    {
                        var soPage = new ScriptableObjectPage(page, pageInfo);
                        AddPage(new PageReference
                        {
                            page = soPage,
                            info = soPage
                        });
                    }
                }
                else
                {
                    IMetaHubPage page;
                    if (pageType.IsSubclassOf(typeof(ScriptableObject)))
                    {
                        page = (IMetaHubPage) ScriptableObject.CreateInstance(pageType);
                    }
                    else
                    {
                        page = Activator.CreateInstance(pageType) as IMetaHubPage;
                    }
                    if(page is IPageInfo info) AddPage(new PageReference { page = page, info = info});
                    else AddPage(new PageReference { page = page, info = pageInfo});

                    var method = page.GetType().GetMethod("OnEnable", BindingFlags.Default | BindingFlags.Public);
                    method?.Invoke(page, new object[0]);
                }
            }

            // Sort the pages by priority then alpha
            foreach (var group in _pageGroupMap.Values)
            {
                group.Sort();
            }
        }

        private void AddPage(PageReference page)
        {
            if (string.IsNullOrEmpty(page.info.Context)) _rootPageGroup.AddPage(page);
            else _pageGroupMap[_contextMap[page.info.Context]].AddPage(page);
        }

        protected virtual void OnGUI()
        {
            titleContent = TitleContent;
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _searchString = EditorGUILayout.TextField(_searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
            GUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));

            var logo = LogoImage;
            // Draw logo image
            if (logo)
            {
                float aspectRatio = logo.width / (float) logo.height;
                GUILayout.Box(logo, GUILayout.Width(_leftPanelWidth), GUILayout.Height(_leftPanelWidth / aspectRatio));
            }

            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            DrawPageGroup(_rootPageGroup);
            foreach (var context in _pageGroups)
            {
                DrawPageGroup(context);
            }
            GUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawPageGroup(PageGroup group)
        {
            if (!IsGroupVisible(group)) return;

            var searchMatchedGroupContext = ContextFilter.Count != 1 && IsGroupInSearch(group);

            List<PageReference> pages = new List<PageReference>();
            if (!string.IsNullOrEmpty(_searchString) && !searchMatchedGroupContext)
            {
                foreach (var page in group.Pages)
                {
                    if (PageInSearch(page))
                    {
                        pages.Add(page);
                    }
                }
            }
            
            if (ContextFilter.Count == 0 && (string.IsNullOrEmpty(_searchString) || pages.Count > 0))
            {
                if (null != group.Context && 
                    (string.IsNullOrEmpty(_searchString) && group.PageCount > 0 || pages.Count > 0) && 
                    !string.IsNullOrEmpty(group.Context.Name) && group.Context.ShowPageGroupTitle)
                {
                    GUILayout.Space(8);
                    GUILayout.Label(group.Context.Name, EditorStyles.boldLabel);
                }
            }

            if(!string.IsNullOrEmpty(_searchString))
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    DrawPageEntry(pages[i]);
                }
            }
            else
            {
                group.Hierarchy.Draw();
            }
        }

        private bool PageInSearch(PageReference page)
        {
            #if UNITY_2021_1_OR_NEWER
            return page.info.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
            #else
            return page.info.Name.ToLower().Contains(_searchString.ToLower());
            #endif
        }

        private bool IsGroupInSearch(PageGroup group)
        {
            #if UNITY_2021_1_OR_NEWER
            return group.Context && group.Context.Name.Contains(_searchString,
                StringComparison.OrdinalIgnoreCase);
            #else
            return group.Context && group.Context.Name.ToLower().Contains(_searchString.ToLower());
            #endif
        }

        private bool IsGroupVisible(PageGroup group)
        {
            return group.PageCount > 0 && 
                   ContextFilter.Count == 0 && (!group.Context || group.Context.AllowWithoutContextFilter) ||
                   ContextFilter.Contains(group.Context ? group.Context.Name : "");
        }

        private void DrawPageEntry(PageReference page)
        {
            GUIStyle optionStyle = new GUIStyle(GUI.skin.label);
            optionStyle.normal.background = null;
            optionStyle.normal.textColor = _selectedPage == page.page ? Color.white : GUI.skin.label.normal.textColor;

            if (null == _selectedPage)
            {
                // TODO: We will need to improve this logic.
                if (!string.IsNullOrEmpty(SelectedPage) && page.PageId == SelectedPage) _selectedPage = page.page;
                else if(string.IsNullOrEmpty(SelectedPage)) _selectedPage = page.page;
            }

            EditorGUILayout.BeginHorizontal();
            {
                Rect optionRect = GUILayoutUtility.GetRect(GUIContent.none, optionStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20));

                bool isHover = optionRect.Contains(Event.current.mousePosition);
                if (isHover)
                {
                    EditorGUIUtility.AddCursorRect(optionRect, MouseCursor.Link);
                }

                Color backgroundColor;
                if (page.page == _selectedPage)
                {
                    backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.44f, 0.88f) : new Color(0.24f, 0.48f, 0.90f);
                }
                else
                {
                    backgroundColor = Color.clear;
                }

                EditorGUI.DrawRect(optionRect, backgroundColor);
                GUI.Label(optionRect, new GUIContent(page.info.Name, page.info.Name), optionStyle);

                if (Event.current.type == EventType.MouseDown && isHover)
                {
                    _selectedPage = page.page;
                    Event.current.Use();
                }
            }
            EditorGUILayout.EndHorizontal();
        }


        protected virtual void DrawRightPanel()
        {
            // Create a GUIStyle with a darker background color
            GUIStyle darkBackgroundStyle = new GUIStyle();
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, .25f));
            backgroundTexture.Apply();
            darkBackgroundStyle.normal.background = backgroundTexture;

            // Apply the dark background style to the right panel
            EditorGUILayout.BeginVertical(darkBackgroundStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedPage is ScriptableObjectPage soPage)
            {
                if(soPage.Editor is IOverrideSize size) {
                    size.OverrideWidth = EditorGUIUtility.currentViewWidth - _leftPanelWidth;
                }
            }
            _selectedPage?.OnGUI();
            
            EditorGUILayout.EndVertical();
        }

        public static T ShowWindow<T>(params string[] contexts) where T : MetaHub
        {
            var window = EditorWindow.GetWindow<T>();
            window._selectedPage = null;
            window.titleContent = new GUIContent("Meta Hub");
            window.ContextFilter.Clear();
            foreach (var context in contexts)
            {
                window.ContextFilter.Add(context);
            }
            window.UpdateContextFilter();
            window.Show();
            return window;
        }
    }

    internal class ScriptableObjectPage : IMetaHubPage, IPageInfo
    {
        private readonly ScriptableObject _page;
        private string _context;
        private Editor _editor;
        private string _name;
        private string _prefix = "";
        private int _priority;

        public string Name => _name;
        public string Prefix => _prefix;
        public string Context => _context;
        public Editor Editor => _editor;
        public int Priority => _priority;
        
        public ScriptableObjectPage(ScriptableObject page, MetaHubPageAttribute pageInfo)
        {
            _page = page;
            _context = pageInfo.Context;
            _priority = pageInfo.Priority;
            _prefix = pageInfo.Prefix;

            UpdatePageInfo();
        }
        
        public ScriptableObjectPage(ScriptableObject page, string context, string prefix = "", int priority = 0)
        {
            _page = page;
            _context = context;
            _priority = priority;
            _prefix = prefix;

            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            if (_page is IPageInfo info)
            {
                if (!string.IsNullOrEmpty(info.Name)) _name = info.Name;
                if (!string.IsNullOrEmpty(info.Context)) _context = info.Context;
                if (!string.IsNullOrEmpty(info.Prefix)) _prefix = info.Prefix;
                if (info.Priority != 0) _priority = info.Priority;
            }
            else
            {
                _name = _page.name;
            }
        }

        public void OnGUI()
        {
            if (_page)
            {
                // Create an editor for the assigned ScriptableObject
                if (_editor == null || _editor.target != _page)
                {
                    _editor = Editor.CreateEditor(_page);
                }

                // Render the ScriptableObject with its default editor
                _editor.OnInspectorGUI();
            }
        }
    }
}
