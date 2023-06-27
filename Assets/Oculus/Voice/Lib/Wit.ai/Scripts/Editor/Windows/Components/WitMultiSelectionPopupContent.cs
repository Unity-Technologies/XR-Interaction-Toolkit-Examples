/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows.Conponents
{
  public class WitMultiSelectionPopupContent: PopupWindowContent
  {
    private Dictionary<string, bool> options;
    private Action<IList<string>> callback;

    private const float WINDOW_WIDTH = 400f;

    public WitMultiSelectionPopupContent(IList<string> options, HashSet<string> disabledOptions, Action<IList<string>> callback)
    {
      this.options = new Dictionary<string, bool>();
      this.callback = callback;

      options.ToList().ForEach(optName => this.options[optName] = !disabledOptions.Contains(optName));
    }

    public override Vector2 GetWindowSize()
    {
      var lineHeight = 20;
      var height = 25 + (options.Count() * lineHeight);
      return new Vector2(WINDOW_WIDTH, height);
    }

    public override void OnGUI(Rect rect)
    {
      var initialLabelWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth = WINDOW_WIDTH - 30;

      GUILayout.Label("Assemblies to include in the generated Manifest", EditorStyles.boldLabel);
      var keys = new List<string>(options.Keys);
      foreach (var optName in keys) {
        options[optName] = EditorGUILayout.Toggle(optName, options[optName], WitStyles.Toggle);
      }

      EditorGUIUtility.labelWidth = initialLabelWidth;
    }

    public override void OnClose()
    {
      var deselectedValues = this.options.Keys.Where(optName => false == this.options[optName]).ToList();
      callback(deselectedValues);
    }
  }
}
