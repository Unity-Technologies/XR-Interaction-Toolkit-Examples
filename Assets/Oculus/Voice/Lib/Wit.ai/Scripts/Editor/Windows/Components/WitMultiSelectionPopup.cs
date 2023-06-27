/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows.Conponents
{
  public static class WitMultiSelectionPopup
  {
    public static void Show(IList<string> options, HashSet<string> disabledOptions, Action<IList<string>> callback) {
      // TODO It should be a rect of a button which triggers this popup, so the popup is shown next to that button.
      Rect parentRect = new Rect(0, 0, 50, 50);
      WitMultiSelectionPopupContent content = new WitMultiSelectionPopupContent(options, disabledOptions, callback);
      PopupWindow.Show(parentRect, content);
    }
  }
}
