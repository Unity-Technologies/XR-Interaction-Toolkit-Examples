using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class GroupByEnabledDisabled : IGroupByFilter
    {
        public string GetName()
        {
            return "Is Enabled";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return fi.go.activeInHierarchy ? "Enabled" : "Disabled";
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            int compareVal = 0;
            if (b.go.activeInHierarchy == true && a.go.activeInHierarchy == false)
            {
                compareVal = -1;
            }
            if (b.go.activeInHierarchy == false && a.go.activeInHierarchy == true)
            {
                compareVal = 1;
            }
            return compareVal;
        }
    }
}



