/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.Voice.Hub.UIComponents;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub
{
    [MetaHubPageScriptableObject]
    public class ImagePage : ScriptableObject
    {
        [SerializeField] public Texture2D image;
    }
    
    [CustomEditor(typeof(ImagePage))]
    public class ImageDisplayScriptableObjectEditor : Editor
    {
        private ImagePage _imageDisplay;
        private ImageView _imageView;

        private void OnEnable()
        {
            _imageDisplay = (ImagePage)target;
            _imageView = new ImageView(this);
        }

        public override void OnInspectorGUI()
        {
            if (_imageDisplay.image)
            {
                _imageView.Draw(_imageDisplay.image);
            }
            else
            {
                // Draw the default properties
                base.OnInspectorGUI();
            }
        }
    }
}
