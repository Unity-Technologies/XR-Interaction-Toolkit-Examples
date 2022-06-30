using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class GroupByStatic : IGroupByFilter
    {
        public string GetName()
        {
            return "Static";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return "isStatic=" + fi.isStatic;
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            int staticCompare = 0;
            if (b.isStatic == true && a.isStatic == false)
            {
                staticCompare = -1;
            }
            if (b.isStatic == false && a.isStatic == true)
            {
                staticCompare = 1;
            }
            return staticCompare;
        }
    }
}



