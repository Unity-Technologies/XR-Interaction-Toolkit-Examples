using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub.UIComponents
{
    
    
    public class FoldoutHierarchy<T>
    {
        private Dictionary<string, FoldoutGroup<T>> _groups = new Dictionary<string, FoldoutGroup<T>>();
        private List<FoldoutGroup<T>> _orderedGroups = new List<FoldoutGroup<T>>();

        public void Add(string path, FoldoutHierarchyItem<T> item)
        {
            string[] parts = path.Split('/');
            FoldoutGroup<T> currentGroup = null;
            

            for (int i = 0; i < parts.Length; i++)
            {
                string key = string.Join("/", parts, 0, i + 1);

                if (!_groups.ContainsKey(key))
                {
                    FoldoutGroup<T> newGroup = new FoldoutGroup<T>(parts[i]);
                    _groups.Add(key, newGroup);
                    _orderedGroups.Add(newGroup);

                    if (currentGroup != null)
                    {
                        currentGroup.AddChild(newGroup, item, i == parts.Length - 1);
                    }
                }

                currentGroup = _groups[key];
            }
        }

        public void Draw()
        {
            foreach (var group in _orderedGroups)
            {
                if (group.Parent == null)
                {
                    group.Draw();
                }
            }
        }
    }
    
    public class FoldoutHierarchyItem<T>
    {
        public string path;
        public T item;
        public Action<T> onDraw;
    }

    public class FoldoutGroup<T>
    {
        private string _name;
        private FoldoutGroup<T> _parent;
        private List<object> _children = new List<object>();
        private List<FoldoutHierarchyItem<T>> _data = new List<FoldoutHierarchyItem<T>>();
        private int _indentSpace = 10;

        private bool _isFoldedOut = false;

        public string Name => _name;
        public FoldoutGroup<T> Parent => _parent;

        public FoldoutGroup(string name)
        {
            this._name = name;
        }

        public void AddChild(FoldoutGroup<T> child, FoldoutHierarchyItem<T> data, bool isLeaf)
        {
            child._parent = this;
            _data.Add(data);
            if(isLeaf) _children.Add(data);
            else _children.Add(child);
        }

        public void Draw(int indentLevel = 0)
        {
            if (string.IsNullOrEmpty(_name))
            {
                DrawExpanded(indentLevel);
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (indentLevel >= 0)
                {
                    GUILayout.Space(_indentSpace);
                }
                GUILayout.BeginVertical();
                _isFoldedOut = EditorGUILayout.Foldout(_isFoldedOut, _name, true);

                if (_isFoldedOut)
                {
                    DrawExpanded(indentLevel);
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private void DrawExpanded(int indentLevel)
        {   
            foreach (var child in _children)
            {
                if (child is FoldoutGroup<T> foldoutGroup)
                {
                    foldoutGroup.Draw(indentLevel);
                } else if (child is FoldoutHierarchyItem<T> leaf)
                {
                    GUILayout.BeginHorizontal();
                    if (indentLevel >= 0)
                    {
                        GUILayout.Space(_indentSpace);
                    }
                    GUILayout.BeginVertical();
                    leaf.onDraw(leaf.item);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}
