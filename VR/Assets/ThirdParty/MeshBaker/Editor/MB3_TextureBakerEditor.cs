//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    [CustomEditor(typeof(MB3_TextureBaker))]
    [CanEditMultipleObjects]
    public class MB3_TextureBakerEditor : Editor
    {

        MB3_TextureBakerEditorInternal tbe = new MB3_TextureBakerEditorInternal();

        void OnEnable()
        {
            tbe.OnEnable(serializedObject);
        }

        void OnDisable()
        {
            tbe.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            tbe.DrawGUI(serializedObject, (MB3_TextureBaker)target, targets, typeof(MB3_MeshBakerEditorWindow));
        }

    }
}