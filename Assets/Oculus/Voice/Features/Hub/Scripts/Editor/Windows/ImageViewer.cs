/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.UIComponents;
using UnityEngine;
using UnityEditor;

public class ImageViewer : EditorWindow
{
    private Texture2D _image;
    private ImageView _imageView;

    public static void ShowWindow(Texture2D image, string title)
    {
        ImageViewer window = CreateInstance<ImageViewer>();
        window._image = image;
        window.titleContent = new GUIContent(title);
        window.Show();
    }

    private void OnEnable()
    {
        if (_image == null)
        {
            Close();
            return;
        }
    }

    private void OnGUI()
    {
        if(null == _imageView) _imageView = new ImageView(this);
        _imageView.Draw(_image);
    }
}
