// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;
using VRBuilder.Core;
using UnityEngine;

namespace VRBuilder.Editor.Tabs
{
    internal class LockablePropertyTab : ITab
    {
        private readonly Step.EntityData data;

        private LockableObjectsCollection collection;

        public GUIContent Label { get; private set; }

        public LockablePropertyTab(GUIContent label, Step.EntityData data)
        {
            Label = label;
            this.data = data;
            collection = new LockableObjectsCollection(data);
        }

        public object GetValue()
        {
            return collection;
        }

        public void SetValue(object value)
        {
        }

        public void OnSelected()
        {
            collection = new LockableObjectsCollection(data);
        }

        public void OnUnselect()
        {

        }
    }
}
