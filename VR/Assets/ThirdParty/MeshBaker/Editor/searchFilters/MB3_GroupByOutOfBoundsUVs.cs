using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class GroupByOutOfBoundsUVs : IGroupByFilter
    {
        public string GetName()
        {
            return "OutOfBoundsUVs";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return "OutOfBoundsUVs=" + fi.outOfBoundsUVs;
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            return Convert.ToInt32(b.outOfBoundsUVs) - Convert.ToInt32(a.outOfBoundsUVs);
        }
    }
}



